﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.SDK.OVR.Device;

public class HMD : OVRDevice
{
    // TODO Check if this override is actually needed
    protected override bool IsTrackedDeviceConnected() => Valve.VR.OpenVR.IsHmdPresent();
}
