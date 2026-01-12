// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using AI_Interviewer.Models;

namespace AI_Interviewer.Helpers;

public static class InterviewQuestionPromptBuilder {
    public static string BuildPromptWithDifficulty(Resume resume, int questionCount, DifficultyLevel difficulty) {
        var name = resume.BasicInfo.Name;
        var position = resume.JobIntention.TargetPosition;

        // 教育经历
        var educationDetails = resume.Educations
            .Where(e => !string.IsNullOrWhiteSpace(e.School))
            .Select(e => $"{e.School} {e.Major} {CommonHelper.GetEnumDescription(e.Degree)}")
            .ToList();

        // 工作经历
        var workDetails = resume.WorkExperiences
            .Where(w => !string.IsNullOrWhiteSpace(w.Company))
            .Select(w => new {
                w.Company,
                w.Position,
                w.Industry,
                Duration = $"{w.StartTime:yyyy.MM} 至 {w.EndTime:yyyy.MM}"
            })
            .ToList();

        var difficultyText = CommonHelper.GetEnumDescription(difficulty);
        var difficultyRequirement = GetDifficultyRequirement(difficultyText);

        var sb = new StringBuilder();

        sb.AppendLine($"你是一位资深的HR面试官，需要为以下候选人设计{questionCount}道{difficultyText}难度的面试题。");
        sb.AppendLine();
        sb.AppendLine("【候选人信息】");
        sb.AppendLine($"姓名：{name}");
        sb.AppendLine($"应聘岗位：{position}");
        sb.AppendLine($"教育背景：{(educationDetails.Any() ? string.Join('\n', educationDetails) : "暂无教育经历信息")}");
        sb.AppendLine();
        sb.AppendLine("【工作经历】");

        if (workDetails.Any()) {
            int i = 1;
            foreach (var work in workDetails) {
                sb.AppendLine($"{i}. {work.Company} - {work.Position}");
                sb.AppendLine($"   行业：{work.Industry}");
                sb.AppendLine($"   时间：{work.Duration}");
                i++;
            }
        } else {
            sb.AppendLine("暂无工作经历信息");
        }

        sb.AppendLine();
        sb.AppendLine($"【{difficultyText}难度要求】");
        sb.AppendLine(difficultyRequirement.Description);
        sb.AppendLine($"重点考察：{difficultyRequirement.Focus}");
        sb.AppendLine($"题目类型：{difficultyRequirement.Examples}");
        sb.AppendLine();
        sb.AppendLine("【面试题设计要求】");
        sb.AppendLine($"请设计{questionCount}道{difficultyText}难度的面试题，要求：");
        sb.AppendLine("1. 符合对应难度标准");
        sb.AppendLine("2. 结合候选人的具体背景");
        sb.AppendLine("3. 针对应聘岗位设计");
        sb.AppendLine("4. 题目具体明确，避免过于宽泛");
        sb.AppendLine("5. 能够有效区分候选人能力水平");
        sb.AppendLine();
        sb.AppendLine($"请直接输出{questionCount}道面试题，每题一行，格式如下：");
        sb.AppendLine("1. 题目内容");
        sb.AppendLine("2. 题目内容");
        sb.AppendLine("...");

        return sb.ToString();
    }

    private static (string Description, string Focus, string Examples) GetDifficultyRequirement(string difficulty) {
        return difficulty switch {
            "初级" => (
                "基础问题，适合应届生或初入职场",
                "基础知识、学习能力、沟通表达、团队合作",
                "基础专业知识、自我介绍、学习经历、团队项目经验"
            ),
            "高级" => (
                "深度问题，适合资深从业者",
                "战略思维、领导能力、创新能力、行业洞察",
                "架构设计、团队管理、业务创新、行业趋势"
            ),
            _ => (
                "需要一定经验，适合有1-3年工作经验",
                "专业技能、问题解决、项目经验、职业规划",
                "技术深度、项目管理、问题分析、职业发展"
            )
        };
    }

    public static string BuildFollowUpPrompt(Question currentQuestion, string candidateAnswer,
        FollowUpDepthLevel depth) {
        var depthText = CommonHelper.GetEnumDescription(depth);
        var depthRequirement = GetDepthRequirement(depthText);

        var sb = new StringBuilder();

        sb.AppendLine("你是一位经验丰富的面试官，正在进行一场真实的面试。");
        sb.AppendLine($"请基于以下问答内容，生成一个【{depthText}】追问问题。");
        sb.AppendLine();
        sb.AppendLine("【原问题】");
        sb.AppendLine(currentQuestion.QuestionText);
        sb.AppendLine();
        sb.AppendLine("【候选人回答】");
        sb.AppendLine(candidateAnswer);
        sb.AppendLine();
        sb.AppendLine($"【{depthText}追问要求】");
        sb.AppendLine(depthRequirement);
        sb.AppendLine();
        sb.AppendLine("【追问设计要求】");
        sb.AppendLine("1. 只生成一个追问问题");
        sb.AppendLine("2. 问题应紧密围绕候选人的回答内容");
        sb.AppendLine("3. 不要重复原问题");
        sb.AppendLine("4. 不要评价或总结候选人的回答");
        sb.AppendLine("5. 不要输出除问题以外的任何内容");
        sb.AppendLine();
        sb.AppendLine("请直接输出追问问题：");

        return sb.ToString();
    }

    private static string GetDepthRequirement(string depthText) =>
        depthText switch {
            "浅层" =>
                "用于简单澄清候选人回答中的关键信息，确认其真实含义或具体背景，例如细节补充、事实确认。",
            "深层" =>
                "用于深入考察候选人的思考深度、原理理解、决策依据、边界条件或潜在风险，具有一定挑战性。",
            _ =>
                "用于进一步挖掘候选人的思路、方法和经验，关注其分析过程和解决问题的能力。"
        };

    public static List<Question> ParseQuestions(string response, DifficultyLevel difficulty) {
        if (string.IsNullOrWhiteSpace(response)) {
            return [];
        }

        var questions = new List<Question>();
        var lines = response.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in lines) {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            // 移除序号
            if (char.IsDigit(line[0]) && line.Contains(". ")) {
                line = line.Split(". ", 2)[1];
            }

            line = CleanQuestionText(line);

            if (!string.IsNullOrWhiteSpace(line)) {
                questions.Add(new Question {
                    QuestionText = line,
                    Difficulty = difficulty
                });
            }
        }

        return questions;
    }

    private static string CleanQuestionText(string text) {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // **标签**
        text = Regex.Replace(text, @"\*\*[^*]+\*\*[：:]?\s*", "");

        // (括号注释)
        text = Regex.Replace(text, @"\([^)]*\)", "");

        // 【标签】
        text = Regex.Replace(text, @"【[^】]+】[：:]?\s*", "");

        // 多余空白
        text = Regex.Replace(text, @"\s+", " ").Trim();

        // 开头标点
        text = Regex.Replace(text, @"^[：:，,。.\s]+", "");

        return text;
    }
}