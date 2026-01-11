// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;

namespace AI_Interviewer.Services.IServices;

public interface ISpeechRecognitionService {
    bool IsRunning { get; }

    event EventHandler<SpeechRecognitionResultEventArgs>? OnResult;
    event EventHandler<ErrorEventArgs>? OnError;

    Task StartAsync(string appId, string apiKey, string apiSecret, CancellationToken cancellationToken = default);
    ValueTask PushAudioAsync(AudioDataAvailableEventArgs audio);
    Task StopAsync();
}