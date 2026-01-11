// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;

namespace AI_Interviewer.Services.IServices;

public interface IEmotionAnalysisService {
    event EventHandler<EmotionAnalysisResultEventArgs>? OnResultUpdated;
    event EventHandler<ErrorEventArgs>? OnError;

    void Start();
    void Stop();
    void SubmitFrame(ReadOnlySpan<byte> bgr24Data, int width, int height);
}