// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using AI_Interviewer.Attributes;
using AI_Interviewer.ViewModels.LearningPathPage;

namespace AI_Interviewer.Views.Pages.LearningPathPage;

[AvailbleStartPage(4)]
[Description("个人学习路径")]
public partial class LearningPathPage : Page {
    public LearningPathPage(LearningPathViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}