// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace AI_Interviewer.Models;

public sealed class InterviewResult {
    public string Summary { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = [];
    public List<string> Weaknesses { get; set; } = [];
    public HiringRecommendation Recommendation { get; set; }
}

public enum HiringRecommendation {
    [Description("强推荐")] StrongHire,
    [Description("推荐")] Hire,
    [Description("中立")] Neutral,
    [Description("拒绝")] Reject
}