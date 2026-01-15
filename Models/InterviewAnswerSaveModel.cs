// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

#pragma warning disable CS0657 // 不是此声明的有效特性位置

namespace AI_Interviewer.Models;

public partial class InterviewAnswer : ObservableObject {
    public string Name { get; set; } = string.Empty;
    public required DateTime Time { get; set; }
    public List<Question> QAndA { get; set; } = [];
    public Resume Resume { get; set; } = new();
    public EmotionSummary EmotionSummary { get; set; } = new();
    public InterviewAnalysisResult InterviewAnalysisResult { get; set; } = new();

    [property: JsonIgnore] [ObservableProperty]
    private bool _isMenuOpen;

    [property: JsonIgnore] [ObservableProperty]
    private bool _hasResult;
}