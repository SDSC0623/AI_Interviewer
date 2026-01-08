// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using AI_Interviewer.Attributes;
using AI_Interviewer.ViewModels.HomePage;

namespace AI_Interviewer.Views.Pages.HomePage;

[AvailbleStartPage(0)]
[Description("主页")]
public partial class HomePage : Page {
    public HomePage(HomePageViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}