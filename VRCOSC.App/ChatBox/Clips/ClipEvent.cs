﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using VRCOSC.App.Modules;
using VRCOSC.App.Settings;
using VRCOSC.App.Utils;

namespace VRCOSC.App.ChatBox.Clips;

public class ClipEvent : ClipElement
{
    public string ModuleID { get; } = null!;
    public string EventID { get; } = null!;

    public Observable<float> Length = new();

    public override string DisplayName
    {
        get
        {
            var module = ModuleManager.GetInstance().GetModuleOfID(ModuleID);
            var eventReference = ChatBoxManager.GetInstance().GetEvent(ModuleID, EventID);
            Debug.Assert(eventReference is not null);

            return module.Title + " - " + eventReference.DisplayName.Value;
        }
    }

    public override bool ShouldBeVisible
    {
        get
        {
            if (!SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.ShowRelevantModules)) return true;

            var selectedClip = MainWindow.GetInstance().ChatBoxPage.SelectedClip;
            Debug.Assert(selectedClip is not null);

            var enabledModuleIDs = ModuleManager.GetInstance().GetEnabledModuleIDs().Where(moduleID => selectedClip.LinkedModules.Contains(moduleID)).OrderBy(moduleID => moduleID);
            return enabledModuleIDs.Contains(ModuleID);
        }
    }

    public override bool IsDefault => base.IsDefault && Length.IsDefault;

    [JsonConstructor]
    public ClipEvent()
    {
    }

    public ClipEvent(ClipEventReference reference)
    {
        ModuleID = reference.ModuleID;
        EventID = reference.EventID;
        Format = new Observable<string>(reference.DefaultFormat);
        Length = new Observable<float>(reference.DefaultLength);
    }
}

/// <summary>
/// Used as a reference for what events a module has created.
/// </summary>
public class ClipEventReference
{
    internal string ModuleID { get; init; }
    internal string EventID { get; init; }
    internal string DefaultFormat { get; init; }
    internal float DefaultLength { get; init; }

    public Observable<string> DisplayName { get; } = new("INVALID");
}