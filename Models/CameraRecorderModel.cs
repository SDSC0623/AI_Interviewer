// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace AI_Interviewer.Models;

public class CameraFrameEventArgs(byte[] bgrData, int width, int height, long frameIndex, DateTime timestamp) {
    public byte[] BgrData { get; } = bgrData;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public long FrameIndex { get; } = frameIndex;
    public DateTime Timestamp { get; } = timestamp;
}