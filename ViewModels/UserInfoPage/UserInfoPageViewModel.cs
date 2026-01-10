// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

// ReSharper disable ConvertToPrimaryConstructor

namespace AI_Interviewer.ViewModels.UserInfoPage;

public partial class UserInfoPageViewModel : ObservableObject {
    [ObservableProperty] private Resume _resume = new();

    public bool NoEducations => Resume.Educations.Count == 0;

    public bool NoWorkExperiences => Resume.WorkExperiences.Count == 0;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // Logger
    private readonly ILogger _logger;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public UserInfoPageViewModel(IPreferencesService preferencesService, ILogger logger, SnackbarServiceHelper snackbarService) {
        _preferencesService = preferencesService;
        _logger = logger;
        _snackbarService = snackbarService;
        Resume = _preferencesService.Get("UserResume", new Resume())!;
        Resume.PropertyChanged += (_, _) => {
            OnPropertyChanged(nameof(NoEducations));
            OnPropertyChanged(nameof(NoWorkExperiences));
        };
    }

    public async Task Dispose() {
        await SaveResume();
    }

    [RelayCommand]
    private async Task SaveResume() {
        try {
            await _preferencesService.Set("UserResume", Resume);
            _snackbarService.ShowSuccess("保存成功", "简历保存成功");
        } catch (Exception e) {
            _logger.Error("保存失败: {ExMessage}", e.Message);
        }
    }

    [RelayCommand]
    private void AddEducation() {
        Resume.Educations.Add(new Education());
    }

    [RelayCommand]
    private void RemoveEducation(Education education) {
        Resume.Educations.Remove(education);
    }

    [RelayCommand]
    private void AddWorkExperience() {
        Resume.WorkExperiences.Add(new WorkExperience());
    }

    [RelayCommand]
    private void RemoveWorkExperience(WorkExperience workExperience) {
        Resume.WorkExperiences.Remove(workExperience);
    }
}