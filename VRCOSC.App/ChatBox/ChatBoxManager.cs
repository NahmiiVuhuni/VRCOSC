// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VRCOSC.App.ChatBox.Clips;
using VRCOSC.App.ChatBox.Clips.Variables;
using VRCOSC.App.ChatBox.Clips.Variables.Instances;
using VRCOSC.App.ChatBox.Serialisation;
using VRCOSC.App.Modules;
using VRCOSC.App.OSC.VRChat;
using VRCOSC.App.Profiles;
using VRCOSC.App.Serialisation;
using VRCOSC.App.Settings;
using VRCOSC.App.Utils;

namespace VRCOSC.App.ChatBox;

public class ChatBoxManager : INotifyPropertyChanged
{
    private static ChatBoxManager? instance;
    internal static ChatBoxManager GetInstance() => instance ??= new ChatBoxManager();

    public ObservableCollection<ClipStateReference> StateReferences = new();
    public ObservableCollection<ClipEventReference> EventReferences = new();

    // TODO: Create a default string variable reference that's always available
    // TODO: Create global variables (focused window)

    public ObservableCollection<ClipVariableReference> VariableReferences = new();

    //public readonly Dictionary<(string, string), string?> VariableValues = new();

    // moduleID, stateID
    public readonly Dictionary<string, string?> StateValues = new();

    // moduleID, eventID
    public readonly List<(string, string)> TriggeredEvents = new();

    public Timeline Timeline { get; } = new();

    public double CurrentPercentage => ((DateTimeOffset.Now - startTime).TotalSeconds % Timeline.Length.Value) / Timeline.Length.Value;
    public int CurrentSecond => (int)Math.Floor((DateTimeOffset.Now - startTime).TotalSeconds) % Timeline.Length.Value;
    private DateTimeOffset startTime;

    private Repeater? sendTask;
    private Repeater? updateTask;
    private bool isClear;

    public bool SendEnabled { get; set; }

    private readonly SerialisationManager serialisationManager;
    private readonly SerialisationManager validationSerialisationManager;
    private readonly ChatBoxValidationSerialiser chatBoxValidationSerialiser;

    public Observable<bool> IsLoaded { get; } = new();

    private ChatBoxManager()
    {
        serialisationManager = new SerialisationManager();
        serialisationManager.RegisterSerialiser(1, new ChatBoxSerialiser(AppManager.GetInstance().Storage, this, ProfileManager.GetInstance().ActiveProfile));

        validationSerialisationManager = new SerialisationManager();
        validationSerialisationManager.RegisterSerialiser(1, chatBoxValidationSerialiser = new ChatBoxValidationSerialiser(AppManager.GetInstance().Storage, this, ProfileManager.GetInstance().ActiveProfile));
    }

    public void Load()
    {
        addBuiltInVariables();

        Deserialise();
    }

    private void addBuiltInVariables()
    {
        VariableReferences.Add(new ClipVariableReference
        {
            VariableID = BuiltInVariables.Text.ToLookup(),
            DisplayName = { Value = "Text" },
            ClipVariableType = typeof(TextClipVariable),
            ValueType = typeof(string)
        });

        VariableReferences.Add(new ClipVariableReference
        {
            VariableID = BuiltInVariables.FocusedWindow.ToLookup(),
            DisplayName = { Value = "Focused Window" },
            ClipVariableType = typeof(StringClipVariable),
            ValueType = typeof(string)
        });
    }

    public void Unload(string moduleID = "")
    {
        MainWindow.GetInstance().ChatBoxPage.SelectedClip = null;

        Serialise();

        if (string.IsNullOrEmpty(moduleID))
        {
            StateReferences.Clear();
            EventReferences.Clear();
            VariableReferences.Clear();
        }
        else
        {
            StateReferences.RemoveIf(clipStateReference => clipStateReference.ModuleID == moduleID);
            EventReferences.RemoveIf(clipEventReference => clipEventReference.ModuleID == moduleID);
            VariableReferences.RemoveIf(clipVariableReference => clipVariableReference.ModuleID == moduleID);
        }

        IsLoaded.Value = false;
    }

    public void Serialise()
    {
        if (!IsLoaded.Value) return;

        serialisationManager.Serialise();
    }

