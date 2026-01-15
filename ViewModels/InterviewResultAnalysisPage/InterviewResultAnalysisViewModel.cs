// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace AI_Interviewer.ViewModels.InterviewResultAnalysisPage;

public partial class InterviewResultAnalysisViewModel : ObservableObject {
    [ObservableProperty] private BindingList<InterviewAnswer> _interviewHistorys = [];

    [ObservableProperty] private bool _hasSelectedInterview;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasResult), nameof(ShowNoResult))]
    private InterviewAnswer _selectedInterview = new() {
        Time = DateTime.MinValue,
        HasResult = false
    };

    [ObservableProperty] private bool _isGeneratingResult;

    public bool HasResult => SelectedInterview.HasResult && !IsGeneratingResult;

    public bool ShowNoResult => !SelectedInterview.HasResult && !IsGeneratingResult;

    // Logger
    private readonly ILogger _logger;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 面试回答保存服务
    private readonly IInterviewAnswerSaveService _interviewAnswerSaveService;

    // 面试结果分析服务
    private readonly IInterviewResultService _interviewResultAnalysisService;

    public InterviewResultAnalysisViewModel(IInterviewAnswerSaveService interviewAnswerSaveService,
        IInterviewResultService interviewResultAnalysisService, IPreferencesService preferencesService, ILogger logger,
        SnackbarServiceHelper snackbarService) {
        _interviewAnswerSaveService = interviewAnswerSaveService;
        _interviewResultAnalysisService = interviewResultAnalysisService;
        _preferencesService = preferencesService;
        _logger = logger;
        _snackbarService = snackbarService;
        Init();
    }

    private void Init() {
        RefreshInterviewAnswer();
        var appId = _preferencesService.Get("SparkAI/AppId", string.Empty)!;
        var apiKey = _preferencesService.Get("SparkAI/ApiKey", string.Empty)!;
        var apiSecret = _preferencesService.Get("SparkAI/ApiSecret", string.Empty)!;
        _interviewResultAnalysisService.Init(appId, apiKey, apiSecret);
    }

    private void ResetSelected() {
        SelectedInterview = new InterviewAnswer {
            Time = DateTime.MinValue,
            HasResult = false
        };
    }

    [RelayCommand]
    private void RefreshInterviewAnswer() {
        HasSelectedInterview = false;
        ResetSelected();
        InterviewHistorys = [.._interviewAnswerSaveService.GetAllAnswer()];
    }

    [RelayCommand]
    private void SelectInterview(InterviewAnswer interviewAnswer) {
        if (SelectedInterview == interviewAnswer) {
            return;
        }

        SelectedInterview = interviewAnswer;
        HasSelectedInterview = true;
    }

    [RelayCommand]
    private void OpenMenu(InterviewAnswer interviewAnswer) {
        interviewAnswer.IsMenuOpen = true;
    }

    [RelayCommand]
    private void DeleteInterview(InterviewAnswer interviewAnswer) {
        if (SelectedInterview == interviewAnswer) {
            ResetSelected();
            HasSelectedInterview = false;
        }

        InterviewHistorys.Remove(interviewAnswer);
        try {
            _interviewAnswerSaveService.DeleteAnswer(interviewAnswer.Name);
        } catch (Exception e) {
            _snackbarService.ShowError("删除面试记录失败", $"错误：{e.Message}");
            _logger.Error(e, "删除面试记录失败");
        }
    }

    [RelayCommand]
    private async Task GenerateAnalysisReport() {
        try {
            IsGeneratingResult = true;
            SelectedInterview.InterviewAnalysisResult = new InterviewAnalysisResult();
            SelectedInterview.HasResult = false;
            OnPropertyChanged(nameof(SelectedInterview));
            OnPropertyChanged(nameof(HasResult));
            OnPropertyChanged(nameof(ShowNoResult));
            var result = await _interviewResultAnalysisService.GenerateAnalysisAsync(SelectedInterview);
            SelectedInterview.InterviewAnalysisResult = result;
            SelectedInterview.HasResult = true;
            _interviewAnswerSaveService.SaveAnswer(SelectedInterview);
            IsGeneratingResult = false;
            OnPropertyChanged(nameof(SelectedInterview));
            OnPropertyChanged(nameof(HasResult));
            OnPropertyChanged(nameof(ShowNoResult));
            _snackbarService.ShowSuccess("AI面试结果分析成功", "可以查看面试结果");
        } catch (Exception e) {
            _snackbarService.ShowError("AI面试结果分析失败", $"错误：{e.Message}");
            _logger.Error(e, "AI面试结果分析失败");
        }
    }
}