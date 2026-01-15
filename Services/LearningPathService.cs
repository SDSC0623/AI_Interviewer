// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using Markdig;

namespace AI_Interviewer.Services;

public class LearningPathService(ILlmChatService llm) : ILearningPathService {
    private string _appId = string.Empty;
    private bool _hasInitialized;
    private static string TempFolder => GlobalSettings.TempDirectory;

    public void Init(string appId, string apiKey, string apiSecret) {
        if (_hasInitialized) {
            return;
        }

        _appId = appId;
        llm.Init(appId, apiKey, apiSecret);
        _hasInitialized = true;
    }

    public async Task<LearningPathResult> GenerateLearningPathAsync(List<InterviewAnswer> answers) {
        if (!_hasInitialized) {
            throw new InvalidOperationException("LearningPathService 尚未初始化");
        }

        var prompt = LearningPathPromptBuilder.Build(answers);

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
        var response = await llm.SendAsync(request);
        var markdown = response.Payload?.Choices?.Text.FirstOrDefault()?.Content;

        if (string.IsNullOrWhiteSpace(markdown)) {
            throw new Exception("AI 未返回有效内容");
        }

        var html = BuildHtml(markdown);

        var path = SaveHtmlToTemp(html);

        return new LearningPathResult {
            Markdown = markdown,
            HtmlPath = path
        };
    }

    private static string BuildHtml(string markdown) {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        var body = Markdown.ToHtml(markdown, pipeline);

        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\" />");
        sb.AppendLine("<title>AI 个性化学习路径报告</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body {");
        sb.AppendLine("font-family: \"Microsoft YaHei\", sans-serif;");
        sb.AppendLine("background: #f8f9fa;");
        sb.AppendLine("padding: 40px;");
        sb.AppendLine("}");
        sb.AppendLine(".container {");
        sb.AppendLine("max-width: 1000px;");
        sb.AppendLine("margin: auto;");
        sb.AppendLine("background: #fff;");
        sb.AppendLine("padding: 40px;");
        sb.AppendLine("border-radius: 12px;");
        sb.AppendLine("box-shadow: 0 8px 20px rgba(0,0,0,.08);");
        sb.AppendLine("}");
        sb.AppendLine("h1, h2 {");
        sb.AppendLine("border-left: 4px solid #2196F3;");
        sb.AppendLine("padding-left: 12px;");
        sb.AppendLine("}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("<h1>🎯 AI 个性化学习路径报告</h1>");
        sb.AppendLine($"<p style=\"color:#666;\">生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("<hr />");

        sb.AppendLine(body);

        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string SaveHtmlToTemp(string html) {
        var file = Path.Combine(TempFolder, $"learning_path_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        File.WriteAllText(file, html, Encoding.UTF8);
        return file;
    }
}