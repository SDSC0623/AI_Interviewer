// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace AI_Interviewer.Helpers;

public class UnexpectedCallException(string message = "这不应该被调用，请检查逻辑") : Exception(message);