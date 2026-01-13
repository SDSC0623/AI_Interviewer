// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AI_Interviewer.ViewModels.InterviewResultAnalysisPage;

public partial class InterviewResultAnalysisViewModel : ObservableObject {
    [ObservableProperty] private BindingList<InterviewAnswer> _interviewHistorys = [];

    [ObservableProperty] private bool _hasSelectedInterview;

    [ObservableProperty] private InterviewAnswer _selectedInterview = null!;

    // 面试回答保存服务
    private readonly IInterviewAnswerSaveService _interviewAnswerSaveService;

    public InterviewResultAnalysisViewModel(IInterviewAnswerSaveService interviewAnswerSaveService) {
        _interviewAnswerSaveService = interviewAnswerSaveService;
        Init();
    }

    private void Init() {
        RefreshInterviewAnswer();
    }

    [RelayCommand]
    private void RefreshInterviewAnswer() {
        HasSelectedInterview = false;
        SelectedInterview = null!;
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
}