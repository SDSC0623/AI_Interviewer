// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;

namespace AI_Interviewer.Services.IServices;

public interface ILearningPathService {
    void Init(string appId, string apiKey, string apiSecret);
    Task<LearningPathResult> GenerateLearningPathAsync(List<InterviewAnswer> answers);
}