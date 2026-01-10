// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using Serilog;

namespace AI_Interviewer.ViewModels.InterviewPage;

public partial class InterviewPageViewModel : ObservableObject {
    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // Logger
    private readonly ILogger _logger;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // 音频录制服务
    private readonly IAudioRecorderService _audioRecorderService;

    // 语音识别服务
    private readonly ISpeechRecognitionService _speechRecognitionService;

    public InterviewPageViewModel(IPreferencesService preferencesService, ILogger logger,
        SnackbarServiceHelper snackbarService, IAudioRecorderService audioRecorderService,
        ISpeechRecognitionService speechRecognitionService) {
        _preferencesService = preferencesService;
        _logger = logger;
        _snackbarService = snackbarService;
        _audioRecorderService = audioRecorderService;
        _speechRecognitionService = speechRecognitionService;
        Init();
        CustomQuestions.ListChanged += (_, _) => {
            OnPropertyChanged(nameof(HasCustomQuestions));
            OnPropertyChanged(nameof(MinQuestionCount));
            OnPropertyChanged(nameof(NeedQuestionCountWarning));
            if (InterviewQuestionCount < MinQuestionCount) {
                InterviewQuestionCount = MinQuestionCount;
            }

            _preferencesService.Set("CustomInterviewQuestions", CustomQuestions);
        };
    }

    public async Task Dispose() {
        await Save();
        _audioRecorderService.Dispose();
        InterviewDispose();
    }

    private async Task Save() {
        await _preferencesService.Set("CustomInterviewQuestions", CustomQuestions);
    }

    private void Init() {
        try {
            CustomQuestions =
                _preferencesService.Get("CustomInterviewQuestions", new BindingList<Question>())!;
            InterviewQuestionCount = CustomQuestions.Count;
            _audioRecorderService.Initialize(new RecorderConfiguration {
                SaveMode = SaveMode.SaveToFile,
                OutputFilePathBase = Path.Combine(GlobalSettings.AppDataDirectory, "Record")
            });
            _audioRecorderService.OnError += (_, e) => {
                _snackbarService.ShowError("音频录制错误，建议重新启动软件", $"{e.Operation} 发生错误: {e.Exception.Message}");
                _logger.Error("音频录制错误, {e.Operation} 发生错误: {ExMessage}", e.Operation, e.Exception.Message);
            };
            RefreshMicrophoneDevices();
        } catch (Exception e) {
            _snackbarService.ShowError("加载自定义面试题失败", $"错误: {e.Message}");
            _logger.Error("加载自定义面试题失败: {ExMessage}", e.Message);
        }
    }

    #region 准备页面

    [ObservableProperty] private bool _interviewing;

    [ObservableProperty] private DifficultyLevel _selectedDifficulty;

    [ObservableProperty] private BindingList<Question> _customQuestions = [];

