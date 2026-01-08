// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;
using AI_Interviewer.ViewModels.AskBeforeExitDialog;
using Wpf.Ui.Controls;

namespace AI_Interviewer.Views.Windows;

public partial class AskBeforeExit : FluentWindow {
    public ExitMode ExitMode { get; set; } = ExitMode.Ask;

    public AskBeforeExit(AskBeforeExitViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
    }
}