    public void Deserialise(string filePathOverride = "", bool bypassValidation = false)
    {
        Timeline.Clips.Clear();

        chatBoxValidationSerialiser.Reset();

        if (!bypassValidation)
        {
            var validationDeserialisationSuccess = validationSerialisationManager.Deserialise();

            if (validationDeserialisationSuccess != DeserialisationResult.Success)
            {
                Logger.Log($"ChatBox validation deserialisation ended in {validationDeserialisationSuccess}");
                return;
            }

            if (!chatBoxValidationSerialiser.IsValid)
            {
                Logger.Log("ChatBox could not validate all data");
                ExceptionHandler.Handle("ChatBox could not load all data.\nThis is usually the fault of a module not loading correctly or a missing config.\nPlease make sure all your modules are up-to-date and have correct configs.");
                return;
            }
        }

        var deserialisationSuccess = serialisationManager.Deserialise(string.IsNullOrEmpty(filePathOverride), filePathOverride);

        if (deserialisationSuccess != DeserialisationResult.Success)
        {
            Logger.Log($"ChatBox deserialisation ended in {deserialisationSuccess}");
            return;
        }

        for (var i = 0; i < Timeline.LayerCount; i++) Timeline.GenerateDroppableAreas(i);

        IsLoaded.Value = true;

        if (!string.IsNullOrEmpty(filePathOverride) || bypassValidation) Serialise();
    }

    public void Start()
    {
        var sendInterval = SettingsManager.GetInstance().GetValue<int>(VRCOSCSetting.ChatBoxSendInterval);

        startTime = DateTimeOffset.Now;
        sendTask = new Repeater(chatBoxUpdate);
        updateTask = new Repeater(update);
        SendEnabled = true;
        isClear = true;

        sendTask.Start(TimeSpan.FromMilliseconds(sendInterval));
        updateTask.Start(TimeSpan.FromSeconds(1f / 60f));

        Timeline.Clips.ForEach(clip => clip.ChatBoxStart());

        foreach (var pair in StateValues)
        {
            StateValues[pair.Key] = null;
        }

        TriggeredEvents.Clear();
    }

    public async void Stop()
    {
        await (sendTask?.StopAsync() ?? Task.CompletedTask);
        await (updateTask?.StopAsync() ?? Task.CompletedTask);
        sendTask = null;
        updateTask = null;
        clearChatBox();
    }

    private void update()
    {
        OnPropertyChanged(nameof(CurrentPercentage));
    }

    private void chatBoxUpdate()
    {
        updateBuiltInVariables();
        ModuleManager.GetInstance().ChatBoxUpdate();

        Timeline.Clips.ForEach(clip => clip.Update());
        TriggeredEvents.Clear();

        evaluateClips();
    }

    private void updateBuiltInVariables()
    {
        GetVariable(null, BuiltInVariables.FocusedWindow.ToLookup())!.SetValue(ProcessExtensions.GetActiveWindowTitle());
        GetVariable(null, BuiltInVariables.Text.ToLookup())!.SetValue(string.Empty);
    }

    private void evaluateClips()
    {
        var validClip = getValidClip();

        if (ChatBoxWorldBlacklist.IsCurrentWorldBlacklisted && SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.ChatBoxWorldBlacklist) && validClip is not null)
        {
            handleClip(null);
            return;
        }

