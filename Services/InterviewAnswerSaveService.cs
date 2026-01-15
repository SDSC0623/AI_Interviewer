// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO;
using System.Text;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using Newtonsoft.Json;

namespace AI_Interviewer.Services;

public class InterviewAnswerSaveService : IInterviewAnswerSaveService {
    private static string FolderPath => Path.Combine(GlobalSettings.AppDataDirectory, "InterviewAnswer");

    public void SaveAnswer(string name, List<Question> qAndA, EmotionSummary emotionSummary, Resume resume,
        InterviewAnalysisResult? interviewAnalysisResult = null) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("name cannot be empty.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(qAndA);

        EnsureFolderExists();
        var path = GetFilePath(name);
        bool success = DateTime.TryParseExact(
            name,
            "'面试-'yyyy-MM-dd_HH-mm-ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime time
        );

        if (!success) {
            throw new ArgumentException("命名不符合规范");
        }

        var answer = new InterviewAnswer {
            Name = name,
            Time = time,
            QAndA = qAndA,
            Resume = resume,
            EmotionSummary = emotionSummary
        };
        if (interviewAnalysisResult != null) {
            answer.InterviewAnalysisResult = interviewAnalysisResult;
            answer.HasResult = true;
        } else {
            answer.HasResult = false;
        }

        var json = JsonConvert.SerializeObject(answer, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public void SaveAnswer(InterviewAnswer interviewAnswer) {
        SaveAnswer(interviewAnswer.Name, interviewAnswer.QAndA, interviewAnswer.EmotionSummary, interviewAnswer.Resume,
            interviewAnswer.InterviewAnalysisResult);
    }

    public void DeleteAnswer(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("name cannot be empty.", nameof(name));
        }

        var path = GetFilePath(name);
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    public InterviewAnswer GetAnswer(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("name cannot be empty.", nameof(name));
        }

        var path = GetFilePath(name);
        if (!File.Exists(path)) {
            return new InterviewAnswer {
                Name = name,
                Time = DateTime.MinValue,
                QAndA = [],
                Resume = new Resume(),
                EmotionSummary = new EmotionSummary()
            };
        }

        var json = File.ReadAllText(path);
        var answer = JsonConvert.DeserializeObject<InterviewAnswer>(json);
        if (answer == null) {
            return new InterviewAnswer {
                Name = name,
                Time = DateTime.MinValue,
                QAndA = [],
                Resume = new Resume(),
                EmotionSummary = new EmotionSummary()
            };
        }

        return answer;
    }

    public List<InterviewAnswer> GetAllAnswer() {
        EnsureFolderExists();
        if (!Directory.Exists(FolderPath)) {
            return [];
        }

        List<InterviewAnswer> result = [];
        var files = Directory.GetFiles(FolderPath, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var file in files) {
            var json = File.ReadAllText(file);
            var answer = JsonConvert.DeserializeObject<InterviewAnswer>(json);
            if (answer != null) {
                if (string.IsNullOrWhiteSpace(answer.InterviewAnalysisResult.Overall.Summary)) {
                    answer.HasResult = false;
                } else {
                    answer.HasResult = true;
                }

                result.Add(answer);
            }
        }

        return result.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void EnsureFolderExists() {
        if (!Directory.Exists(FolderPath)) {
            Directory.CreateDirectory(FolderPath);
        }
    }

    private string GetFilePath(string name) {
        var safeName = SanitizeFileName(name);
        return Path.Combine(FolderPath, safeName + ".json");
    }

    private static string SanitizeFileName(string name) {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name) {
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? "unnamed" : result;
    }
}