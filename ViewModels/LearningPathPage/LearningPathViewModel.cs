// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text;
using AI_Interviewer.Helpers;
using AI_Interviewer.Services.IServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AI_Interviewer.ViewModels.LearningPathPage;

public partial class LearningPathViewModel : ObservableObject {
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string _reportContent = string.Empty;
    [ObservableProperty] private string _reportPreview = "暂未生成";
    [ObservableProperty] private string _htmlFilePath = string.Empty;

    public bool HasHtmlReport => !string.IsNullOrEmpty(HtmlFilePath);

    // Logger
    private readonly ILogger _logger;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 面试回答保存服务
    private readonly IInterviewAnswerSaveService _interviewAnswerSaveService;

    // 学习路径服务
    private readonly ILearningPathService _learningPathService;

    // 内容对话框服务
    private readonly IContentDialogService _contentDialogService;

    public LearningPathViewModel(IPreferencesService preferencesService, ILogger logger,
        SnackbarServiceHelper snackbarService, IInterviewAnswerSaveService interviewAnswerSaveService,
        ILearningPathService learningPathService, IContentDialogService contentDialogService) {
        _preferencesService = preferencesService;
        _logger = logger;
        _snackbarService = snackbarService;
        _interviewAnswerSaveService = interviewAnswerSaveService;
        _learningPathService = learningPathService;
        _contentDialogService = contentDialogService;

        Init();
    }

    private void Init() {
        var appId = _preferencesService.Get("SparkAI/AppId", string.Empty)!;
        var apiKey = _preferencesService.Get("SparkAI/ApiKey", string.Empty)!;
        var apiSecret = _preferencesService.Get("SparkAI/ApiSecret", string.Empty)!;

        _learningPathService.Init(appId, apiKey, apiSecret);
    }

    [RelayCommand]
    private async Task GenerateReport() {
        try {
            IsGenerating = true;
            var answers = _interviewAnswerSaveService.GetAllAnswer();

            var result = await _learningPathService.GenerateLearningPathAsync(answers);

            ReportContent = result.Markdown;
            ReportPreview = BuildPreview(ReportContent);
            HtmlFilePath = result.HtmlPath;

            OnPropertyChanged(nameof(HasHtmlReport));
            _snackbarService.ShowSuccess("提示", "学习路径报告已生成");
        } catch (Exception e) {
            _snackbarService.ShowError("错误", $"生成学习路径报告时出错：{e.Message}");
            _logger.Error(e, "生成学习路径报告时出错");
        } finally {
            IsGenerating = false;
        }
    }

    private static string BuildPreview(string markdown) {
        var lines = markdown.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Take(30)
            .Select(l => l.Replace("#", "").Replace("*", ""));

        return string.Join(Environment.NewLine, lines)
               + "\n\n…（完整内容请在浏览器中查看）";
    }

    [RelayCommand]
    private void OpenInBrowser() {
        if (!File.Exists(HtmlFilePath)) {
            _snackbarService.ShowError("错误", "HTML文件不存在");
            return;
        }

        Process.Start(new ProcessStartInfo {
            FileName = HtmlFilePath,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task SaveMarkdownReport() {
        if (string.IsNullOrWhiteSpace(ReportContent)) {
            _snackbarService.ShowError("错误", "没有内容可保存");
            return;
        }

        SaveFileDialog dialog = new() {
            FileName = "学习路径报告.md",
            DefaultExt = ".md",
            Filter = "Markdown文件|*.md",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() != true) {
            return;
        }

        await File.WriteAllTextAsync(dialog.FileName, ReportContent, Encoding.UTF8);
    }

    [RelayCommand]
    private async Task SaveHtmlReport() {
        if (!File.Exists(HtmlFilePath)) {
            _snackbarService.ShowError("错误", "HTML文件不存在");
            return;
        }

        OpenFolderDialog dialog = new() {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() != true) {
            return;
        }

        if (!Directory.Exists(dialog.FolderName)) {
            _snackbarService.ShowError("错误", "目录不存在");
            return;
        }

        var targetPath = Path.Combine(dialog.FolderName, Path.GetFileName(HtmlFilePath));

        var flag = false;
        if (File.Exists(targetPath)) {
            var result = await _contentDialogService.ShowAsync(new ContentDialog {
                Title = "操作暂停",
                Content = "当前文件已存在，是否覆盖？",
                PrimaryButtonText = "是",
                PrimaryButtonAppearance = ControlAppearance.Danger,
                SecondaryButtonText = "另存",
                SecondaryButtonAppearance = ControlAppearance.Success,
                CloseButtonText = "取消保存"
            }, CancellationToken.None);
            switch (result) {
                case ContentDialogResult.Primary:
                    flag = true;
                    break;
                case ContentDialogResult.Secondary:
                    targetPath = Path.Combine(dialog.FolderName,
                        Path.GetFileName(HtmlFilePath) + $"{Guid.NewGuid()}.html");
                    break;
                case ContentDialogResult.None:
                default:
                    return;
            }
        }

        File.Copy(HtmlFilePath, targetPath, flag);
    }
}