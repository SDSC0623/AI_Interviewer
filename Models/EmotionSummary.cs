// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace AI_Interviewer.Models;

public class EmotionSummary {
    public Dictionary<EmotionType, double> Ratios { get; set; } = new();
    public EmotionType DominantEmotion { get; set; }
    public double Volatility { get; set; }
    public int TotalSamples { get; set; }
    public bool HasFaceMissingIssue { get; set; }
}