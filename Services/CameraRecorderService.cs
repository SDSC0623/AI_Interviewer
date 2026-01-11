using System.Diagnostics;
using System.Runtime.InteropServices;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using OpenCvSharp;

namespace AI_Interviewer.Services;

public class CameraRecorderService : ICameraRecorderService {
    private int Width => 1280;
    private int Height => 720;
    private bool MirrorHorizontal { get; set; }
    public bool IsRunning { get; private set; }

    public event EventHandler<CameraFrameEventArgs>? OnFrameArrived;
    public event EventHandler<ErrorEventArgs>? OnError;

    private readonly object _stateLock = new();

    private VideoCapture? _capture;
    private Thread? _workerThread;
    private CancellationTokenSource? _cts;

    private int _cameraIndex = -1;
    private double _targetFps;
    private long _frameIndex;

    public void Start(double targetFps, int cameraIndex = -1) {
        lock (_stateLock) {
            if (IsRunning) {
                return;
            }

            if (cameraIndex != -1) {
                _cameraIndex = cameraIndex;
            }

            if (_cameraIndex == -1) {
                RaiseError(new Exception("请选择一个可用的摄像头"), "Start");
                return;
            }

            if (targetFps <= 0 || targetFps > 60) {
                throw new ArgumentException("FPS 必须在 1-60 之间");
            }

            _targetFps = targetFps;
            _frameIndex = 0;

            _capture = new VideoCapture(_cameraIndex, VideoCaptureAPIs.DSHOW);
            if (!_capture.IsOpened()) {
                throw new Exception("无法打开摄像头");
            }

            _capture.Set(VideoCaptureProperties.FrameWidth, Width);
            _capture.Set(VideoCaptureProperties.FrameHeight, Height);
            _capture.Set(VideoCaptureProperties.Fps, _targetFps);

            _cts = new CancellationTokenSource();
            _workerThread = new Thread(() => CaptureLoop(_cts.Token)) {
                IsBackground = true,
                Name = "CameraCaptureThread"
            };

            IsRunning = true;
            _workerThread.Start();
        }
    }

    public void Stop() {
        lock (_stateLock) {
            if (!IsRunning) {
                return;
            }

            IsRunning = false;

            _cts?.Cancel();

            if (_workerThread is { IsAlive: true }) {
                _workerThread.Join(500);
            }

            _capture?.Release();
            _capture?.Dispose();

            _cts = null;
            _workerThread = null;
            _capture = null;
        }
    }

    public void SetMirrorHorizontal(bool mirror) {
        MirrorHorizontal = mirror;
    }

    private void CaptureLoop(CancellationToken token) {
        try {
            var frameIntervalMs = 1000.0 / _targetFps;
            var stopwatch = Stopwatch.StartNew();

            using var mat = new Mat();

            while (!token.IsCancellationRequested && IsRunning) {
                var loopStart = stopwatch.ElapsedMilliseconds;

                if (_capture == null || !_capture.Read(mat) || mat.Empty()) {
                    continue;
                }

                if (MirrorHorizontal) {
                    Cv2.Flip(mat, mat, FlipMode.Y);
                }

                int bufferSize = mat.Rows * mat.Cols * mat.ElemSize();
                var data = new byte[bufferSize];

                Marshal.Copy(mat.Data, data, 0, bufferSize);

                var frame = new CameraFrameEventArgs(
                    data,
                    mat.Width,
                    mat.Height,
                    Interlocked.Increment(ref _frameIndex),
                    DateTime.UtcNow
                );

                RaiseFrame(frame);

                var elapsed = stopwatch.ElapsedMilliseconds - loopStart;
                var delay = frameIntervalMs - elapsed;
                if (delay > 1)
                    Thread.Sleep((int)delay);
            }
        } catch (Exception e) {
            RaiseError(e, "CaptureLoop");
        }
    }


    private void RaiseFrame(CameraFrameEventArgs frame) {
        var handlers = OnFrameArrived?.GetInvocationList();
        if (handlers == null) {
            return;
        }

        foreach (var @delegate in handlers) {
            var handler = (EventHandler<CameraFrameEventArgs>)@delegate;
            try {
                handler(this, frame);
            } catch {
                /* 吞掉，保护采集线程 */
            }
        }
    }

    private void RaiseError(Exception ex, string source) {
        var handlers = OnError?.GetInvocationList();
        if (handlers == null) {
            return;
        }

        var args = new ErrorEventArgs(ex, source);

        foreach (var @delegate in handlers) {
            var handler = (EventHandler<ErrorEventArgs>)@delegate;
            try {
                handler(this, args);
            } catch {
                /* 吞掉，保护采集线程 */
            }
        }
    }
}