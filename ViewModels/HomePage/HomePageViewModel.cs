// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

namespace AI_Interviewer.ViewModels.HomePage;

public partial class HomePageViewModel : ObservableObject {
    [ObservableProperty] private string _homePageTitle = "主页";
}