// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace AI_Interviewer.Models;

public class InterviewAnswer {
    public string Name { get; set; } = string.Empty;
    public required DateTime Time { get; set; }
    public List<Question> QAndA { get; set; } = [];
    public Resume Resume { get; set; } = new();
    public EmotionSummary EmotionSummary { get; set; } = new();
}