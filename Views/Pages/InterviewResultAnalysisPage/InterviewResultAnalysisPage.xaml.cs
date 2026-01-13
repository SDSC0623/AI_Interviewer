// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using AI_Interviewer.Attributes;
using AI_Interviewer.ViewModels.InterviewResultAnalysisPage;

namespace AI_Interviewer.Views.Pages.InterviewResultAnalysisPage;

[AvailbleStartPage(3)]
[Description("面试结果分析")]
public partial class InterviewResultAnalysisPage : Page {
    public InterviewResultAnalysisPage(InterviewResultAnalysisViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}