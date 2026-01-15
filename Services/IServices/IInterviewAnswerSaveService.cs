// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;

namespace AI_Interviewer.Services.IServices;

public interface IInterviewAnswerSaveService {
    /// <summary>
    /// 特别提醒，命名要符合如下规范:
    /// "面试-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}"
    /// </summary>
    /// <param name="name">文件名</param>
    /// <param name="qAndA">问题和回答List</param>
    /// <param name="emotionSummary">情绪分布</param>
    /// <param name="resume">简历</param>
    /// <param name="interviewAnalysisResult">分析结果</param>
    void SaveAnswer(string name, List<Question> qAndA, EmotionSummary emotionSummary, Resume resume, InterviewAnalysisResult? interviewAnalysisResult = null);

    void SaveAnswer(InterviewAnswer interviewAnswer);
    void DeleteAnswer(string name);
    InterviewAnswer GetAnswer(string name);
    List<InterviewAnswer> GetAllAnswer();
}