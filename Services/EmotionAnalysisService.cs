// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using ErrorEventArgs = AI_Interviewer.Models.ErrorEventArgs;

namespace AI_Interviewer.Services;

public class EmotionAnalysisService : IEmotionAnalysisService {
    public bool IsRunning { get; private set; }
    public event EventHandler<EmotionAnalysisResultEventArgs>? OnResultUpdated;
    public event EventHandler<ErrorEventArgs>? OnError;

    private readonly object _frameLock = new();

    private byte[]? _latestFrame;
    private int _frameWidth;
    private int _frameHeight;

    private Thread? _workerThread;
    private CancellationTokenSource? _cts;

    private readonly TimeSpan _analysisInterval = TimeSpan.FromMilliseconds(300);
    private DateTime _lastAnalysisTime = DateTime.MinValue;

    private readonly CascadeClassifier _faceCascade;
    private readonly CascadeClassifier _eyeCascade;

    private readonly InferenceSession _emotionSession;

    public EmotionAnalysisService() {
        var cascadePath = OpenCvResourceBootstrapper.EnsureHaarCascades(GlobalSettings.AppDataDirectory);

        _faceCascade = new CascadeClassifier(
            Path.Combine(cascadePath, "haarcascade_frontalface_default.xml"));

        _eyeCascade = new CascadeClassifier(
            Path.Combine(cascadePath, "haarcascade_eye.xml"));

        var modelPath = EmotionModelBootstrapper
            .EnsureEmotionModel(GlobalSettings.AppDataDirectory);

        _emotionSession = new InferenceSession(modelPath, new SessionOptions {
            IntraOpNumThreads = 1,
            InterOpNumThreads = 1
        });

        if (_faceCascade.Empty()) {
            throw new InvalidOperationException("人脸分类器加载失败");
        }
    }

    public void Start() {
        if (IsRunning) {
            return;
        }

        IsRunning = true;
        _cts = new CancellationTokenSource();

        _workerThread = new Thread(WorkerLoop) {
            IsBackground = true,
            Name = "EmotionAnalysisWorker"
        };

        _workerThread.Start(_cts.Token);
    }

    public void Stop() {
        if (!IsRunning) {
            return;
        }

        _cts?.Cancel();
        _workerThread?.Join(500);

        _cts = null;
        _workerThread = null;
        IsRunning = false;
    }

    private readonly AutoResetEvent _frameArrived = new(false);

    public void SubmitFrame(ReadOnlySpan<byte> bgr24Data, int width, int height) {
        lock (_frameLock) {
            if (_latestFrame == null || _latestFrame.Length != bgr24Data.Length) {
                _latestFrame = new byte[bgr24Data.Length];
            }

            bgr24Data.CopyTo(_latestFrame);
            _frameWidth = width;
            _frameHeight = height;
        }

        _frameArrived.Set();
    }

    private void WorkerLoop(object? obj) {
        var token = (CancellationToken)obj!;

        while (!token.IsCancellationRequested) {
            // 等新帧 or 超时
            _frameArrived.WaitOne(_analysisInterval);

            if (token.IsCancellationRequested) {
                break;
            }

            byte[]? frame;
            int w, h;

            lock (_frameLock) {
                if (_latestFrame == null) {
                    continue;
                }

                frame = (byte[])_latestFrame.Clone();
                w = _frameWidth;
                h = _frameHeight;
            }

            try {
                var result = AnalyzeInternal(frame, w, h);
                OnResultUpdated?.Invoke(this, result);
            } catch (Exception ex) {
                RaiseError(ex, "WorkerLoop");
            }
        }
    }


    /*private void WorkerLoop(object? obj) {
        var token = (CancellationToken)obj!;

        while (!token.IsCancellationRequested) {
            try {
                if (DateTime.UtcNow - _lastAnalysisTime < _analysisInterval) {
                    Thread.Sleep(_analysisInterval);
                    continue;
                }

                byte[]? frame;
                int w, h;

                lock (_frameLock) {
                    if (_latestFrame == null) {
                        Thread.Sleep(100);
                        continue;
                    }

                    frame = (byte[])_latestFrame.Clone();
                    w = _frameWidth;
                    h = _frameHeight;
                }

                _lastAnalysisTime = DateTime.UtcNow;

                var result = AnalyzeInternal(frame, w, h);

                OnResultUpdated?.Invoke(this, result);
            } catch (OperationCanceledException) {
                /* 正常退出 #1#
            } catch (Exception ex) {
                RaiseError(ex, "WorkerLoop");
            }
        }
    }*/

    private int _faceDetectCounter;
    private Rect[] _cachedFaces = [];
    private int _noFaceCount;

    private Mat ResizeForDetection(Mat src, out double scale) {
        const int maxDim = 640;

        if (src is { Width: <= maxDim, Height: <= maxDim }) {
            scale = 1.0;
            return src;
        }

        scale = src.Width > src.Height
            ? (double)maxDim / src.Width
            : (double)maxDim / src.Height;

        var resized = new Mat();
        Cv2.Resize(src, resized, new Size(), scale, scale);
        return resized;
    }