    public bool HasCustomQuestions => CustomQuestions.Count > 0;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NeedQuestionCountWarning))]
    private int _interviewQuestionCount;

    public int MinQuestionCount => Math.Max(1, CustomQuestions.Count);

    public bool NeedQuestionCountWarning => InterviewQuestionCount < MinQuestionCount;

    [ObservableProperty] private double _microphoneVolume;

    [ObservableProperty] private ObservableCollection<MicrophoneDevice> _microphoneDevices = [];

    [ObservableProperty] private MicrophoneDevice? _selectedMicrophone;

    [ObservableProperty] private bool _isTestingMicrophone;

    [RelayCommand]
    private void AddCustomQuestion() {
        CustomQuestions.Add(new Question());
    }

    [RelayCommand]
    private void RemoveCustomQuestion(Question question) {
        CustomQuestions.Remove(question);
    }

    private static IEnumerable<MicrophoneDevice> EnumerateMicrophones() {
        for (int i = 0; i < WaveIn.DeviceCount; i++) {
            var caps = WaveIn.GetCapabilities(i);
            yield return new MicrophoneDevice { DeviceIndex = i, Name = caps.ProductName };
        }
    }

    [RelayCommand]
    private void RefreshMicrophoneDevices() {
        foreach (var device in EnumerateMicrophones()) {
            MicrophoneDevices.Add(device);
        }

        SelectedMicrophone = MicrophoneDevices.FirstOrDefault();
    }

    private static double CalculateRms(byte[] buffer, int bytesRecorded) {
        int samples = bytesRecorded / 2;
        if (samples == 0) return 0;

        double sum = 0;

        for (int i = 0; i < bytesRecorded; i += 2) {
            short sample = BitConverter.ToInt16(buffer, i);
            double normalized = sample / 32768.0;
            sum += normalized * normalized;
        }

        return Math.Sqrt(sum / samples);
    }

    partial void OnSelectedMicrophoneChanged(MicrophoneDevice? value) {
        if (value == null) {
            return;
        }

        _audioRecorderService.Configuration.InputDeviceIndex = value.DeviceIndex;

        if (IsTestingMicrophone) {
            StopTestMicrophone();
        }
    }

    [RelayCommand]
    private void StartTestMicrophone() {
        try {
            _audioRecorderService.OnDataAvailable += TestMicrophoneHandler;
            _audioRecorderService.StartRecording();
            IsTestingMicrophone = true;
        } catch (Exception e) {
            _logger.Error("错误: {ExMessage}", e.Message);
        }
    }

    private double _smoothedVolume;

    private void ShowVolume(byte[] buffer, int bytesRecorded) {
        double rms = CalculateRms(buffer, bytesRecorded);

        // 对数压缩
        double display = Math.Log10(1 + rms * 9);

        // 平滑
        _smoothedVolume = 0.85 * _smoothedVolume + 0.15 * display;

        MicrophoneVolume = _smoothedVolume;
    }

    private void TestMicrophoneHandler(object? sender, AudioDataAvailableEventArgs e) {
        if (e.IsFinal || e.BytesRecorded == 0) {
            return;
        }

        ShowVolume(e.AudioData, e.BytesRecorded);
    }

    [RelayCommand]
    private void StopTestMicrophone() {
        try {
            _audioRecorderService.OnDataAvailable -= TestMicrophoneHandler;
            _audioRecorderService.StopRecording();
            IsTestingMicrophone = false;
            MicrophoneVolume = 0;
        } catch (Exception e) {
            _logger.Error("错误: {ExMessage}", e.Message);
        }
    }

    [RelayCommand]
    private void StartInterview() {
        if (InterviewQuestionCount < MinQuestionCount || InterviewQuestionCount > 50) {
            _snackbarService.ShowError("面试题数量错误", "面试题数量必须在 " + MinQuestionCount + " 到 50 之间");
            return;
        }

        Interviewing = true;
        StopTestMicrophone();
        InterviewQuestions = [new Question { QuestionText = "测试题目114514" }, ..CustomQuestions];
        CurrentQuestionIndex = 0;
        InterviewPartInit();
    }

    #endregion

    #region 面试页面

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentQuestion), nameof(CurrentQuestionIndexText))]
    private int _currentQuestionIndex;

    public string CurrentQuestionIndexText => $"第 {CurrentQuestionIndex + 1} / {InterviewQuestions.Count} 题";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentQuestion), nameof(CurrentQuestionIndexText))]
    private BindingList<Question> _interviewQuestions = [];

    public Question CurrentQuestion => InterviewQuestions[CurrentQuestionIndex];

    [ObservableProperty] private bool _hasGeneratedQuestion;

    [ObservableProperty] private FollowUpDepthLevel _followUpDepth;

    [ObservableProperty] private string _currentAnswer = string.Empty;

    public long AnswerWordCount => CurrentAnswer.Length;

    [ObservableProperty] private bool _isSpeeching;

    [ObservableProperty] private string _speechRecognitionResult = string.Empty;

    private async void AudioPushHandler(object? sender, AudioDataAvailableEventArgs e) {
        try {
            ShowVolume(e.AudioData, e.BytesRecorded);
            await _speechRecognitionService.PushAudioAsync(e);
        } catch (Exception ex) {
            _logger.Error("错误: {ExMessage}", ex.Message);
        }
    }

    private readonly SparkTextDecoder _decoder = new();
    private string _finalText = string.Empty;

    private void SpeechRecognitionHandler(object? sender, SpeechRecognitionResultEventArgs e) {
        /*_logger.Debug(
            "sn={Sn}, text={Text}, pgs={Op}, rg={Rg}, ls={Ls}",
            e.Sn, e.Text, e.Operation, e.ReplaceRange, e.IsFinal);*/

        _decoder.Decode(e);

        if (!e.IsFinal) {
            SpeechRecognitionResult = _finalText + _decoder.GetText();
            return;
        }

        _finalText += _decoder.GetText();
        SpeechRecognitionResult = _finalText;
        _decoder.Reset();
    }


    private void SpeechRecognitionError(object? sender, SpeechRecognitionErrorEventArgs e) {
        _logger.Error("语音识别 {Op} 错误: {Message}", e.Operation, e.Exception.Message);
    }

    private void InterviewPartInit() {
        try {
            var appId = _preferencesService.Get("SparkAI/AppId", string.Empty)!;
            var apiKey = _preferencesService.Get("SparkAI/ApiKey", string.Empty)!;
            var apiSecret = _preferencesService.Get("SparkAI/ApiSecret", string.Empty)!;
            _speechRecognitionService.StartAsync(appId, apiKey, apiSecret);
            _audioRecorderService.OnDataAvailable += AudioPushHandler;
            _speechRecognitionService.OnResult += SpeechRecognitionHandler;
            _speechRecognitionService.OnError += SpeechRecognitionError;
        } catch (Exception e) {
            _snackbarService.ShowError("错误", "请检查语音识别服务是否正常启动");
            _logger.Error("语音识别服务启动失败，错误：{Message}", e.Message);
        }
    }

    private void InterviewDispose() {
        _audioRecorderService.OnDataAvailable -= AudioPushHandler;
        _audioRecorderService.StopRecording();
        _speechRecognitionService.OnResult -= SpeechRecognitionHandler;
        _speechRecognitionService.StopAsync();
    }

    [RelayCommand]
    private void StartSpeechRecognition() {
        if (IsSpeeching) {
            return;
        }

        IsSpeeching = true;

        _audioRecorderService.StartRecording();
    }

    [RelayCommand]
    private void StopSpeechRecognition() {
        if (!IsSpeeching) {
            return;
        }

        IsSpeeching = false;
        MicrophoneVolume = 0;

        _audioRecorderService.StopRecording();
    }

    [RelayCommand]
    private void ClearSpeechRecognition() {
        SpeechRecognitionResult = string.Empty;
    }

    [RelayCommand]
    private void StopAndBack() {
        Interviewing = false;
        IsSpeeching = false;
        InterviewDispose();
    }

    [RelayCommand]
    private void PreviousQuestion() {
        if (CurrentQuestionIndex > 0) {
            CurrentQuestionIndex--;
        }
    }

    [RelayCommand]
    private void NextQuestion() {
        if (CurrentQuestionIndex < InterviewQuestions.Count - 1) {
            CurrentQuestionIndex++;
        }
    }

    #endregion
}

