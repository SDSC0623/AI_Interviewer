// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using AI_Interviewer.Helpers;

namespace AI_Interviewer.Models;

public enum EmotionType {
    [Description("愤怒")] Angry,
    [Description("厌恶")] Disgusted,
    [Description("恐惧")] Fearful,
    [Description("开心")] Happy,
    [Description("中性")] Neutral,
    [Description("悲伤")] Sad,
    [Description("惊讶")] Surprised,
    [Description("未知/未识别到人脸")] Unknown
}

public enum HeadHorizontalPose {
    [Description("未知/未识别到人脸")] Unknown,
    [Description("正视")] Front,
    [Description("向左")] Left,
    [Description("向右")] Right
}

public enum HeadVerticalPose {
    [Description("未知/未识别到人脸")] Unknown,
    [Description("平视")] Level,
    [Description("抬头")] Up,
    [Description("低头")] Down
}

public readonly record struct HeadPose(HeadHorizontalPose Horizontal, HeadVerticalPose Vertical) {
    public override string ToString() =>
        $"{CommonHelper.GetEnumDescription(Horizontal)}-{CommonHelper.GetEnumDescription(Vertical)}";
}

public enum GazeDirection {
    [Description("未知/未识别到人脸")] Unknown,
    [Description("正视")] Center,
    [Description("左视")] Left,
    [Description("右视")] Right
}

public class EmotionAnalysisResultEventArgs {
    public EmotionType Emotion { get; init; }

    public HeadPose HeadPose { get; init; }

    public GazeDirection Gaze { get; init; }

    public double Confidence { get; init; }

    public static EmotionAnalysisResultEventArgs NoFace =>
        new() {
            Emotion = EmotionType.Unknown,
            HeadPose = new HeadPose(
                HeadHorizontalPose.Unknown,
                HeadVerticalPose.Unknown),
            Gaze = GazeDirection.Unknown,
            Confidence = 0
        };
}