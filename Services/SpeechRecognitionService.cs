// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Web;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using Newtonsoft.Json;

namespace AI_Interviewer.Services;

public sealed class SpeechRecognitionService : ISpeechRecognitionService {
    private readonly SemaphoreSlim _sessionLock = new(1, 1);

    private ClientWebSocket? _ws;
    private CancellationTokenSource? _serviceCts;
    private CancellationTokenSource? _sessionCts;

    private Channel<AudioDataAvailableEventArgs>? _audioChannel;

    private Task? _sendLoopTask;
    private Task? _receiveLoopTask;

    private int _frameIndex;
    private DateTime _sessionStartTime;
    private readonly TimeSpan _maxSession = TimeSpan.FromSeconds(55);

    private string _appId = string.Empty;
    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;

    private volatile bool _uiRequestedStop;

    public bool IsRunning { get; private set; }

    public event EventHandler<SpeechRecognitionResultEventArgs>? OnResult;
    public event EventHandler<SpeechRecognitionErrorEventArgs>? OnError;

    public async Task StartAsync(
        string appId,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default) {
        if (IsRunning) {
            return;
        }

        _appId = appId;
        _apiKey = apiKey;
        _apiSecret = apiSecret;

        _uiRequestedStop = false;
        IsRunning = true;

        _serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _audioChannel = Channel.CreateBounded<AudioDataAvailableEventArgs>(
            new BoundedChannelOptions(32) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

        try {
            await RestartSessionAsync();

            _sendLoopTask = Task.Run(SendLoopAsync, _serviceCts.Token);
        } catch (Exception ex) {
            OnError?.Invoke(this, new SpeechRecognitionErrorEventArgs(ex, "StartAsync"));
            throw;
        }
    }

    public async Task StopAsync() {
        if (!IsRunning) {
            return;
        }

        _uiRequestedStop = true;
        IsRunning = false;

        try {
            _audioChannel?.Writer.TryComplete();

            await _serviceCts?.CancelAsync()!;

            await StopSessionAsync();

            if (_sendLoopTask != null)
                await _sendLoopTask;
        } catch (Exception ex) {
            OnError?.Invoke(this, new SpeechRecognitionErrorEventArgs(ex, "StopAsync"));
        } finally {
            _serviceCts?.Dispose();
            _serviceCts = null;
        }
    }

    public ValueTask PushAudioAsync(AudioDataAvailableEventArgs audio) {
        if (!IsRunning || _audioChannel == null) {
            return ValueTask.CompletedTask;
        }

        _audioChannel.Writer.TryWrite(audio);
        return ValueTask.CompletedTask;
    }

    private async Task RestartSessionAsync() {
        if (_uiRequestedStop) {
            return;
        }

        await _sessionLock.WaitAsync();
        try {
            await StopSessionAsync();
            await StartSessionAsync();
        } finally {
            _sessionLock.Release();
        }
    }

    private async Task StartSessionAsync() {
        _frameIndex = 0;
        _sessionStartTime = DateTime.UtcNow;

        _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(_serviceCts!.Token);

        var ws = new ClientWebSocket();
        var url = BuildAuthUrl(_apiKey, _apiSecret);

        await ws.ConnectAsync(new Uri(url), _sessionCts.Token);

        _ws = ws;

        _receiveLoopTask = Task.Run(
            () => ReceiveLoopAsync(ws, _sessionCts.Token),
            _sessionCts.Token);
    }

    private async Task StopSessionAsync() {
        if (_sessionCts == null) {
            return;
        }

        try {
            await _sessionCts.CancelAsync();

            if (_ws?.State == WebSocketState.Open) {
                await _ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "session stop",
                    CancellationToken.None);
            }

            if (_receiveLoopTask != null)
                await _receiveLoopTask;
        } finally {
            _ws?.Dispose();
            _ws = null;

            _sessionCts.Dispose();
            _sessionCts = null;
        }
    }

