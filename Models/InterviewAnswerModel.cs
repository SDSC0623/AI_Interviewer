// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace AI_Interviewer.Models;

public class InterviewAnalysisResult {
    public OverallEvaluation Overall { get; set; } = new();

    public Dictionary<string, DimensionEvaluation> Dimensions { get; set; } = new();

    public EmotionAnalysisResult EmotionAnalysis { get; set; } = new();

    public List<string> Strengths { get; set; } = [];
    public List<string> Risks { get; set; } = [];

    public RecommendationResult Recommendation { get; set; } = new();
}

public class OverallEvaluation {
    public int Score { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public class DimensionEvaluation {
    public int Score { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class EmotionAnalysisResult {
    public string Summary { get; set; } = string.Empty;
    public string Stability { get; set; } = string.Empty;
    public List<string> RiskFlags { get; set; } = new();
}

public class RecommendationResult {
    public string Result { get; set; } = string.Empty;
    public string ConfidenceLevel { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}