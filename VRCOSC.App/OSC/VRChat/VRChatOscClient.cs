﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VRCOSC.App.OSC.Client;
using VRCOSC.App.OSC.Query;
using VRCOSC.App.Utils;

namespace VRCOSC.App.OSC.VRChat;

public class VRChatOscClient : OscClient
{
    public Action<VRChatOscMessage>? OnParameterSent;
    public Action<VRChatOscMessage>? OnParameterReceived;

    private readonly HttpClient client = new();

    private ConnectionManager connectionManager = null!;

    public VRChatOscClient()
    {
        OnMessageSent += message => { OnParameterSent?.Invoke(new VRChatOscMessage(message)); };

        OnMessageReceived += message =>
        {
            var data = new VRChatOscMessage(message);
            if (!data.Values.Any()) return;

            OnParameterReceived?.Invoke(data);
        };
    }

    public void Init(ConnectionManager connectionManager)
    {
        this.connectionManager = connectionManager;
    }

    private Task<OSCQueryNode?> findParameter(string parameterName) => findAddress(VRChatOscConstants.ADDRESS_AVATAR_PARAMETERS_PREFIX + parameterName);

    private async Task<OSCQueryNode?> findAddress(string address)
    {
        Console.WriteLine($"Querying address: {address}");

        try
        {
            if (connectionManager.VRChatQueryPort is null) return null;

            var url = $"http://127.0.0.1:{connectionManager.VRChatQueryPort}/{address}";

            var response = await client.GetAsync(new Uri(url));
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            var node = JsonConvert.DeserializeObject<OSCQueryNode>(content);

            return node is null || node.Access == OSCQueryNodeAccess.NoValue ? null : node;
        }
        catch (Exception e)
        {
            ExceptionHandler.Handle(e, $"Exception when trying to find parameter: {address}");
            return null;
        }
    }

    public async Task<object?> FindParameterValue(string parameterName)
    {
        var node = await findAddress(parameterName);
        if (node is null) return null;

        return node.OscType switch
        {
            "f" => Convert.ToSingle(node.Value[0]),
            "i" => Convert.ToInt32(node.Value[0]),
            "T" => Convert.ToBoolean(node.Value[0]),
            "F" => Convert.ToBoolean(node.Value[0]),
            _ => throw new InvalidOperationException($"Unknown type {node.OscType}")
        };
    }

    public async Task<TypeCode?> FindParameterType(string parameterName)
    {
        var node = await findAddress(parameterName);
        if (node is null) return null;

        return node.OscType switch
        {
            "f" => TypeCode.Single,
            "i" => TypeCode.Int32,
            "T" => TypeCode.Boolean,
            "F" => TypeCode.Boolean,
            _ => throw new InvalidOperationException($"Unknown type {node.OscType}")
        };
    }
}
