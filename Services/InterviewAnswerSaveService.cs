// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using Newtonsoft.Json;

namespace AI_Interviewer.Services;

public class InterviewAnswerSaveService : IInterviewAnswerSaveService {
    private static string FolderPath => Path.Combine(GlobalSettings.AppDataDirectory, "InterviewAnswer");

    public void SaveAnswer(string name, List<Question> qAndA) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("name cannot be empty.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(qAndA);

        EnsureFolderExists();
        var path = GetFilePath(name);
        var answer = new InterviewAnswer {
            Name = name,
            QAndA = qAndA
        };
        var json = JsonConvert.SerializeObject(answer, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public InterviewAnswer GetAnswer(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("name cannot be empty.", nameof(name));
        }

        var path = GetFilePath(name);
        if (!File.Exists(path)) {
            return new InterviewAnswer {
                Name = name,
                QAndA = []
            };
        }

        var json = File.ReadAllText(path);
        var answer = JsonConvert.DeserializeObject<InterviewAnswer>(json);
        if (answer == null) {
            return new InterviewAnswer {
                Name = name,
                QAndA = []
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