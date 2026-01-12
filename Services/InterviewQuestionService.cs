// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;

namespace AI_Interviewer.Services;

public class InterviewQuestionService(ILlmChatService llmChatService) : IInterviewQuestionService {
    private string _appId = string.Empty;
    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;
    private bool _hasInitialized = false;

    public void Init(string appId, string apiKey, string apiSecret) {
        _appId = appId;
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        llmChatService.Init(appId, apiKey, apiSecret);
        _hasInitialized = true;
    }

    private void EnsureInit() {
        if (!_hasInitialized) {
            throw new InvalidOperationException("请先初始化 Spark LLM 服务。");
        }
    }

    public async Task<List<Question>> GenerateQuestionsAsync(Resume resume, DifficultyLevel difficulty, int count,
        CancellationToken cancellationToken = default) {
        EnsureInit();

        if (count <= 0) {
            return [];
        }

        var prompt = InterviewQuestionPromptBuilder.BuildPromptWithDifficulty(resume, count, difficulty);
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

        // 3. 调用 LLM
        var response = await llmChatService.SendAsync(request, cancellationToken);
        var content = response.Payload?.Choices?.Text.FirstOrDefault()?.Content;

        if (string.IsNullOrWhiteSpace(content)) {
            return [];
        }

        return InterviewQuestionPromptBuilder.ParseQuestions(content, difficulty);
    }

    public async Task<Question> GenerateFollowUpQuestionAsync(Question currentQuestion, string candidateAnswer,
        FollowUpDepthLevel depth,
        CancellationToken cancellationToken = default) {
        EnsureInit();
        var prompt = InterviewQuestionPromptBuilder.BuildFollowUpPrompt(currentQuestion, candidateAnswer, depth);
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
        var response = await llmChatService.SendAsync(request, cancellationToken);

        var content = response.Payload?.Choices?.Text.FirstOrDefault()?.Content;

        if (string.IsNullOrWhiteSpace(content)) {
            return new Question {
                QuestionText = "请你进一步展开说明刚才的回答。",
                Difficulty = currentQuestion.Difficulty
            };
        }

        return new Question {
            QuestionText = content.Trim(),
            Difficulty = currentQuestion.Difficulty
        };
    }
}