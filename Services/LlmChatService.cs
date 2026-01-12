// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace AI_Interviewer.Services;

public class LlmChatService : ILlmChatService {
    private string _appId = string.Empty;
    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;
    private const string BaseWsUrl = "wss://spark-api.xf-yun.com/v1/x1";

    private void EnsureInit() {
        if (string.IsNullOrWhiteSpace(_appId) || string.IsNullOrWhiteSpace(_apiKey) ||
            string.IsNullOrWhiteSpace(_apiSecret)) {
            throw new InvalidOperationException("Spark LLM 尚未初始化");
        }
    }

    public void Init(string appId, string apiKey, string apiSecret) {
        _appId = appId;
        _apiKey = apiKey;
        _apiSecret = apiSecret;
    }

    public async Task<SparkChatResponse> SendAsync(SparkChatRequest request,
        CancellationToken cancellationToken = default) {
        EnsureInit();

        if (string.IsNullOrWhiteSpace(request.Header.AppId)) {
            request.Header.AppId = _appId;
        }

        var wsUrl = SparkWebSocketAuthHelper.BuildAuthUrl(_apiKey, _apiSecret, BaseWsUrl);

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(wsUrl), cancellationToken);

        var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings {
            ContractResolver = new DefaultContractResolver {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        });
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        await ws.SendAsync(requestBytes, WebSocketMessageType.Text, true, cancellationToken);

        var buffer = new byte[16 * 1024];
        SparkChatResponse? lastResponse = null;

        var mergedText = new StringBuilder();

        while (ws.State == WebSocketState.Open) {
            var result = await ws.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close) {
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = JsonConvert.DeserializeObject<SparkChatResponse>(json, new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            });

            if (response == null) {
                continue;
            }

            lastResponse = response;

            if (response.Header.Code != 0) {
                throw new InvalidOperationException(
                    $"Spark LLM error: {response.Header.Code}, {response.Header.Message}, sid={response.Header.Sid}");
            }

            var choices = response.Payload?.Choices;
            if (choices?.Text == null) {
                continue;
            }

            foreach (var text in choices.Text) {
                if (!string.IsNullOrEmpty(text.Content)) {
                    mergedText.Append(text.Content);
                }
            }

            if (choices.Status == 2) {
                break;
            }
        }

        if (ws.State == WebSocketState.Open) {
            await ws.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "completed",
                CancellationToken.None);
        }

        if (lastResponse == null) {
            throw new InvalidOperationException("No response received from Spark LLM.");
        }

        var finalChoices = new SparkChoices {
            Status = lastResponse.Payload?.Choices?.Status ?? 2,
            Seq = lastResponse.Payload?.Choices?.Seq ?? 0,
            Text = [
                new SparkChoiceText {
                    Role = "assistant",
                    Content = mergedText.ToString(),
                    Index = 0
                }
            ]
        };

        var finalPayload = new SparkChatResponsePayload {
            Choices = finalChoices
        };

        return new SparkChatResponse {
            Header = lastResponse.Header,
            Payload = finalPayload
        };
    }
}