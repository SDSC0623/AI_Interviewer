// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using Newtonsoft.Json;

namespace AI_Interviewer.Services;

public class InterviewResultService(ILlmChatService llm) : IInterviewResultService {
    private string _appId = string.Empty;
    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;
    private bool _hasInitialized;

    public void Init(string appId, string apiKey, string apiSecret) {
        if (_hasInitialized) {
            return;
        }

        _appId = appId;
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        llm.Init(appId, apiKey, apiSecret);
        _hasInitialized = true;
    }

    public InterviewAnswer BuildInterviewAnswer(string candidateName, Resume resume, IReadOnlyList<Question> qAndA,
        EmotionSummary emotionSummary, DateTime? time = null) {
        return new InterviewAnswer {
            Name = candidateName,
            Time = time ?? DateTime.Now,
            Resume = resume,
            QAndA = qAndA.ToList(),
            EmotionSummary = emotionSummary
        };
    }

    public async Task<InterviewAnalysisResult> GenerateAnalysisAsync(InterviewAnswer interview,
        CancellationToken cancellationToken = default) {
        // 1. 构建 Prompt
        var prompt = InterviewAnalysisPromptBuilder.BuildAnalysisPrompt(interview);

        var request = new SparkChatRequest {
            Header = new SparkRequestHeader {
                AppId = _appId
            },
            Payload = new SparkChatPayload {
                Message = new SparkMessage {
                    Text = [
                        new SparkMessageText {
                            Role = "user",
                            Content = prompt
                        }
                    ]
                }
            }
        };

        // 2. 调用大模型
        var response = await llm.SendAsync(request, cancellationToken);

        // 3. 提取 JSON（防止模型前后乱说话）
        var json = ExtractJson(response.Payload?.Choices?.Text.FirstOrDefault()?.Content ?? string.Empty);

        // 4. 反序列化
        var result = JsonConvert.DeserializeObject<InterviewAnalysisResult>(json);

        if (result == null) {
            throw new InvalidOperationException("面试分析结果解析失败");
        }

        return result;
    }

    private static string ExtractJson(string content) {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');

        if (start < 0 || end <= start) {
            throw new InvalidOperationException("未能从模型输出中提取 JSON");
        }

        return content.Substring(start, end - start + 1);
    }
}