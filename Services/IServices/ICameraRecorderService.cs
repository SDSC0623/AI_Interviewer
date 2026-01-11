// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using AI_Interviewer.Models;
using DirectShowLib;

namespace AI_Interviewer.Services.IServices;

public interface ICameraRecorderService {
    bool IsRunning { get; }

    void Start(double targetFps, int cameraIndex = -1);
    void Stop();
    void SetMirrorHorizontal(bool mirror);

    public static IEnumerable<CameraDevice> EnumerateCameras() {
        DsDevice[] videoDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
        for (var i = 0; i < videoDevices.Length; i++) {
            var device = videoDevices[i];
            yield return new CameraDevice { Name = device.Name, Index = i };
            device.Dispose();
        }

        yield return new CameraDevice { Name = "Camera 1", Index = 0 };
    }

    event EventHandler<CameraFrameEventArgs> OnFrameArrived;
    event EventHandler<ErrorEventArgs> OnError;
}