        handleClip(validClip);
    }

    private Clip? getValidClip()
    {
        return Timeline.Clips.OrderBy(clip => clip.Layer.Value).FirstOrDefault(clip => clip.Evaluate());
    }

    private void handleClip(Clip? clip)
    {
        if (!SendEnabled || clip is null)
        {
            clearChatBox();
            return;
        }

        isClear = false;
        sendText(clip.GetFormattedText());
    }

    private void sendText(string text)
    {
        var finalText = convertSpecialCharacters(text);
        AppManager.GetInstance().VRChatOscClient.SendValues(VRChatOscConstants.ADDRESS_CHATBOX_INPUT, [finalText, true, false]);
    }

    private static string convertSpecialCharacters(string input)
    {
        // Convert new line to vertical tab
        return input.Replace("\n", "\v");
    }

    private void clearChatBox()
    {
        if (isClear) return;

        sendText(string.Empty);
        isClear = true;
    }

    public void SetTyping(bool typing)
    {
        AppManager.GetInstance().VRChatOscClient.SendValue(VRChatOscConstants.ADDRESS_CHATBOX_TYPING, typing);
    }

    #region States

    public void CreateState(ClipStateReference reference)
    {
        StateReferences.Add(reference);

        Timeline.Clips.ForEach(clip =>
        {
            if (clip.LinkedModules.Contains(reference.ModuleID))
            {
                clip.States.Add(new ClipState(reference));
            }
        });
    }

    public void DeleteState(string moduleID, string stateID)
    {
        var stateToDelete = GetState(moduleID, stateID);
        if (stateToDelete is null) return;

        StateReferences.Remove(stateToDelete);

        Timeline.Clips.ForEach(clip =>
        {
            var stateInstances = new List<ClipState>();
            stateInstances.AddRange(clip.States.Where(clipState => clipState.States.ContainsKey(moduleID) && clipState.States[moduleID] == stateID));
            stateInstances.ForEach(clipState => clip.States.Remove(clipState));
        });
    }

    public ClipStateReference? GetState(string moduleID, string stateID)
    {
        return StateReferences.FirstOrDefault(reference => reference.ModuleID == moduleID && reference.StateID == stateID);
    }

    public void ChangeStateTo(string moduleID, string stateID)
    {
        StateValues[moduleID] = stateID;
    }

    #endregion

    #region Events

    public void CreateEvent(ClipEventReference reference)
    {
        EventReferences.Add(reference);

        Timeline.Clips.ForEach(clip =>
        {
            if (clip.LinkedModules.Contains(reference.ModuleID))
            {
                clip.Events.Add(new ClipEvent(reference));
            }
        });
    }

    public void DeleteEvent(string moduleID, string eventID)
    {
        var eventToDelete = GetEvent(moduleID, eventID);
        if (eventToDelete is null) return;

        EventReferences.Remove(eventToDelete);

        Timeline.Clips.ForEach(clip =>
        {
            var eventInstances = new List<ClipEvent>();
            eventInstances.AddRange(clip.Events.Where(clipEvent => clipEvent.ModuleID == moduleID && clipEvent.EventID == eventID));
            eventInstances.ForEach(clipEvent => clip.Events.Remove(clipEvent));
        });
    }

    public ClipEventReference? GetEvent(string moduleID, string eventID)
    {
        return EventReferences.FirstOrDefault(reference => reference.ModuleID == moduleID && reference.EventID == eventID);
    }

    public void TriggerEvent(string moduleID, string eventID)
    {
        if (TriggeredEvents.Contains((moduleID, eventID))) return;

        TriggeredEvents.Add((moduleID, eventID));
    }

    #endregion

    #region Variables

    public void CreateVariable(ClipVariableReference reference)
    {
        VariableReferences.Add(reference);
    }

    public void DeleteVariable(string moduleID, string variableID)
    {
        var variableToDelete = GetVariable(moduleID, variableID);
        if (variableToDelete is null) return;

        VariableReferences.Remove(variableToDelete);

        Timeline.Clips.ForEach(clip =>
        {
            clip.States.ForEach(clipState =>
            {
                var variableInstances = new List<ClipVariable>();
                variableInstances.AddRange(clipState.Variables.Where(clipVariable => clipVariable.ModuleID == moduleID && clipVariable.VariableID == variableID));
                variableInstances.ForEach(clipVariable => clipState.Variables.Remove(clipVariable));
            });

            clip.Events.ForEach(clipEvent =>
            {
                var variableInstances = new List<ClipVariable>();
                variableInstances.AddRange(clipEvent.Variables.Where(clipVariable => clipVariable.ModuleID == moduleID && clipVariable.VariableID == variableID));
                variableInstances.ForEach(clipVariable => clipEvent.Variables.Remove(clipVariable));
            });
        });
    }

    public ClipVariableReference? GetVariable(string? moduleID, string variableID)
    {
        return VariableReferences.FirstOrDefault(reference => reference.ModuleID == moduleID && reference.VariableID == variableID);
    }

    #endregion

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum BuiltInVariables
{
    Text,
    FocusedWindow
}
