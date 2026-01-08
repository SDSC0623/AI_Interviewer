// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AI_Interviewer.Models;

public partial class Resume : ObservableObject {
    [ObservableProperty] private BasicInfo _basicInfo = new();
    [ObservableProperty] private JobIntention _jobIntention = new();
    [ObservableProperty] private ObservableCollection<Education> _educations = [];
    [ObservableProperty] private ObservableCollection<WorkExperience> _workExperiences = [];

    public Resume() {
        BasicInfo.PropertyChanged += HandlePropertyChanged;
        JobIntention.PropertyChanged += HandlePropertyChanged;
        Educations.CollectionChanged += HandlePropertyChanged;
        WorkExperiences.CollectionChanged += HandlePropertyChanged;
    }

    private void HandlePropertyChanged(object? sender, EventArgs e) {
        OnPropertyChanged(sender?.ToString());
    }
}

public enum Gender {
    [Description("男")] Male,
    [Description("女")] Female
}

public partial class BasicInfo : ObservableObject {
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private Gender _gender = Gender.Male;
    [ObservableProperty] private string _age = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _location = string.Empty;
    [ObservableProperty] private string _ethnicity = string.Empty;
}

public enum JobType {
    [Description("全职")] FullTime,
    [Description("兼职")] PartTime,
    [Description("实习")] Internship
}

public partial class JobIntention : ObservableObject {
    [ObservableProperty] private string _targetPosition = string.Empty;
    [ObservableProperty] private string _targetCity = string.Empty;
    [ObservableProperty] private string _expectedSalary = string.Empty;
    [ObservableProperty] private JobType _jobType = JobType.FullTime;
}

public enum Degree {
    [Description("中专")] MiddleSchool,
    [Description("大专")] College,
    [Description("本科")] Undergraduate,
    [Description("硕士")] Master,
    [Description("博士")] Doctor
}

public partial class Education : ObservableObject {
    [ObservableProperty] private DateTime _startTime = DateTime.Now;
    [ObservableProperty] private DateTime _endTime = DateTime.Now;
    [ObservableProperty] private string _school = string.Empty;
    [ObservableProperty] private string _major = string.Empty;
    [ObservableProperty] private Degree _degree = Degree.MiddleSchool;
}

public partial class WorkExperience : ObservableObject {
    [ObservableProperty] private DateTime _startTime = DateTime.Now;
    [ObservableProperty] private DateTime _endTime = DateTime.Now;
    [ObservableProperty] private string _company = string.Empty;
    [ObservableProperty] private string _position = string.Empty;
    [ObservableProperty] private string _industry = string.Empty;
    [ObservableProperty] private string _salary = string.Empty;
}