    private EmotionAnalysisResultEventArgs AnalyzeInternal(byte[] bgr, int width, int height) {
        try {
            if (bgr.Length != width * height * 3) {
                throw new InvalidOperationException("Frame buffer size mismatch");
            }

            using var mat = new Mat(height, width, MatType.CV_8UC3);
            Marshal.Copy(bgr, 0, mat.Data, bgr.Length);

            using var detectMat = ResizeForDetection(mat, out double scale);

            using var gray = new Mat();
            Cv2.CvtColor(detectMat, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.EqualizeHist(gray, gray);

            bool needDetect = _faceDetectCounter++ % 15 == 0 || _cachedFaces.Length == 0;

            if (needDetect) {
                _cachedFaces = _faceCascade.DetectMultiScale(
                    gray,
                    1.05,
                    3,
                    HaarDetectionTypes.ScaleImage,
                    new Size(20, 20));
            }

            if (_cachedFaces.Length == 0) {
                _noFaceCount++;
                if (_noFaceCount > 10) {
                    Thread.Sleep(200);
                }

                return NoFaceResult();
            }

            _noFaceCount = 0;

            var faceSmall = _cachedFaces
                .OrderByDescending(r => r.Width * r.Height)
                .First();

            // 映射回原图坐标
            var face = Math.Abs(scale - 1.0) < 1e-5
                ? faceSmall
                : new Rect(
                    (int)(faceSmall.X / scale),
                    (int)(faceSmall.Y / scale),
                    (int)(faceSmall.Width / scale),
                    (int)(faceSmall.Height / scale));

            face &= new Rect(0, 0, mat.Width, mat.Height);
            if (face.Width <= 0 || face.Height <= 0) {
                return NoFaceResult();
            }

            using var faceGray = new Mat(mat, face);
            Cv2.CvtColor(faceGray, faceGray, ColorConversionCodes.BGR2GRAY);

            var emotion = AnalyzeEmotionByCnn(faceGray);
            var headPose = AnalyzeHeadPose(face, mat.Width, mat.Height);
            var gaze = AnalyzeGaze(faceGray);

            return new EmotionAnalysisResultEventArgs {
                Emotion = emotion,
                HeadPose = headPose,
                Gaze = gaze,
                Confidence = 0.8
            };
        } catch (Exception ex) {
            RaiseError(ex, "AnalyzeInternal");
            return NoFaceResult();
        }
    }

    private DenseTensor<float> PreprocessFace(Mat faceGray) {
        using var resized = new Mat();
        Cv2.Resize(faceGray, resized, new Size(48, 48));

        var data = new float[1 * 48 * 48 * 1];

        int idx = 0;
        for (int y = 0; y < 48; y++) {
            for (int x = 0; x < 48; x++) {
                data[idx++] = resized.At<byte>(y, x) / 255f;
            }
        }

        return new DenseTensor<float>(data, [1, 48, 48, 1]);
    }

    private EmotionType AnalyzeEmotionByCnn(Mat faceGray) {
        var inputTensor = PreprocessFace(faceGray);

        var inputs = new List<NamedOnnxValue> {
            NamedOnnxValue.CreateFromTensor("conv2d_input", inputTensor)
        };

        using var results = _emotionSession.Run(inputs);
        var scores = results.First().AsEnumerable<float>().ToArray();

        int maxIndex = Array.IndexOf(scores, scores.Max());

        return maxIndex switch {
            0 => EmotionType.Angry,
            1 => EmotionType.Disgusted,
            2 => EmotionType.Fearful,
            3 => EmotionType.Happy,
            4 => EmotionType.Neutral,
            5 => EmotionType.Sad,
            6 => EmotionType.Surprised,
            _ => EmotionType.Unknown
        };
    }

    private HeadPose AnalyzeHeadPose(Rect face, int frameWidth, int frameHeight) {
        int faceCenterX = face.X + face.Width / 2;
        int faceCenterY = face.Y + face.Height / 2;

        int frameCenterX = frameWidth / 2;
        int frameCenterY = frameHeight / 2;

        double hOffset = (double)(faceCenterX - frameCenterX) / frameWidth;
        double vOffset = (double)(faceCenterY - frameCenterY) / frameHeight;

        HeadHorizontalPose horizontal =
            Math.Abs(hOffset) < 0.15 ? HeadHorizontalPose.Front :
            hOffset > 0 ? HeadHorizontalPose.Right :
            HeadHorizontalPose.Left;

        HeadVerticalPose vertical =
            Math.Abs(vOffset) < 0.15 ? HeadVerticalPose.Level :
            vOffset > 0 ? HeadVerticalPose.Down :
            HeadVerticalPose.Up;

        return new HeadPose(horizontal, vertical);
    }

    private GazeDirection AnalyzeGaze(Mat faceGray) {
        var eyes = _eyeCascade.DetectMultiScale(
            faceGray,
            scaleFactor: 1.1,
            minNeighbors: 3,
            minSize: new Size(10, 10)
        );

        if (eyes.Length == 0) {
            return GazeDirection.Center;
        }

        double avgEyeX = eyes.Average(e => e.X + e.Width / 2.0);
        double faceCenterX = faceGray.Width / 2.0;

        double offsetRatio = (avgEyeX - faceCenterX) / faceGray.Width;

        if (Math.Abs(offsetRatio) < 0.2) {
            return GazeDirection.Center;
        }

        return offsetRatio > 0 ? GazeDirection.Right : GazeDirection.Left;
    }


    private static EmotionAnalysisResultEventArgs NoFaceResult() {
        return EmotionAnalysisResultEventArgs.NoFace;
    }


    private void RaiseError(Exception ex, string operation) {
        OnError?.Invoke(this, new ErrorEventArgs(ex, operation));
    }
}