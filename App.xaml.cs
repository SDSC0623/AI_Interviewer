// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;
using System.Windows.Interop;
using AI_Interviewer.Helpers;
using AI_Interviewer.Services;
using AI_Interviewer.Services.IServices;
using AI_Interviewer.ViewModels;
using AI_Interviewer.ViewModels.AskBeforeExitDialog;
using AI_Interviewer.ViewModels.HomePage;
using AI_Interviewer.ViewModels.SettingPage;
using AI_Interviewer.Views;
using AI_Interviewer.Views.Pages.HomePage;
using AI_Interviewer.Views.Pages.SettingPage;
using AI_Interviewer.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.RichTextBox.Abstraction;
using Vanara.PInvoke;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantExtendsListEntry

namespace AI_Interviewer;

public partial class App : Application {
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        // .UseAdmin()
        .EnsureSingleInstance("AI_Interviewer-SDSC0623")
        .EnsureNotInRoot().Result
        .EnsureNotInDesktop().Result
        .ConfigureLogging(logging => { logging.ClearProviders(); })
        .ConfigureServices((_, services) => {
// 日志
            var logFile = Path.Combine(GlobalSettings.LogDirectory, "log.txt");
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.File(logFile,
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} {SourceContext} {Exception}{NewLine}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning);
#if DEBUG
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Exception}{NewLine}{NewLine}");
#endif
            var richTextBox = new RichTextBoxImpl();
            services.AddSingleton<IRichTextBox>(richTextBox);

            loggerConfiguration.WriteTo.RichTextBox(richTextBox, LogEventLevel.Information);
            // OutputTemplate "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"

            Log.Logger = loggerConfiguration.CreateLogger();
            services.AddLogging(c => c.AddSerilog());
            services.AddSingleton(Log.Logger);

            // 导航服务提供器
            services.AddNavigationViewPageProvider();

            // 服务
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();
            services.AddSingleton<IPreferencesService, JsonPreferencesService>();

            // 特殊服务
            services.AddSingleton<SnackbarServiceHelper>(); // 弹窗服务
            services.AddSingleton<AppRunningHelper>(); // App运行操作服务

            // 窗口
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<AboutWindow>();

            // 页面
            // 主页
            services.AddSingleton<HomePageViewModel>();
            services.AddSingleton<HomePage>();
            // 设置页面
            services.AddSingleton<SettingPageViewModel>();
            services.AddSingleton<SettingPage>();

            // 问询对话框
            services.AddTransient<AskBeforeExitViewModel>();
            services.AddTransient<AskBeforeExit>();
        })
        .Build();

    private static IServiceProvider ServiceProvider => Host.Services;

    private Serilog.ILogger _logger = Log.Logger;

    public new static App Current => (App)Application.Current;

    protected override async void OnStartup(StartupEventArgs e) {
        try {
            base.OnStartup(e);

            await Host.StartAsync();

            _logger = GetService<Serilog.ILogger>()!;

            var mainWindow = GetService<MainWindow>()!;
            mainWindow.Show();

            GetService<AppRunningHelper>()!.StartApp();
            ApplicationThemeManager.Apply(GetService<IPreferencesService>()!.Get("ThemeIsDark", true)
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light);
        } catch (Exception ex) {
            _logger.Error("启动时发生错误: {ExMessage}", ex.Message);
        }
    }

    protected override async void OnExit(ExitEventArgs e) {
        try {
            GetService<AppRunningHelper>()!.EndApp();

            base.OnExit(e);

            await Host.StopAsync();

            Host.Dispose();
        } catch (Exception ex) {
            _logger.Error("退出时发生错误: {ExMessage}", ex.Message);
        }
    }

    public static T? GetService<T>() where T : class {
        return ServiceProvider.GetService(typeof(T)) as T;
    }

    public static object? GetService(Type type) {
        return ServiceProvider.GetService(type);
    }
}

internal static class StartExtension {
    public static IHostBuilder UseAdmin(this IHostBuilder app) {
        AppRunningHelper.EnsureAdmin();
        return app;
    }

    public static IHostBuilder EnsureSingleInstance(this IHostBuilder app, string instanceName,
        Action<bool>? callback = null) {
        if (Environment.GetCommandLineArgs().Contains("--no-single")) {
            return app;
        }

        EventWaitHandle? handle;
        try {
            handle = EventWaitHandle.OpenExisting(instanceName);
            handle.Set();
            callback?.Invoke(false);
            Environment.Exit('s' + 'i' + 'n' + 'g' + 'l' + 'e' + 'n' + 'c' + 'e');
        } catch (WaitHandleCannotBeOpenedException) {
            callback?.Invoke(true);
            handle = new EventWaitHandle(false, EventResetMode.AutoReset, instanceName);
        }

        _ = Task.Factory.StartNew(() => {
            while (handle.WaitOne()) {
                App.Current.Dispatcher?.BeginInvoke(() => {
                    App.Current.MainWindow?.Show();
                    App.Current.MainWindow?.Activate();
                    var hWnd = new WindowInteropHelper(App.Current.MainWindow!).Handle;
                    if (User32.IsWindow(hWnd)) {
                        _ = User32.SendMessage(hWnd, User32.WindowMessage.WM_SYSCOMMAND, User32.SysCommand.SC_RESTORE);
                        _ = User32.SetForegroundWindow(hWnd);

                        if (User32.IsIconic(hWnd)) {
                            _ = User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
                        }

                        _ = User32.BringWindowToTop(hWnd);
                        _ = User32.SetActiveWindow(hWnd);
                    }
                });
            }
        }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        return app;
    }

    public static async Task<IHostBuilder> EnsureNotInRoot(this IHostBuilder app) {
        if (GlobalSettings.BaseDirectory.Split(Path.DirectorySeparatorChar).Length == 1 ||
            new DirectoryInfo(GlobalSettings.BaseDirectory).Parent == null) {
            var messageBox = new Wpf.Ui.Controls.MessageBox {
                Title = "程序主动停止",
                Content = "请不要在根目录下运行程序，你应当把本程序放在一个文件夹内运行。",
                CloseButtonText = "确认"
            };
            await messageBox.ShowDialogAsync();
            Environment.Exit('r' + 'u' + 'n' + 'i' + 'n' + 'r' + 'o' + 'o' + 't');
        }


        return app;
    }

    public static async Task<IHostBuilder> EnsureNotInDesktop(this IHostBuilder app) {
        if (Path.TrimEndingDirectorySeparator(GlobalSettings.BaseDirectory) ==
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop)) {
            var messageBox = new Wpf.Ui.Controls.MessageBox {
                Title = "程序主动停止",
                Content = "请不要在桌面运行本程序，你应当把本程序放在一个独立文件夹内运行。",
                CloseButtonText = "确认"
            };
            await messageBox.ShowDialogAsync();
            Environment.Exit('r' + 'u' + 'n' + 'i' + 'n' + 'd' + 'e' + 's' + 'k' + 't' + 'o' + 'p');
        }

        return app;
    }
}