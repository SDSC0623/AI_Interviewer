// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using AI_Interviewer.ViewModels;
using AI_Interviewer.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace AI_Interviewer.Views;

public partial class MainWindow : INavigationWindow {
    // App运行辅助
    private readonly AppRunningHelper _appRunningHelper;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService,
        ISnackbarService snackbarService, IContentDialogService contentDialogService, AppRunningHelper appRunningHelper,
        IPreferencesService preferencesService, SnackbarServiceHelper snackbarService1) {
        InitializeComponent();
        DataContext = viewModel;

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        _snackbarService = snackbarService1;
        navigationService.SetNavigationControl(RootNavigation);

        Application.Current.MainWindow = this;

        _appRunningHelper = appRunningHelper;
        contentDialogService.SetDialogHost(RootContentDialog);
        _preferencesService = preferencesService;

        Loaded += (_, _) => { WindowState = WindowState.Maximized; };
    }

    private void OnNotifyIconLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e) {
        _appRunningHelper.Show();
    }

    protected override async void OnClosing(CancelEventArgs e) {
        try {
            e.Cancel = true;

            var result = _appRunningHelper.NeedConfirmBeforeEndApp();

            if (result.NeedConfirm) {
                var dialog = new MessageBox {
                    Title = "有任务正在运行，是否退出？",
                    Content = $"{result.Sender} 正在运行中，退出会终止任务且不会保存进度。",
                    PrimaryButtonText = "退出",
                    PrimaryButtonAppearance = ControlAppearance.Danger,
                    CloseButtonText = "取消"
                };
                if (await dialog.ShowDialogAsync() != MessageBoxResult.Primary) {
                    return;
                }
            }

            var mode = _preferencesService.Get("ExitMode", ExitMode.Ask);
            if (mode == ExitMode.Ask) {
                var dialog = App.GetService<AskBeforeExit>()!;
                dialog.Owner = Application.Current.MainWindow;
                mode = dialog.ShowDialog() == true ? dialog.ExitMode : ExitMode.Ask;
            }

            if (mode == ExitMode.Exit) {
                _appRunningHelper.ExitApp();
                base.OnClosing(e);
            } else if (mode == ExitMode.Hide) {
                _appRunningHelper.Hide();
            }
        } catch (Exception ex) {
            _snackbarService.ShowError("退出时发生错误", ex.Message);
            await Task.Delay(3000);
            Environment.Exit(-1);
        }
    }

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetServiceProvider(IServiceProvider serviceProvider) {
        throw new UnexpectedCallException();
    }

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
        RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();
}