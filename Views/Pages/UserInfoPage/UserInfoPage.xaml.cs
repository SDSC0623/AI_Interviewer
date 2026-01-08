// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using AI_Interviewer.Attributes;
using AI_Interviewer.ViewModels.UserInfoPage;

namespace AI_Interviewer.Views.Pages.UserInfoPage;

[AvailbleStartPage(1)]
[Description("个人信息设置")]
[NeedDisposePage(nameof(Dispose))]
public partial class UserInfoPage : Page {
    public UserInfoPage(UserInfoPageViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task Dispose() {
        await (DataContext as UserInfoPageViewModel)!.Dispose();
    }
}