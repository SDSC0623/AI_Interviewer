// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;
using AI_Interviewer.Models;

namespace AI_Interviewer.Helpers;

public static class LearningPathPromptBuilder {
    public static string Build(List<InterviewAnswer> answers) {
        var sb = new StringBuilder();

        sb.AppendLine("你是一名资深技术导师与职业发展顾问。");
        sb.AppendLine("以下内容是系统已经完成的【面试评估与能力分析结果】。");
        sb.AppendLine("这些分析结论是可信的，不需要你重新判断对错。");
        sb.AppendLine("你的任务是：基于这些结论，为用户制定一份【可执行的个性化学习发展路径报告】。");
        sb.AppendLine();

        sb.AppendLine("====== 已有 AI 面试分析结果（核心依据） ======");

        if (answers.Count == 0) {
            sb.AppendLine("暂无可用的面试分析结果。");
        } else {
            var recent = answers
                .OrderByDescending(a => a.Time)
                .FirstOrDefault(a => a.HasResult);

            if (recent != null) {
                AppendAnalysis(sb, recent);
            } else {
                sb.AppendLine("最近的面试尚未生成分析结果。");
            }
        }

        sb.AppendLine();
        sb.AppendLine("====== 参考：最近面试背景（非主要依据） ======");

        AppendInterviewHistory(sb, answers);

        sb.AppendLine();
        sb.AppendLine("====== 报告生成要求 ======");
        sb.AppendLine("""

                      请生成一份结构清晰、可执行的学习路径报告，必须严格基于以上【分析结果】：

                      1. 能力评估回顾（基于已有分析总结，不要重新评分）
                      2. 学习目标设定
                         - 短期（1–3 个月）
                         - 中期（3–6 个月）
                         - 长期（6–12 个月）
                      3. 个性化学习计划
                         - 学习方向
                         - 每周学习时间建议
                         - 学习方法与工具
                      4. 技能提升与实战建议
                      5. 情绪与面试状态改善建议（如有风险）
                      6. 阶段性评估与调整建议
                      7. 综合职业发展建议

                      要求：
                      - 必须严格使用 Markdown 语法，且直接输出Markdown，不要用代码块包裹Markdown
                      - 中文输出
                      - 建议具体、可执行
                      - 避免空泛描述

                      """);

        return sb.ToString();
    }

    private static void AppendAnalysis(StringBuilder sb, InterviewAnswer answer) {
        var a = answer.InterviewAnalysisResult;

        sb.AppendLine($"面试时间：{answer.Time:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("【总体评估】");
        sb.AppendLine($"- 综合评分：{a.Overall.Score}");
        sb.AppendLine($"- 能力等级：{a.Overall.Level}");
        sb.AppendLine($"- 总结：{a.Overall.Summary}");
        sb.AppendLine();

        if (a.Dimensions.Count > 0) {
            sb.AppendLine("【能力维度评估】");
            foreach (var (name, dim) in a.Dimensions) {
                sb.AppendLine($"- {name}：{dim.Score} 分，评价：{dim.Comment}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("【优势】");
        foreach (var s in a.Strengths)
            sb.AppendLine($"- {s}");

        sb.AppendLine("【风险与不足】");
        foreach (var r in a.Risks)
            sb.AppendLine($"- {r}");

        sb.AppendLine();

        sb.AppendLine("【情绪与面试状态分析】");
        sb.AppendLine($"- 情绪概述：{a.EmotionAnalysis.Summary}");
        sb.AppendLine($"- 稳定性：{a.EmotionAnalysis.Stability}");
        if (a.EmotionAnalysis.RiskFlags.Count > 0) {
            sb.AppendLine("- 风险信号：");
            foreach (var flag in a.EmotionAnalysis.RiskFlags)
                sb.AppendLine($"  • {flag}");
        }

        sb.AppendLine();

        sb.AppendLine("【AI 建议与匹配度】");
        sb.AppendLine($"- 推荐结论：{a.Recommendation.Result}");
        sb.AppendLine($"- 信心水平：{a.Recommendation.ConfidenceLevel}");
        sb.AppendLine($"- 备注：{a.Recommendation.Notes}");
    }

    private static void AppendInterviewHistory(StringBuilder sb, List<InterviewAnswer> answers) {
        var recent = answers
            .OrderByDescending(a => a.Time)
            .Take(2);

        foreach (var a in recent) {
            sb.AppendLine($"- 时间：{a.Time:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"  岗位：{a.Resume.JobIntention.TargetPosition}");
            sb.AppendLine($"  题目数：{a.QAndA.Count}");
        }
    }
}