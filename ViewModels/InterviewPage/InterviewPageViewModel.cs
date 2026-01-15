// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Controls;
using ErrorEventArgs = AI_Interviewer.Models.ErrorEventArgs;

namespace AI_Interviewer.ViewModels.InterviewPage;

public partial class InterviewPageViewModel : ObservableObject {
    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // Logger
    private readonly ILogger _logger;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // 问题生成服务
    private readonly IInterviewQuestionService _interviewQuestionService;

    // 摄像头录制服务
    private readonly ICameraRecorderService _cameraRecorderService;

    // 面部分析服务
    private readonly IEmotionAnalysisService _emotionAnalysisService;

    // 音频录制服务
    private readonly IAudioRecorderService _audioRecorderService;

    // 语音识别服务
    private readonly ISpeechRecognitionService _speechRecognitionService;

    // 面试回答保存服务
    private readonly IInterviewAnswerSaveService _interviewAnswerSaveService;

    // 内容对话框服务
    private readonly IContentDialogService _contentDialogService;

    public InterviewPageViewModel(IPreferencesService preferencesService, ILogger logger,
        SnackbarServiceHelper snackbarService, IAudioRecorderService audioRecorderService,
        ISpeechRecognitionService speechRecognitionService, IInterviewAnswerSaveService interviewAnswerSaveService,
        ICameraRecorderService cameraRecorderService, IEmotionAnalysisService emotionAnalysisService,
        IInterviewQuestionService interviewQuestionService, IContentDialogService contentDialogService) {
        _preferencesService = preferencesService;
        _logger = logger;
        _snackbarService = snackbarService;
        _audioRecorderService = audioRecorderService;
        _speechRecognitionService = speechRecognitionService;
        _interviewAnswerSaveService = interviewAnswerSaveService;
        _cameraRecorderService = cameraRecorderService;
        _emotionAnalysisService = emotionAnalysisService;
        _interviewQuestionService = interviewQuestionService;
        _contentDialogService = contentDialogService;
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

    private void Save() {
        _preferencesService.Set("CustomInterviewQuestions", CustomQuestions);
    }

    private void AudioRecorderErrorHandler(object? sender, ErrorEventArgs e) {
        _snackbarService.ShowError("音频录制错误，建议重新启动软件", $"{e.Operation} 发生错误: {e.Exception.Message}");
        _logger.Error("音频录制错误, {e.Operation} 发生错误: {ExMessage}", e.Operation, e.Exception.Message);
    }

    private void CameraRecorderErrorHandler(object? sender, ErrorEventArgs e) {
        App.Current.Dispatcher.Invoke(() => {
            _snackbarService.ShowError("摄像头错误", $"{e.Operation} 发生错误: {e.Exception.Message}");
            _logger.Error("摄像头错误, {e.Operation} 发生错误: {ExMessage}", e.Operation, e.Exception.Message);
        });
    }

    private void SpeechRecognitionError(object? sender, ErrorEventArgs e) {
        _snackbarService.ShowError($"语音识别 {e.Operation} 错误", $"错误信息: {e.Exception.Message}");
        _logger.Error("语音识别 {Op} 错误: {Message}", e.Operation, e.Exception.Message);
    }

    private void EmotionAnalysisErrorHandler(object? sender, ErrorEventArgs e) {
        App.Current.Dispatcher.Invoke(() => {
            _snackbarService.ShowError("面部分析错误", $"{e.Operation} 发生错误: {e.Exception.Message}");
            _logger.Error("面部分析错误, {e.Operation} 发生错误: {ExMessage}", e.Operation, e.Exception.Message);
        });
    }

    private void Init() {
        try {
            _audioRecorderService.Initialize(new RecorderConfiguration {
                SaveMode = SaveMode.DoNotSave,
                OutputFilePathBase = Path.Combine(GlobalSettings.AppDataDirectory, "Record")
            });
            _audioRecorderService.OnDataAvailable += MicrophoneVolumeHandler;
            _audioRecorderService.OnError += AudioRecorderErrorHandler;
            _cameraRecorderService.OnFrameArrived += CameraRecorderHandler;
            _cameraRecorderService.OnError += CameraRecorderErrorHandler;
            _emotionAnalysisService.OnError += EmotionAnalysisErrorHandler;
            _speechRecognitionService.OnError += SpeechRecognitionError;
            App.GetService<AppRunningHelper>()!.CallBeforeExit += () => new BeforeExitArgs(Interviewing, "面试");
            PreparePartInit();
            InterviewPartInit();
        } catch (Exception e) {
            _snackbarService.ShowError("加载自定义面试题失败", $"错误: {e.Message}");
            _logger.Error("加载自定义面试题失败: {ExMessage}", e.Message);
        }
    }

    public void Dispose() {
        _audioRecorderService.OnDataAvailable += MicrophoneVolumeHandler;
        _audioRecorderService.OnError -= AudioRecorderErrorHandler;
        _cameraRecorderService.OnFrameArrived -= CameraRecorderHandler;
        _cameraRecorderService.OnError -= CameraRecorderErrorHandler;
        _emotionAnalysisService.OnError -= EmotionAnalysisErrorHandler;
        _speechRecognitionService.OnError -= SpeechRecognitionError;
        Save();
        PreparePartDispose();
        InterviewPartDispose();
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

    [ObservableProperty] private ImageSource _cameraPicture = null!;

    [ObservableProperty] private ObservableCollection<CameraDevice> _cameraDevices = [];

    [ObservableProperty] private CameraDevice? _selectedCamera;

    [ObservableProperty] private bool _isTestingCamera;

    [ObservableProperty] private bool _needMirrorHorizontal = true;

    public bool IsCameraActive => _cameraRecorderService.IsRunning;

    [ObservableProperty] private double _microphoneVolume;

    private double _smoothedVolume;

    [ObservableProperty] private ObservableCollection<MicrophoneDevice> _microphoneDevices = [];

    [ObservableProperty] private MicrophoneDevice? _selectedMicrophone;

    [ObservableProperty] private bool _isTestingMicrophone;

    [ObservableProperty] private bool _canStartInterviewing = true;

    private void PreparePartInit() {
        CustomQuestions = _preferencesService.Get("CustomInterviewQuestions", new BindingList<Question>())!;
        InterviewQuestionCount = CustomQuestions.Count;
        _cameraRecorderService.SetMirrorHorizontal(NeedMirrorHorizontal);
        RefreshMicrophoneDevices();
        RefreshCameraDevices();
    }

    private void PreparePartDispose() {
        StopCamera();
        _audioRecorderService.Dispose();
    }

    [RelayCommand]
    private void AddCustomQuestion() {
        CustomQuestions.Add(new Question());
    }

    [RelayCommand]
    private void RemoveCustomQuestion(Question question) {
        CustomQuestions.Remove(question);
    }

    private void CameraRecorderHandler(object? sender, CameraFrameEventArgs e) {
        App.Current.Dispatcher.Invoke(() => {
            CameraPicture =
                BitmapSource.Create(e.Width, e.Height, 96, 96, PixelFormats.Bgr24, null, e.BgrData, e.Width * 3);
            _emotionAnalysisService.SubmitFrame(e.BgrData, e.Width, e.Height);
        });
    }

    [RelayCommand]
    private void RefreshCameraDevices() {
        CameraDevices.Clear();
        foreach (var cameraDevice in ICameraRecorderService.EnumerateCameras()) {
            CameraDevices.Add(cameraDevice);
        }

        SelectedCamera = CameraDevices.FirstOrDefault();
    }

    private void StartCamera() {
        var fps = _preferencesService.Get("CameraFps", 30.0);
        _cameraRecorderService.Start(fps, SelectedCamera?.Index ?? -1);
        OnPropertyChanged(nameof(IsCameraActive));
    }

    private Task StartCameraAsync() {
        return Task.Run(StartCamera);
    }

    private void StopCamera() {
        _cameraRecorderService.Stop();
        CameraPicture = null!;
        OnPropertyChanged(nameof(IsCameraActive));
    }

    private Task StopCameraAsync() {
        return Task.Run(StopCamera);
    }

    [RelayCommand]
    private async Task StartCameraTest() {
        if (_cameraRecorderService.IsRunning) {
            return;
        }

        if (SelectedCamera == null) {
            _snackbarService.ShowError("无摄像头", "未找到摄像头");
            return;
        }

        try {
            await StartCameraAsync();
            IsTestingCamera = true;
        } catch (Exception e) {
            _snackbarService.ShowError("摄像头测试失败", $"错误: {e.Message}");
            _logger.Error("摄像头测试失败: {ExMessage}", e.Message);
        }
    }

    [RelayCommand]
    private async Task StopCameraTest() {
        if (!_cameraRecorderService.IsRunning) {
            return;
        }

        await StopCameraAsync();
        IsTestingCamera = false;
    }

    async partial void OnSelectedCameraChanged(CameraDevice? oldValue, CameraDevice? newValue) {
        try {
            if (oldValue != newValue) {
                await StopCameraAsync();
                IsTestingCamera = false;
            }
        } catch (Exception e) {
            _snackbarService.ShowError("关闭摄像头是发生错误", $"错误: {e.Message}");
            _logger.Error("关闭摄像头是发生错误: {ExMessage}", e.Message);
        }
    }

    [RelayCommand]
    private void RefreshMicrophoneDevices() {
        MicrophoneDevices.Clear();
        foreach (var device in IAudioRecorderService.EnumerateMicrophones()) {
            MicrophoneDevices.Add(device);
        }

        SelectedMicrophone = MicrophoneDevices.FirstOrDefault();
    }

    partial void OnNeedMirrorHorizontalChanged(bool value) {
        _cameraRecorderService.SetMirrorHorizontal(value);
    }

    private void ShowVolume(byte[] buffer, int bytesRecorded) {
        var rms = CommonHelper.CalculateRms(buffer, bytesRecorded);

        var display = Math.Log10(1 + rms * 9);

        _smoothedVolume = 0.85 * _smoothedVolume + 0.15 * display;

        MicrophoneVolume = _smoothedVolume;
    }

    private void MicrophoneVolumeHandler(object? sender, AudioDataAvailableEventArgs e) {
        if (e.IsFinal || e.BytesRecorded == 0) {
            return;
        }

        ShowVolume(e.AudioData, e.BytesRecorded);
    }

    private void StartMicrophone() {
        _audioRecorderService.StartRecording();
    }

    private Task StartMicrophoneAsync() {
        return Task.Run(StartMicrophone);
    }

    private void StopMicrophone() {
        _audioRecorderService.StopRecording();
        MicrophoneVolume = 0;
    }

    private Task StopMicrophoneAsync() {
        return Task.Run(StopMicrophone);
    }

    [RelayCommand]
    private async Task StartTestMicrophone() {
        try {
            await StartMicrophoneAsync();
            IsTestingMicrophone = true;
        } catch (Exception e) {
            _logger.Error("错误: {ExMessage}", e.Message);
        }
    }

    [RelayCommand]
    private async Task StopTestMicrophone() {
        try {
            await StopMicrophoneAsync();
            IsTestingMicrophone = false;
        } catch (Exception e) {
            _logger.Error("错误: {ExMessage}", e.Message);
        }
    }

    async partial void OnSelectedMicrophoneChanged(MicrophoneDevice? value) {
        try {
            if (value == null) {
                return;
            }

            _audioRecorderService.Configuration.InputDeviceIndex = value.DeviceIndex;

            if (IsTestingMicrophone) {
                await StopTestMicrophone();
            }
        } catch (Exception e) {
            _snackbarService.ShowError("关闭麦克风失败", $"错误: {e.Message}");
            _logger.Error("关闭麦克风失败，错误: {ExMessage}", e.Message);
        }
    }

    [RelayCommand]
    private async Task StartInterview() {
        if (InterviewQuestionCount < MinQuestionCount || InterviewQuestionCount > 50) {
            _snackbarService.ShowError("面试题数量错误", "面试题数量必须在 " + MinQuestionCount + " 到 50 之间");
            return;
        }

        await StopTestMicrophone();
        IsTestingCamera = false;
        await InterviewStart();
        CanStartInterviewing = false;
    }

    #endregion

    #region 面试页面

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousQuestion), nameof(HasNextQuestion)
        , nameof(CurrentQuestion), nameof(CurrentQuestionIndexText))]
    private int _currentQuestionIndex;

    public bool HasPreviousQuestion => CurrentQuestionIndex > 0 && HasGeneratedQuestion && HasGeneratedFollowUpQuestion;

    public bool HasNextQuestion => CurrentQuestionIndex < InterviewQuestions.Count - 1 && HasGeneratedQuestion &&
                                   HasGeneratedFollowUpQuestion;

    private string _interviewName = string.Empty;

    public string CurrentQuestionIndexText => $"第 {CurrentQuestionIndex + 1} / {InterviewQuestions.Count} 题";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentQuestionIndexText), nameof(CurrentQuestion))]
    private BindingList<Question> _interviewQuestions = [new() { QuestionText = "占位问题" }];

    public Question CurrentQuestion => InterviewQuestions[CurrentQuestionIndex];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousQuestion), nameof(HasNextQuestion), nameof(CanSaveOrFinish))]
    private bool _hasGeneratedQuestion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousQuestion), nameof(HasNextQuestion), nameof(CanSaveOrFinish))]
    private bool _hasGeneratedFollowUpQuestion = true;

    public bool CanSaveOrFinish => HasGeneratedQuestion && HasGeneratedFollowUpQuestion;

    [ObservableProperty] private FollowUpDepthLevel _followUpDepth;

    [ObservableProperty] private string _currentQuestionAnswer = string.Empty;

    [ObservableProperty] private bool _isSpeeching;

    [ObservableProperty] private string _speechRecognitionResult = string.Empty;

    [ObservableProperty] private EmotionType _facialEmotion;

    // [ObservableProperty] private HeadPose _headPose;

    // [ObservableProperty] private GazeDirection _gazeDirection;

    private readonly SparkTextDecoder _decoder = new();

    private readonly EmotionStatisticsCollector _emotionCollector = new();

    private string _finalText = string.Empty;

    private string _appId = string.Empty;
    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;

    private void InterviewPartInit() {
        _audioRecorderService.OnDataAvailable += AudioPushHandler;
        _speechRecognitionService.OnResult += SpeechRecognitionHandler;
        _emotionAnalysisService.OnResultUpdated += EmotionAnaysisResultUpdate;
    }

    private void InterviewPartDispose() {
        _audioRecorderService.OnDataAvailable -= AudioPushHandler;
        _speechRecognitionService.OnResult -= SpeechRecognitionHandler;
        _emotionAnalysisService.OnResultUpdated -= EmotionAnaysisResultUpdate;
        StopCamera();
        SpeechStop();
    }

    partial void OnInterviewQuestionsChanged(BindingList<Question>? oldValue, BindingList<Question> newValue) {
        if (oldValue != newValue) {
            InterviewQuestions.ListChanged += (_, _) => {
                OnPropertyChanged(nameof(CurrentQuestion));
                OnPropertyChanged(nameof(CurrentQuestionIndexText));
            };
        }
    }

    private async Task InterviewStart() {
        try {
            _interviewName = $"面试-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            _appId = _preferencesService.Get("SparkAI/AppId", string.Empty)!;
            _apiKey = _preferencesService.Get("SparkAI/ApiKey", string.Empty)!;
            _apiSecret = _preferencesService.Get("SparkAI/ApiSecret", string.Empty)!;
            if (!_cameraRecorderService.IsRunning && CameraDevices.Count > 0) {
                await StartCameraAsync();
            }

            _emotionAnalysisService.Start();
            Interviewing = true;
            _cts = new CancellationTokenSource();

            _interviewQuestionService.Init(_appId, _apiKey, _apiSecret);
            var resume = _preferencesService.Get("UserResume", new Resume())!;
            InterviewQuestions = [
                ..CustomQuestions.Select(question => {
                    question.QuestionText += " (用户自定义题目)";
                    return question;
                }),
                ..await _interviewQuestionService.GenerateQuestionsAsync(resume, SelectedDifficulty,
                    InterviewQuestionCount - CustomQuestions.Count, _cts.Token)
            ];
            CurrentQuestionIndex = 0;
            HasGeneratedQuestion = true;
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            _snackbarService.ShowError("面试启动失败", $"错误: {e.Message}");
            _logger.Error("面试启动失败，错误: {ExMessage}", e.Message);
        }
    }

    private async Task InterviewStop(bool needConfirm) {
        var result = ContentDialogResult.Primary;
        if (needConfirm) {
            result = await _contentDialogService.ShowAsync(new ContentDialog {
                Title = "是否要保存本次面试回答？",
                Content = "保存后可以查看历史面试记录",
                PrimaryButtonIcon = new SymbolIcon(SymbolRegular.Save24),
                PrimaryButtonAppearance = ControlAppearance.Success,
                PrimaryButtonText = "保存",
                SecondaryButtonIcon = new SymbolIcon(SymbolRegular.Dismiss24),
                SecondaryButtonText = "不保存",
                SecondaryButtonAppearance = ControlAppearance.Danger,
                CloseButtonText = "取消退出"
            }, CancellationToken.None);
        }

        switch (result) {
            case ContentDialogResult.Primary:
                SaveAnswer();
                break;
            case ContentDialogResult.Secondary:
                _interviewAnswerSaveService.DeleteAnswer(_interviewName);
                break;
            case ContentDialogResult.None:
            default:
                return;
        }

        CurrentQuestionAnswer = string.Empty;
        _interviewName = string.Empty;
        _appId = string.Empty;
        _apiKey = string.Empty;
        _apiSecret = string.Empty;
        InterviewQuestions = [new Question { QuestionText = "占位问题" }];
        HasGeneratedQuestion = false;
        HasGeneratedFollowUpQuestion = true;

        await _cts.CancelAsync();
        ClearSpeechRecognition();
        _emotionAnalysisService.Stop();
        _ = SpeechStopAsync();
        _ = StopCameraAsync();
        IsSpeeching = false;
        Interviewing = false;
        _ = Task.Run(async () => {
            await Task.Delay(5000);
            CanStartInterviewing = true;
        });
    }

    private CancellationTokenSource _cts = new();

    [RelayCommand]
    private async Task GenerateFollowUpQuestion() {
        try {
            HasGeneratedFollowUpQuestion = false;
            var question =
                await _interviewQuestionService.GenerateFollowUpQuestionAsync(CurrentQuestion, FollowUpDepth,
                    _cts.Token);
            question.QuestionText +=
                $"(问题 {CurrentQuestionIndex + 1} 的 {CommonHelper.GetEnumDescription(FollowUpDepth)} 追问深度的追问)";
            InterviewQuestions.Insert(CurrentQuestionIndex + 1, question);
            await NextQuestion();
            HasGeneratedFollowUpQuestion = true;
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            _snackbarService.ShowError("生成追问失败", $"错误: {e.Message}");
            _logger.Error("生成追问失败，错误: {ExMessage}", e.Message);
        }
    }

    private async void AudioPushHandler(object? sender, AudioDataAvailableEventArgs e) {
        try {
            await _speechRecognitionService.PushAudioAsync(e);
        } catch (Exception ex) {
            _snackbarService.ShowError("发送音频错误", $"错误信息: {ex.Message}");
            _logger.Error("发送音频错误: {ExMessage}", ex.Message);
        }
    }

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

    private void EmotionAnaysisResultUpdate(object? sender, EmotionAnalysisResultEventArgs e) {
        FacialEmotion = e.Emotion;
        if (CanSaveOrFinish) {
            _emotionCollector.AddSample(e.Emotion);
        }
        // HeadPose = e.HeadPose;
        // GazeDirection = e.Gaze;
    }

    private async Task SpeechStart() {
        _decoder.Reset();
        await _speechRecognitionService.StartAsync(_appId, _apiKey, _apiSecret);
        await StartMicrophoneAsync();
    }

    private void SpeechStop() {
        Task.Run(() => {
            StopMicrophone();
            _speechRecognitionService.StopAsync();
        }).Wait(3000);
    }

    private Task SpeechStopAsync() {
        return Task.Run(SpeechStop);
    }

    [RelayCommand]
    private async Task StartSpeechRecognition() {
        if (IsSpeeching) {
            return;
        }

        try {
            await SpeechStart();
        } catch (Exception e) {
            _snackbarService.ShowError("语音识别服务启动失败", $"错误：{e.Message}");
            _logger.Error("语音识别服务启动失败，错误：{Message}", e.Message);
        }

        IsSpeeching = true;
    }

    [RelayCommand]
    private async Task StopSpeechRecognition() {
        if (!IsSpeeching) {
            return;
        }

        IsSpeeching = false;

        try {
            await SpeechStopAsync();
        } catch (Exception e) {
            _snackbarService.ShowError("语音识别服务停止失败", $"错误：{e.Message}");
            _logger.Error("语音识别服务停止失败，错误：{Message}", e.Message);
        }

        MicrophoneVolume = 0;
    }

    [RelayCommand]
    private void AddSpeechAnswer() {
        CurrentQuestionAnswer += SpeechRecognitionResult;
    }

    [RelayCommand]
    private void ClearSpeechRecognition() {
        SpeechRecognitionResult = string.Empty;
        _finalText = string.Empty;
        _decoder.Reset();
    }

    [RelayCommand]
    private async Task StopAndBack() {
        _emotionCollector.Reset();
        await InterviewStop(true);
    }

    partial void OnCurrentQuestionAnswerChanged(string value) {
        CurrentQuestion.CustomAnswer = value;
    }

    [RelayCommand]
    private async Task PreviousQuestion() {
        SaveAnswer();
        await StopSpeechRecognition();
        if (HasPreviousQuestion) {
            CurrentQuestionIndex--;
        }

        CurrentQuestionAnswer = CurrentQuestion.CustomAnswer;
    }

    [RelayCommand]
    private async Task NextQuestion() {
        SaveAnswer();
        await StopSpeechRecognition();
        if (HasNextQuestion) {
            CurrentQuestionIndex++;
        }

        CurrentQuestionAnswer = CurrentQuestion.CustomAnswer;
    }

    [RelayCommand]
    private void SaveAnswer() {
        _interviewAnswerSaveService.SaveAnswer(_interviewName, InterviewQuestions.ToList(),
            _emotionCollector.BuildSummary(), _preferencesService.Get("UserResume", new Resume())!);
    }

    [RelayCommand]
    private async Task FinishInterview() {
        SaveAnswer();
        await InterviewStop(false);
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