// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace AI_Interviewer.Models;

public sealed class SparkChatRequest {
    public SparkRequestHeader Header { get; set; } = new();
    public SparkChatParameter Parameter { get; set; } = new();
    public SparkChatPayload Payload { get; set; } = new();
}

public sealed class SparkRequestHeader {
    public string AppId { get; set; } = string.Empty;
    public string? Uid { get; set; }
}

public sealed class SparkChatParameter {
    public SparkChatOptions Chat { get; set; } = new();
}

public sealed class SparkChatOptions {
    // 必传
    public string Domain { get; set; } = "spark-x";
    public int MaxTokens { get; set; } = 8192;
    public float Temperature { get; set; } = 0.3f;
    public float TopP { get; set; } = 0.8f;

    public List<SparkTool> Tools { get; set; } = [new()];
}

public sealed class SparkTool {
    public string Type { get; set; } = "web_search";
    public SparkWebSearchOptions? WebSearch { get; set; } = new();
}

public sealed class SparkWebSearchOptions {
    public bool Enable { get; set; } = true;
    public string SearchMode { get; set; } = "normal"; // normal | deep
}

public sealed class SparkChatPayload {
    public SparkMessage Message { get; set; } = new();
}

public sealed class SparkMessage {
    public List<SparkMessageText> Text { get; set; } = [];
}

public sealed class SparkMessageText {
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;
}

public sealed class SparkChatResponse {
    public SparkResponseHeader Header { get; set; } = new ();
    public SparkChatResponsePayload? Payload { get; set; } = new ();
}

public sealed class SparkResponseHeader {
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Sid { get; set; } = string.Empty;
    public int Status { get; set; }
}

public sealed class SparkChatResponsePayload {
    public SparkChoices? Choices { get; set; }
}

public sealed class SparkChoices {
    public int Status { get; set; }
    public int Seq { get; set; }

    public List<SparkChoiceText> Text { get; set; } = [];
}

public sealed class SparkChoiceText {
    public string Role { get; set; } = "assistant";

    public string? ReasoningContent { get; set; }
    public string Content { get; set; } = string.Empty;

    public int Index { get; set; }
}