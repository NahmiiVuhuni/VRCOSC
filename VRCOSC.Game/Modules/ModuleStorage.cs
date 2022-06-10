﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.IO;
using System.Text;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Platform;
using VRCOSC.Game.Util;

namespace VRCOSC.Game.Modules;

public static class ModuleStorage
{
    private const string storage_directory = "modules";

    public static void Save(Storage storage, ModuleDataManager dataManager)
    {
        var fileName = $"{dataManager.ModuleName}.ini";
        var moduleStorage = storage.GetStorageForDirectory(storage_directory);

        moduleStorage.Delete(fileName);

        using var fileStream = moduleStorage.GetStream(fileName, FileAccess.ReadWrite);
        using var streamWriter = new StreamWriter(fileStream);

        streamWriter.WriteLine(serialize(dataManager));
    }

    public static ModuleDataManager? Load(Storage storage, string moduleName)
    {
        var fileName = $"{moduleName}.ini";
        var moduleStorage = storage.GetStorageForDirectory(storage_directory);

        if (!moduleStorage.Exists(fileName)) return null;

        using var fileStream = moduleStorage.GetStream(fileName);
        using var streamReader = new StreamReader(fileStream);

        return deserialize(storage, streamReader);
    }

    #region Serializer

    private static string serialize(ModuleDataManager dataManager)
    {
        StringBuilder builder = new();
        builder.Append($@"{serializeInternalSettings(dataManager)}").Append('\n');
        builder.Append($@"{serializeModuleSettings(dataManager)}").Append('\n');
        builder.Append($@"{serializeModuleParameters(dataManager)}").Append('\n');
        return builder.ToString();
    }

    private static string serializeInternalSettings(ModuleDataManager dataManager)
    {
        StringBuilder builder = new("#InternalSettings\n");

        builder.Append($@"enabled={dataManager.Enabled}").Append('\n');
        builder.Append("#End\n");

        return builder.ToString();
    }

    private static string serializeModuleSettings(ModuleDataManager dataManager)
    {
        StringBuilder builder = new("#ModuleSettings\n");

        dataManager.Settings.ForEach(pair =>
        {
            var (key, setting) = pair;

            switch (setting)
            {
                case StringModuleSetting stringModuleSetting:
                    builder.Append($@"string:{key}={stringModuleSetting.Value}").Append('\n');
                    break;

                case IntModuleSetting intModuleSetting:
                    builder.Append($@"int:{key}={intModuleSetting.Value}").Append('\n');
                    break;

                case BoolModuleSetting boolModuleSetting:
                    builder.Append($@"bool:{key}={boolModuleSetting.Value}").Append('\n');
                    break;

                case EnumModuleSetting enumModuleSetting:
                    builder.Append($@"enum:{key}#{enumModuleSetting.Value.GetType().Name}={Convert.ToInt32(enumModuleSetting.Value)}").Append('\n');
                    break;
            }
        });

        builder.Append("#End\n");
        return builder.ToString();
    }

    private static string serializeModuleParameters(ModuleDataManager dataManager)
    {
        StringBuilder builder = new("#ModuleParameters\n");

        dataManager.Parameters.ForEach(pair =>
        {
            var (key, parameter) = pair;
            builder.Append($@"{key}={parameter.Value}").Append('\n');
        });

        builder.Append("#End\n");
        return builder.ToString();
    }

    #endregion

    #region Deserializer

    private static ModuleDataManager deserialize(Storage storage, TextReader textReader)
    {
        // pass storage through even though it's not needed as a dummy object
        // TODO Maybe refactor this so it doesn't need the data manager anymore
        ModuleDataManager dataManager = new(storage, string.Empty);

        string? line = textReader.ReadLine();

        while (line != null)
        {
            if (line.StartsWith("#"))
            {
                if (line.EndsWith("InternalSettings"))
                {
                    deserializeInternalSettings(dataManager, textReader);
                }
                else if (line.EndsWith("ModuleSettings"))
                {
                    deserializeModuleSettings(dataManager, textReader);
                }
                else if (line.EndsWith("ModuleParameters"))
                {
                    deserializeModuleParameters(dataManager, textReader);
                }
            }

            line = textReader.ReadLine();
        }

        return dataManager;
    }

    private static void deserializeInternalSettings(ModuleDataManager dataManager, TextReader textReader)
    {
        string? line = textReader.ReadLine();

        while (line != null && line != "#End")
        {
            var lineSplit = line.Split(new[] { '=' }, 2);

            var key = lineSplit[0];
            var value = lineSplit[1];

            if (key == "enabled")
            {
                dataManager.Enabled = bool.Parse(value);
            }

            line = textReader.ReadLine();
        }
    }

    private static void deserializeModuleSettings(ModuleDataManager dataManager, TextReader textReader)
    {
        string? line = textReader.ReadLine();

        while (line != null && line != "#End")
        {
            var lineSplitFirst = line.Split(new[] { ':' }, 2);
            var lineSplitSecond = lineSplitFirst[1].Split(new[] { '=' }, 2);

            var type = lineSplitFirst[0];
            var key = lineSplitSecond[0];
            var value = lineSplitSecond[1];

            switch (type)
            {
                case "string":
                    dataManager.SetSetting(key, new StringModuleSetting { Value = value });
                    break;

                case "int":
                    dataManager.SetSetting(key, new IntModuleSetting { Value = int.Parse(value) });
                    break;

                case "bool":
                    dataManager.SetSetting(key, new BoolModuleSetting { Value = bool.Parse(value) });
                    break;

                case "enum":
                {
                    var lineSplitThird = key.Split(new[] { '#' }, 2);

                    key = lineSplitThird[0];
                    var enumName = lineSplitThird[1];

                    Type enumType;

                    try
                    {
                        enumType = TypeUtils.GetTypeByName(enumName);
                    }
                    catch (InvalidOperationException)
                    {
                        // we're trying to load an enum that no longer exists due to a change in the module
                        // we can ignore this
                        return;
                    }

                    Enum enumValue = (Enum)Enum.ToObject(enumType, int.Parse(value));
                    dataManager.SetSetting(key, new EnumModuleSetting { Value = enumValue });
                    break;
                }
            }

            line = textReader.ReadLine();
        }
    }

    private static void deserializeModuleParameters(ModuleDataManager dataManager, TextReader textReader)
    {
        string? line = textReader.ReadLine();

        while (line != null && line != "#End")
        {
            var lineSplit = line.Split(new[] { '=' }, 2);

            var key = lineSplit[0];
            var value = lineSplit[1];

            dataManager.SetParameter(key, new ModuleParameter { Value = value });

            line = textReader.ReadLine();
        }
    }

    #endregion
}
