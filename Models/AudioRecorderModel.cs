// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

using System.ComponentModel;

namespace AI_Interviewer.Models;

public class AudioDataAvailableEventArgs(byte[] audioData, int bytesRecorded, bool isFinal = false) : EventArgs {
    public byte[] AudioData { get; } = audioData;
    public int BytesRecorded { get; } = bytesRecorded;
    public bool IsFinal { get; } = isFinal;
}

public class AudioFormat {
    public int SampleRate { get; set; } = 16000; // 16000 Hz
    public int BitsPerSample { get; set; } = 16; // 16-bit
    public int Channels { get; set; } = 1; // 单声道
    public int BlockAlign => BitsPerSample / 8 * Channels;
    public int BytesPerSecond => SampleRate * BlockAlign;
}

public class RecorderConfiguration {
    public AudioFormat Format { get; set; } = new();
    public int BufferMilliseconds { get; set; } = 40; // 缓冲区大小(毫秒)
    public int ChunkSize { get; set; } = 1280; // 每块大小(字节)
    public int InputDeviceIndex { get; set; } // 输入设备索引
    public SaveMode SaveMode { get; set; } = SaveMode.DoNotSave;
    public string OutputFilePathBase { get; set; } = string.Empty;
}

public enum SaveMode {
    [Description("不保存")] DoNotSave,
    [Description("保存到文件")] SaveToFile
}