// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Serilog;
using AI_Interviewer.Attributes;
using AI_Interviewer.Models;
using AI_Interviewer.Services;
using AI_Interviewer.Services.IServices;
using AI_Interviewer.Views.Pages.HomePage;
using Wpf.Ui;
using MessageBox = Wpf.Ui.Violeta.Controls.MessageBox;
// ReSharper disable ConvertToPrimaryConstructor

namespace AI_Interviewer.Helpers;

public class AppRunningHelper {
    // 导航服务
    private readonly INavigationService _navigationService;

    // Logger
    private readonly ILogger _logger;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public AppRunningHelper(INavigationService navigationService, ILogger logger,
        IPreferencesService preferencesService, SnackbarServiceHelper snackbarService) {
        _navigationService = navigationService;
        _logger = logger;
        _preferencesService = preferencesService;
        _snackbarService = snackbarService;
    }

    private static string ReArguments() {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        for (int i = 0; i < args.Length; i++) {
            args[i] = $"""
                       "{args[i]}"
                       """;
        }

        return string.Join(" ", args);
    }

    private static void RestartAsAdmin(bool forced = false) {
        try {
            ProcessStartInfo startInfo = new() {
                UseShellExecute = true,
                WorkingDirectory = GlobalSettings.BaseDirectory,
                FileName = Path.GetFileName(Environment.ProcessPath),
                Arguments = ReArguments(),
                Verb = "runas"
            };
            try {
                _ = Process.Start(startInfo);
            } catch (Exception e) {
                App.GetService<ILogger>()!.Error("自动以管理员权限启动失败{ExMessage}", e.Message);
                MessageBox.Error("自动以管理员权限启动失败，非管理员权限下所有模拟操作功能均不可用！\r\n请尝试 右键 —— 以管理员身份运行的方式启动");
                return;
            }
        } catch (Win32Exception) {
            return;
        }

        if (forced) {
            Process.GetCurrentProcess().Kill();
        }

        Environment.Exit('r' + 'u' + 'n' + 'a' + 's');
    }

    public static void EnsureAdmin() {
        if (!GlobalSettings.IsAdmin) {
            RestartAsAdmin();
        }
    }

    private void InitPageWhenStartup() {
        var assembly = Assembly.GetExecutingAssembly();

        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Page)))
            .Where(t => t.GetCustomAttribute<NeedStartupInitAttribute>() != null)
            .OrderBy(t => t.Name);

        foreach (var pageType in pageTypes) {
            _navigationService.Navigate(pageType);
        }
    }

    public void StartApp() {
        try {
            InitPageWhenStartup();
            App.Current.SessionEnding += (_, _) => { EndApp(); };
            InitDirectory();
            var targetPage = _preferencesService.Get("StartPage", typeof(HomePage))!;
            _navigationService.Navigate(targetPage);
            _logger.Information("程序启动完成，详细版本: [{FullVersion}]", GlobalSettings.FullVersion);
            _logger.Information("运行路径: [{BaseDirectory}]", GlobalSettings.BaseDirectory);
        } catch (PreferencesException ex) {
            _navigationService.Navigate(typeof(HomePage));
            _preferencesService.Set("StartPage", typeof(HomePage));
            _snackbarService.ShowError("加载设定页面失败，已跳转到首页并重置本地化配置为首页", ex.Message);
        } catch (Exception ex) {
            _logger.Error("导航到设定启动页面时出错，错误：{Message}", ex.Message);
            _snackbarService.ShowError("导航到设定页面是时发生错误", ex.Message);
        }
    }

    private static void InitDirectory() {
        if (!Directory.Exists(GlobalSettings.TempDirectory)) {
            Directory.CreateDirectory(GlobalSettings.TempDirectory);
        }
    }

    private void DisposePageWhenExit() {
        var assembly = Assembly.GetExecutingAssembly();

        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Page)))
            .Where(t => t.GetCustomAttribute<NeedDisposePageAttribute>() != null)
            .OrderBy(t => t.Name);

        foreach (var pageType in pageTypes) {
            try {
                var attr = pageType.GetCustomAttribute<NeedDisposePageAttribute>()!;
                var method = pageType.GetMethod(attr.DisposeAction)!;
                var instance = App.GetService(pageType);
                method.Invoke(instance, null);
            } catch (Exception ex) {
                _logger.Error("释放页面失败 {Page}: {Msg}", pageType.Name, ex.Message);
            }
        }
    }

    private static void ClearFolderParallel(string folderPath) {
        if (!Directory.Exists(folderPath)) {
            App.GetService<ILogger>()!.Error("文件夹不存在: {FolderPath}", folderPath);
            return;
        }

        var directory = new DirectoryInfo(folderPath);
        foreach (var file in directory.GetFiles()) {
            file.Delete();
        }

        foreach (var dir in directory.GetDirectories()) {
            dir.Delete(true);
        }
    }

    public event Func<BeforeExitArgs>? CallBeforeExit;

    public BeforeExitArgs NeedConfirmBeforeEndApp() {
        if (CallBeforeExit == null) {
            return BeforeExitArgs.Empty;
        }

        var finalResult = false;
        List<string> senderList = [];
        try {
            foreach (var @delegate in CallBeforeExit.GetInvocationList()) {
                var handler = (Func<BeforeExitArgs>)@delegate;
                var handlerResult = handler.Invoke();
                if (handlerResult.NeedConfirm) {
                    finalResult = true;
                    senderList.Add(handlerResult.Sender?.ToString() ?? string.Empty);
                }
            }
        } catch (Exception ex) {
            _logger.Error("处理器异常: {ExMessage}", ex.Message);
            finalResult = false;
        }

        return new BeforeExitArgs(finalResult, senderList.Count > 0 ? string.Join(", ", senderList) : "None");
    }

    public void EndApp() {
        DisposePageWhenExit();
        ClearFolderParallel(GlobalSettings.TempDirectory);
    }

    public void Hide() {
        if (Application.Current.MainWindow!.Visibility != Visibility.Hidden) {
            Application.Current.MainWindow!.Hide();
        }
    }

    public void Show() {
        if (Application.Current.MainWindow!.Visibility != Visibility.Visible) {
            Application.Current.MainWindow.Activate();
            Application.Current.MainWindow.Focus();
            Application.Current.MainWindow.Show();
        }
    }

    public void ExitApp() {
        Application.Current.Shutdown();
    }
}