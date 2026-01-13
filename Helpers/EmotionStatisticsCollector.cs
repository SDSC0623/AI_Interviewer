// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;

namespace AI_Interviewer.Helpers;

public class EmotionStatisticsCollector {
    private readonly Dictionary<EmotionType, int> _counts = new();
    private readonly List<EmotionType> _sequence = [];
    private int _totalSamples;

    public void AddSample(EmotionType emotion) {
        _totalSamples++;
        _sequence.Add(emotion);

        if (_counts.TryGetValue(emotion, out var count)) {
            _counts[emotion] = count + 1;
        } else {
            _counts[emotion] = 1;
        }
    }

    public EmotionSummary BuildSummary() {
        if (_totalSamples == 0) {
            return new EmotionSummary {
                DominantEmotion = EmotionType.Unknown,
                Volatility = 0,
                TotalSamples = 0,
                HasFaceMissingIssue = true
            };
        }

        var ratios = _counts.ToDictionary(
            kv => kv.Key,
            kv => kv.Value / (double)_totalSamples
        );

        var dominant = EmotionType.Unknown;

        var best = _counts
            .Where(kv => kv.Key != EmotionType.Unknown)
            .OrderByDescending(kv => kv.Value)
            .FirstOrDefault();

        if (best.Value > 0) {
            dominant = best.Key;
        }

        var changes = 0;
        for (var i = 1; i < _sequence.Count; i++) {
            if (_sequence[i] != _sequence[i - 1]) {
                changes++;
            }
        }

        var volatility = _sequence.Count > 1
            ? changes / (double)(_sequence.Count - 1)
            : 0;

        var faceMissing = ratios.TryGetValue(EmotionType.Unknown, out var unknownRatio)
                          && unknownRatio > 0.2;

        return new EmotionSummary {
            Ratios = ratios,
            DominantEmotion = dominant,
            Volatility = Math.Round(volatility, 2),
            TotalSamples = _totalSamples,
            HasFaceMissingIssue = faceMissing
        };
    }

    public void Reset() {
        _counts.Clear();
        _sequence.Clear();
        _totalSamples = 0;
    }
}