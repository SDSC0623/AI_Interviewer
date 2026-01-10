// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace AI_Interviewer.Models;

public enum FollowUpDepthLevel {
    [Description("浅")] Shallow,
    [Description("中")] Medium,
    [Description("深")] Deep
}