    private async Task SendLoopAsync() {
        try {
            while (await _audioChannel!.Reader.WaitToReadAsync(_serviceCts!.Token)) {
                if (_uiRequestedStop) {
                    return;
                }

                while (_audioChannel.Reader.TryRead(out var audio)) {
                    if (DateTime.UtcNow - _sessionStartTime > _maxSession) {
                        OnResult?.Invoke(this, new SpeechRecognitionResultEventArgs("", true));

                        await RestartSessionAsync();
                        break;
                    }

                    await SendAudioFrameAsync(audio);
                }
            }
        } catch (OperationCanceledException) {
        } catch (Exception ex) {
            OnError?.Invoke(this, new SpeechRecognitionErrorEventArgs(ex, "SendLoop"));
        }
    }

    private async Task ReceiveLoopAsync(
        ClientWebSocket ws,
        CancellationToken token) {
        var buffer = new byte[8192];

        try {
            while (!token.IsCancellationRequested && ws.State == WebSocketState.Open) {
                var result = await ws.ReceiveAsync(buffer, token);

                if (result.MessageType == WebSocketMessageType.Close) {
                    OnResult?.Invoke(this,
                        new SpeechRecognitionResultEventArgs("", true));

                    await RestartSessionAsync();
                    return;
                }

                HandleMessage(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
        } catch (OperationCanceledException) {
        } catch (Exception ex) {
            OnError?.Invoke(this,
                new SpeechRecognitionErrorEventArgs(ex, "ReceiveLoop"));
        }
    }

    private void HandleMessage(string json) {
        var resp = JsonConvert.DeserializeObject<SparkWsResponse>(json);
        if (resp?.code != 0 || resp.data?.result == null) {
            return;
        }

        var result = resp.data.result;
        var text = ExtractText(resp);
        var isFinal = result.ls;

        var range = result.rg != null
            ? new KeyValuePair<int, int>(result.rg[0], result.rg[1])
            : new KeyValuePair<int, int>(-1, -1);

        OnResult?.Invoke(this,
            new SpeechRecognitionResultEventArgs(
                text, isFinal, null, result.sn, result.pgs, range));

        if (isFinal && IsMeaninglessFinal(text)) {
            OnResult?.Invoke(this, new SpeechRecognitionResultEventArgs("", true));
            _ = RestartSessionAsync();
        }
    }

    private static bool IsMeaninglessFinal(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return true;
        }

        return text.Contains('。') ||
               text.Contains('？') ||
               text.Contains('！');
    }

    private static string ExtractText(SparkWsResponse resp) {
        var sb = new StringBuilder();

        foreach (var ws in resp.data!.result!.ws ?? Enumerable.Empty<dynamic>()) {
            foreach (var cw in ws.cw ?? Enumerable.Empty<dynamic>()) {
                if (!string.IsNullOrEmpty(cw.w))
                    sb.Append(cw.w);
            }
        }

        return sb.ToString();
    }

    private async Task SendAudioFrameAsync(AudioDataAvailableEventArgs audio) {
        var ws = _ws;
        if (ws is not { State: WebSocketState.Open }) {
            return;
        }

        int status = _frameIndex == 0 ? 0 : 1;
        _frameIndex++;

        var payload = new {
            common = new { app_id = _appId },
            business = new {
                language = "zh_cn",
                domain = "iat",
                accent = "mandarin",
                dwa = "wpgs"
            },
            data = new {
                status,
                format = "audio/L16;rate=16000",
                encoding = "raw",
                audio = Convert.ToBase64String(audio.AudioData)
            }
        };

        var json = JsonConvert.SerializeObject(payload);
        await ws.SendAsync(
            Encoding.UTF8.GetBytes(json),
            WebSocketMessageType.Text,
            true,
            _sessionCts!.Token);
    }

    private static string BuildAuthUrl(string apiKey, string apiSecret) {
        const string host = "iat-api.xfyun.cn";
        const string requestUri = "/v2/iat";

        var date = DateTime.UtcNow.ToString("r");

        var signatureOrigin =
            $"host: {host}\n" +
            $"date: {date}\n" +
            $"GET {requestUri} HTTP/1.1";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        var signature = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureOrigin)));

        var authorizationOrigin =
            $"api_key=\"{apiKey}\", algorithm=\"hmac-sha256\", " +
            $"headers=\"host date request-line\", signature=\"{signature}\"";

        var authorization =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationOrigin));

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["authorization"] = authorization;
        query["date"] = date;
        query["host"] = host;

        return $"wss://{host}{requestUri}?{query}";
    }
}