public sealed class SparkTextDecoder {
    private SpeechRecognitionResultEventArgs?[] _texts;
    private int _capacity;

    public SparkTextDecoder(int initialCapacity = 10) {
        _capacity = initialCapacity;
        _texts = new SpeechRecognitionResultEventArgs[_capacity];
    }

    public void Decode(SpeechRecognitionResultEventArgs e) {
        if (e.Sn == null) {
            return;
        }

        int sn = e.Sn.Value;

        EnsureCapacity(sn);

        // rpl：标记删除
        if (e is { Operation: "rpl", ReplaceRange: not null }) {
            var (start, end) = e.ReplaceRange.Value;
            for (int i = start; i <= end; i++) {
                if (i >= 0 && i < _texts.Length && _texts[i] != null) {
                    _texts[i]!.Text = string.Empty;
                }
            }
        }

        _texts[sn] = e;
    }

    public string GetText() {
        var sb = new StringBuilder();
        foreach (var t in _texts) {
            if (t != null && !string.IsNullOrEmpty(t.Text)) {
                sb.Append(t.Text);
            }
        }

        return sb.ToString();
    }

    public void Reset() {
        Array.Clear(_texts);
    }

    private void EnsureCapacity(int sn) {
        if (sn < _capacity) return;

        while (sn >= _capacity) {
            _capacity <<= 1;
        }

        Array.Resize(ref _texts, _capacity);
    }
}