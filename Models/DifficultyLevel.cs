// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace AI_Interviewer.Models;

public enum DifficultyLevel {
    [Description("初级")] Basic,
    [Description("中级")] Intermediate,
    [Description("高级")] Advanced,
    [Description("专家")] Expert
}