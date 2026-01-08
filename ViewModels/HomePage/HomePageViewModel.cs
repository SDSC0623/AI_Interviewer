// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AI_Interviewer.ViewModels.HomePage;

public partial class HomePageViewModel : ObservableObject {
    [ObservableProperty] private string _homePageTitle = "主页";

    [ObservableProperty] private string _markdownText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _multimodalFeatures = [
        "文本分析：NLP技术分析回答的专业性、逻辑性",
        "语音分析：实时语音识别与情感分析",
        "视觉分析：面部表情、头部姿态、眼神交流评估",
        "综合评分：多维度数据融合生成客观评分"
    ];

    [ObservableProperty]
    private ObservableCollection<string> _dialogueFeatures = [
        "大模型驱动的AI面试官",
        "动态生成个性化面试问题",
        "专业领域知识库支持"
    ];

    [ObservableProperty]
    private ObservableCollection<string> _reportFeatures = [
        "可视化能力雷达图",
        "详细改进建议",
        "历史趋势分析",
        "专业HTML报告"
    ];

    public HomePageViewModel() {

    }
}