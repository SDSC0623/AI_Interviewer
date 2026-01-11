// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace AI_Interviewer.Helpers;

public class EmotionModelBootstrapper {
    private const string ModelFileName = "emotion.onnx";

    public static string EnsureEmotionModel(string appDataRoot) {
        var modelDir = Path.Combine(appDataRoot, "models");
        Directory.CreateDirectory(modelDir);

        var targetPath = Path.Combine(modelDir, ModelFileName);
        if (File.Exists(targetPath)) {
            return targetPath;
        }

        var asm = Assembly.GetExecutingAssembly();
        var resourceName =
            asm.GetName().Name + ".Resources.Models." + ModelFileName;

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) {
            throw new InvalidOperationException($"内嵌模型资源未找到: {resourceName}");
        }

        using var fs = File.Create(targetPath);
        stream.CopyTo(fs);

        return targetPath;
    }
}