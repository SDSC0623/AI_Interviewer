// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using AI_Interviewer.Attributes;
using AI_Interviewer.ViewModels.InterviewPage;

namespace AI_Interviewer.Views.Pages.InterviewPage;

[AvailbleStartPage(2)]
[Description("模拟面试")]
[NeedDisposePage(nameof(Dispose))]
public partial class InterviewPage : Page {
    public InterviewPage(InterviewPageViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task Dispose() {
        await (DataContext as InterviewPageViewModel)!.Dispose();
    }
}