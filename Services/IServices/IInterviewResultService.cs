// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;

namespace AI_Interviewer.Services.IServices;

public interface IInterviewResultService {
    /// <summary>
    /// 构建一场完整的面试结果对象（不含 AI 分析）
    /// </summary>
    InterviewAnswer BuildInterviewAnswer(
        string candidateName,
        Resume resume,
        IReadOnlyList<Question> qAndA,
        EmotionSummary emotionSummary,
        DateTime? time = null
    );

    void Init(string appId, string apiKey, string apiSecret);

    /// <summary>
    /// 基于 InterviewAnswer 生成结构化面试分析（JSON → 强类型）
    /// </summary>
    Task<InterviewAnalysisResult> GenerateAnalysisAsync(
        InterviewAnswer interview,
        CancellationToken cancellationToken = default
    );
}