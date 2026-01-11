// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace AI_Interviewer.Helpers;

public static class OpenCvResourceBootstrapper {
    private static readonly string[] CascadeFiles = [
        "haarcascade_frontalface_default.xml",
        "haarcascade_eye.xml"
    ];

    public static string EnsureHaarCascades(string appDataRoot) {
        var cascadeDir = Path.Combine(appDataRoot, "opencv", "haarcascades");
        Directory.CreateDirectory(cascadeDir);

        var asm = Assembly.GetExecutingAssembly();
        var resourceRoot = asm.GetName().Name + ".Resources.OpenCV.haarcascades.";

        foreach (var file in CascadeFiles) {
            var targetPath = Path.Combine(cascadeDir, file);
            if (File.Exists(targetPath)) {
                continue;
            }

            var resourceName = resourceRoot + file;

            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) {
                throw new InvalidOperationException($"内嵌资源未找到: {resourceName}");
            }

            using var fs = File.Create(targetPath);
            stream.CopyTo(fs);
        }

        return cascadeDir;
    }
}