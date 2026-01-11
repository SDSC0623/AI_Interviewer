// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;

namespace AI_Interviewer.Services.IServices;

public interface IInterviewAnswerSaveService {
    void SaveAnswer(string name, List<Question> qAndA);
    InterviewAnswer GetAnswer(string name);
    List<InterviewAnswer> GetAllAnswer();
}