// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;

namespace AI_Interviewer.Services.IServices;

public interface IInterviewQuestionService {
    void Init(string appId, string apiKey, string apiSecret);

    Task<List<Question>> GenerateQuestionsAsync(Resume resume, DifficultyLevel difficulty, int count,
        CancellationToken cancellationToken = default);

    Task<Question> GenerateFollowUpQuestionAsync(Question currentQuestion, FollowUpDepthLevel depth,
        CancellationToken cancellationToken = default);
}