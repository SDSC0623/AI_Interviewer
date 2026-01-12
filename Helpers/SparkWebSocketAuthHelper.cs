// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace AI_Interviewer.Helpers;

public static class SparkWebSocketAuthHelper {
    public static string BuildAuthUrl(string apiKey, string apiSecret, string baseWsUrl) {

        var uri = new Uri(baseWsUrl);

        var host = uri.Host;
        var path = uri.AbsolutePath;

        var date = DateTime.UtcNow.ToString("r");

        var signatureOrigin =
            $"host: {host}\n" +
            $"date: {date}\n" +
            $"GET {path} HTTP/1.1";

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

        return $"wss://{host}{path}?{query}";
    }
}