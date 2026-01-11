// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global

namespace AI_Interviewer.Models;

public class SpeechRecognitionResultEventArgs(
    string text,
    bool isFinal,
    string? sessionId = null,
    int? sn = null,
    string? operation = null,
    KeyValuePair<int, int>? replaceRange = null) : EventArgs {
    public string Text { get; set; } = text;
    public bool IsFinal { get; } = isFinal;
    public string? SessionId { get; } = sessionId;
    public int? Sn { get; } = sn;
    public string? Operation { get; set; } = operation;
    public KeyValuePair<int, int>? ReplaceRange { get; set; } = replaceRange;
}

internal sealed class SparkWsResponse {
    public int code { get; set; }
    public string? message { get; set; }
    public string? sid { get; set; }
    public SparkData? data { get; set; }
}

internal sealed class SparkData {
    public SparkResult? result { get; set; }
}

internal sealed class SparkResult {
    public List<SparkWs>? ws { get; set; }
    public string? pgs { get; set; }
    public List<int>? rg { get; set; }
    public int? sn { get; set; }
    public bool ls { get; set; }
}

internal sealed class SparkWs {
    public List<SparkCw>? cw { get; set; }
}

internal sealed class SparkCw {
    public string? w { get; set; }
}