// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;
using NAudio.Wave;

namespace AI_Interviewer.Services.IServices;

public interface IAudioRecorderService {
    // 录音配置
    RecorderConfiguration Configuration { get; }

    // 回调函数
    event EventHandler<AudioDataAvailableEventArgs>? OnDataAvailable;
    event EventHandler<ErrorEventArgs>? OnError;

    void Initialize(RecorderConfiguration config);

    public static IEnumerable<MicrophoneDevice> EnumerateMicrophones() {
        for (int i = 0; i < WaveIn.DeviceCount; i++) {
            var caps = WaveIn.GetCapabilities(i);
            yield return new MicrophoneDevice { DeviceIndex = i, Name = caps.ProductName };
        }
    }

    void StartRecording(TimeSpan? duration = null);
    void StopRecording();
    void PauseRecording();
    void ResumeRecording();
    void Dispose();
}