// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace AI_Interviewer.Models;

public class ErrorEventArgs(Exception exception, string operation) : EventArgs {
    public Exception Exception { get; } = exception;
    public string Operation { get; } = operation; // 出错的操作
}