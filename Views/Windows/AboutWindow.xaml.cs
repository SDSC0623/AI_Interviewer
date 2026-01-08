// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows;
using AI_Interviewer.Helpers;
using Wpf.Ui.Controls;

namespace AI_Interviewer.Views.Windows;

public partial class AboutWindow : FluentWindow {
    public string Version => GlobalSettings.Version;

    public AboutWindow() {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) {
        Close();
    }
}