// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

namespace AI_Interviewer.Models;

public partial class Question : ObservableObject {
    [ObservableProperty] private string _questionText  = string.Empty;
    [ObservableProperty] private DifficultyLevel _difficulty;
    [ObservableProperty] private string _customAnswer = string.Empty;
}