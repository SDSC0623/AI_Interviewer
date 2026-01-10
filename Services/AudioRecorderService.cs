using System.IO;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using NAudio.Wave;

namespace AI_Interviewer.Services;

public class AudioRecorderService : IAudioRecorderService {
    private bool _initialized;
    private bool _isRecording;
    private bool _isPaused;

    private WaveInEvent? _waveIn;
    private BufferedWaveProvider? _bufferedProvider;
    private WaveFileWriter? _writer;
    private System.Timers.Timer? _durationTimer;

    private readonly object _syncLock = new();

    public RecorderConfiguration Configuration { get; private set; } = new();

    public event EventHandler<AudioDataAvailableEventArgs>? OnDataAvailable;
    public event EventHandler<RecordingErrorEventArgs>? OnError;

    public void Initialize(RecorderConfiguration config) {
        if (_initialized) {
            throw new InvalidOperationException("AudioRecorderService 已被初始化");
        }

        Configuration = config;
        _initialized = true;
    }

    private WaveFormat GetWaveFormat() {
        return new WaveFormat(
            Configuration.Format.SampleRate,
            Configuration.Format.BitsPerSample,
            Configuration.Format.Channels);
    }

    private void EnsureInitialized() {
        if (!_initialized) {
            throw new InvalidOperationException("请先初始化 AudioRecorderService");
        }
    }

    public void StartRecording(TimeSpan? duration = null) {
        EnsureInitialized();

        lock (_syncLock) {
            if (_isRecording) {
                throw new InvalidOperationException("录音正在进行中");
            }

            try {
                _waveIn = new WaveInEvent {
                    DeviceNumber = Configuration.InputDeviceIndex,
                    WaveFormat = GetWaveFormat(),
                    BufferMilliseconds = Configuration.BufferMilliseconds
                };

                _bufferedProvider = new BufferedWaveProvider(_waveIn.WaveFormat) {
                    DiscardOnBufferOverflow = true
                };

                _waveIn.DataAvailable += OnWaveDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;

                if (Configuration.SaveMode == SaveMode.SaveToFile &&
                    !string.IsNullOrWhiteSpace(Configuration.OutputFilePathBase)) {
                    if (!Directory.Exists(Configuration.OutputFilePathBase)) {
                        Directory.CreateDirectory(Configuration.OutputFilePathBase);
                    }

                    var filePath = Path.Combine(
                        Configuration.OutputFilePathBase,
                        $"{DateTime.Now:yyyyMMdd-HHmmssfff}.wav");

                    _writer = new WaveFileWriter(filePath, GetWaveFormat());
                }

                _waveIn.StartRecording();
                _isRecording = true;
                _isPaused = false;

                // 自动停止逻辑
                if (duration is not null) {
                    _durationTimer = new System.Timers.Timer(duration.Value.TotalMilliseconds);
                    _durationTimer.Elapsed += (_, _) => StopRecording();
                    _durationTimer.AutoReset = false;
                    _durationTimer.Start();
                }
            } catch (Exception ex) {
                RaiseError(ex, "StartRecording");
            }
        }
    }

    public void StopRecording() {
        EnsureInitialized();

        lock (_syncLock) {
            if (!_isRecording) {
                return;
            }

            try {
                _durationTimer?.Stop();
                _durationTimer?.Dispose();
                _durationTimer = null;

                _waveIn?.StopRecording();
            } catch (Exception ex) {
                RaiseError(ex, "StopRecording");
            }
        }
    }

    public void PauseRecording() {
        EnsureInitialized();

        lock (_syncLock) {
            if (!_isRecording || _isPaused) {
                return;
            }

            _isPaused = true;
        }
    }

    public void ResumeRecording() {
        EnsureInitialized();

        lock (_syncLock) {
            if (!_isRecording || !_isPaused) {
                return;
            }

            _isPaused = false;
        }
    }


    private void OnWaveDataAvailable(object? sender, WaveInEventArgs e) {
        if (_isPaused) {
            return;
        }

        try {
            _bufferedProvider?.AddSamples(e.Buffer, 0, e.BytesRecorded);

            OnDataAvailable?.Invoke(this,
                new AudioDataAvailableEventArgs(
                    e.Buffer[..e.BytesRecorded],
                    e.BytesRecorded,
                    isFinal: false));
            if (_writer != null) {
                _writer.Write(e.Buffer, 0, e.BytesRecorded);
                _writer.Flush();
            }
        } catch (Exception ex) {
            RaiseError(ex, "OnWaveDataAvailable");
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e) {
        lock (_syncLock) {
            _isRecording = false;
        }

        if (e.Exception != null) {
            RaiseError(e.Exception, "RecordingStopped");
        }

        // 通知最终数据
        OnDataAvailable?.Invoke(this,
            new AudioDataAvailableEventArgs([], 0, isFinal: true));

        Cleanup();
    }

    private void RaiseError(Exception ex, string operation) {
        OnError?.Invoke(this, new RecordingErrorEventArgs(ex, operation));
    }

    private void Cleanup() {
        _writer?.Dispose();
        _writer = null;
        _waveIn?.Dispose();
        _waveIn = null;
        _bufferedProvider = null;
    }

    public void Dispose() {
        StopRecording();
        Cleanup();
    }
}