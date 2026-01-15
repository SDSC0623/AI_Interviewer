// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text;
using AI_Interviewer.Models;

namespace AI_Interviewer.Helpers;

public static class InterviewAnalysisPromptBuilder {
    public static string BuildAnalysisPrompt(InterviewAnswer interview) {
        var sb = new StringBuilder();

        AppendRoleDefinition(sb);
        AppendCandidateProfile(sb, interview);
        AppendResumeContext(sb, interview.Resume);
        AppendQnA(sb, interview.QAndA);
        AppendEmotionSummary(sb, interview.EmotionSummary);
        AppendAnalysisRequirements(sb);

        return sb.ToString();
    }

    private static void AppendRoleDefinition(StringBuilder sb) {
        sb.AppendLine("""
                      你是一位资深的综合面试评估专家，具备以下背景：
                      - 10年以上企业招聘与面试经验
                      - 熟悉行为面试法（STAR）
                      - 熟悉情绪与非语言行为对面试表现的影响
                      - 能综合候选人的语言内容、行为特征与背景进行判断

                      请基于以下信息，对候选人进行**客观、专业、结构化**的面试评估。
                      """);
    }

    private static void AppendCandidateProfile(StringBuilder sb, InterviewAnswer interview) {
        sb.AppendLine($"""
                       【面试基本信息】
                       候选人姓名：{interview.Name}
                       面试时间：{interview.Time:yyyy年MM月dd日 HH:mm}
                       应聘岗位：{interview.Resume.JobIntention.TargetPosition}
                       """);
    }

    private static void AppendResumeContext(StringBuilder sb, Resume resume) {
        sb.AppendLine("【候选人简历背景】");

        sb.AppendLine("教育背景：");
        if (resume.Educations.Any()) {
            foreach (var edu in resume.Educations) {
                sb.AppendLine($"- {edu.School}，{edu.Major}，{edu.Degree}");
            }
        } else {
            sb.AppendLine("- 无明确教育经历信息");
        }

        sb.AppendLine("\n工作经历：");
        if (resume.WorkExperiences.Any()) {
            foreach (var work in resume.WorkExperiences) {
                sb.AppendLine($"- {work.Company} | {work.Position} | {work.Industry}");
            }
        } else {
            sb.AppendLine("- 无明确工作经历信息");
        }
    }

    private static void AppendQnA(StringBuilder sb, List<Question> qna) {
        sb.AppendLine("【面试问答记录】");

        for (int i = 0; i < qna.Count; i++) {
            var q = qna[i];
            sb.AppendLine($"""
                           问题 {i + 1}（难度：{q.Difficulty}）：
                           {q.QuestionText}

                           候选人回答：
                           {(string.IsNullOrWhiteSpace(q.CustomAnswer) ? "（未作答）" : q.CustomAnswer)}
                           """);
        }
    }

    private static void AppendEmotionSummary(StringBuilder sb, EmotionSummary emotion) {
        // 1. 计算积极 / 消极 / 中性占比
        var positiveRatio = GetRatio(emotion, EmotionType.Happy);
        var neutralRatio = GetRatio(emotion, EmotionType.Neutral);

        var negativeRatio =
            GetRatio(emotion, EmotionType.Angry) +
            GetRatio(emotion, EmotionType.Disgusted) +
            GetRatio(emotion, EmotionType.Fearful) +
            GetRatio(emotion, EmotionType.Sad) +
            GetRatio(emotion, EmotionType.Surprised);

        var unknownRatio = GetRatio(emotion, EmotionType.Unknown);

        // 2. 情绪稳定度描述
        var stabilityDescription = emotion.Volatility switch {
            < 0.2 => "情绪表现非常稳定",
            < 0.4 => "整体较为稳定，偶有轻微波动",
            < 0.6 => "情绪存在一定波动",
            < 0.8 => "情绪波动较明显",
            _ => "情绪表现不够稳定"
        };

        // 3. 数据可靠性说明
        string reliabilityNote = emotion.HasFaceMissingIssue
            ? "面试过程中存在一定比例的人脸未识别情况，情绪数据可靠性略有影响"
            : "全程人脸识别正常，情绪数据具有较高参考价值";

        sb.AppendLine("【候选人情绪与行为特征（基于全程面试统计）】");

        sb.AppendLine($"""
                       - 总情绪采样次数：{emotion.TotalSamples}
                       - 主导情绪类型：{emotion.DominantEmotion}
                       - 情绪稳定度评估：{stabilityDescription}
                       - 积极情绪占比：{positiveRatio:P0}
                       - 中性情绪占比：{neutralRatio:P0}
                       - 消极情绪占比：{negativeRatio:P0}
                       - 未识别/异常占比：{unknownRatio:P0}
                       - 数据可靠性说明：{reliabilityNote}

                       请在后续分析中，将情绪数据作为**行为倾向参考**，避免对单一情绪进行过度解读。
                       """);
        sb.AppendLine("上述情绪数据仅用于后续 emotion_analysis 字段中的 summary、stability 与 risk_flags。");
    }

    private static double GetRatio(EmotionSummary summary, EmotionType type) {
        return summary.Ratios.GetValueOrDefault(type, 0d);
    }


    private static void AppendAnalysisRequirements(StringBuilder sb) {
        sb.AppendLine("""
                      【综合评估与输出要求】

                      请你基于以上所有信息，生成一份【结构化面试评估结果】。

                      ⚠️ 强制要求：
                      1. 只允许输出 JSON
                      2. 不要添加 Markdown
                      3. 不要解释字段含义
                      4. 所有 score 范围为 0-100 的整数
                      5. 所有评价应基于给定事实，避免猜测，且评价必须为中文
                      6. 情绪分析仅作为行为参考，不做心理诊断

                      【JSON 输出结构】

                      {
                        "overall": {
                          "score": number,
                          "level": string,
                          "summary": string
                        },
                        "dimensions": {
                          "professional_ability": {
                            "score": number,
                            "comment": string
                          },
                          "communication": {
                            "score": number,
                            "comment": string
                          },
                          "thinking_ability": {
                            "score": number,
                            "comment": string
                          },
                          "attitude_and_values": {
                            "score": number,
                            "comment": string
                          },
                          "learning_potential": {
                            "score": number,
                            "comment": string
                          }
                        },
                        "emotion_analysis": {
                          "summary": string,
                          "stability": string,
                          "risk_flags": string[]
                        },
                        "strengths": string[],
                        "risks": string[],
                        "recommendation": {
                          "result": string,
                          "confidence_level": string,
                          "notes": string
                        }
                      }

                      再次强调：只输出 JSON，不要包含任何其他内容。
                      """);
    }
}