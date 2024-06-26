﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Linq;
using System.Text;

// ReSharper disable SuggestBaseTypeForParameter

namespace VRCOSC.App.OSC.Client;

internal static class OscEncoder
{
    internal static byte[] Encode(OscMessage message)
    {
        validateValueTypes(message);

        var index = 0;
        var data = new byte[calculateMessageLength(message)];

        insertAddress(message, data, ref index);
        insertTypeTags(message, data, ref index);
        insertValues(message, data, ref index);

        return data;
    }

    private static void validateValueTypes(OscMessage message)
    {
        if (message.Values.Any(value => value is not (string or int or float or bool)))
            throw new InvalidOperationException($"Cannot send value that is not of type string, int, float, or bool to address {message.Address}");
    }

    private static int calculateMessageLength(OscMessage message)
    {
        var addressLength = OscUtils.AlignIndex(Encoding.UTF8.GetByteCount(message.Address));
        var typeTagsLength = OscUtils.AlignIndex(1 + message.Values.Count);

        var valuesLength = message.Values.Sum(value => value switch
        {
            string valueStr => OscUtils.AlignIndex(Encoding.UTF8.GetByteCount(valueStr)),
            int => 4,
            float => 4,
            bool => 0,
            _ => 0
        });

        return addressLength + typeTagsLength + valuesLength;
    }

    private static void insertAddress(OscMessage message, byte[] data, ref int index)
    {
        var addressBytes = Encoding.UTF8.GetBytes(message.Address);
        addressBytes.CopyTo(data, index);
        index = OscUtils.AlignIndex(addressBytes.Length);
    }

    private static void insertTypeTags(OscMessage message, byte[] data, ref int index)
    {
        data[index++] = OscChars.CHAR_COMMA;

        foreach (var value in message.Values)
        {
#pragma warning disable CS8509
            data[index++] = value switch
#pragma warning restore CS8509
            {
                string => OscChars.CHAR_STRING,
                int => OscChars.CHAR_INT,
                float => OscChars.CHAR_FLOAT,
                true => OscChars.CHAR_TRUE,
                false => OscChars.CHAR_FALSE
            };
        }

        index = OscUtils.AlignIndex(index);
    }

    private static void insertValues(OscMessage message, byte[] data, ref int index)
    {
        foreach (var value in message.Values)
        {
            switch (value)
            {
                case int intValue:
                    intToBytes(data, ref index, intValue);
                    break;

                case float floatValue:
                    floatToBytes(data, ref index, floatValue);
                    break;

                case string stringValue:
                    stringToBytes(data, ref index, stringValue);
                    break;
            }
        }
    }

    private static void intToBytes(byte[] data, ref int index, int value)
    {
        var bytes = BitConverter.GetBytes(value);
        data[index++] = bytes[3];
        data[index++] = bytes[2];
        data[index++] = bytes[1];
        data[index++] = bytes[0];
    }

    private static void floatToBytes(byte[] data, ref int index, float value)
    {
        var bytes = BitConverter.GetBytes(value);
        data[index++] = bytes[3];
        data[index++] = bytes[2];
        data[index++] = bytes[1];
        data[index++] = bytes[0];
    }

    private static void stringToBytes(byte[] data, ref int index, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);

        bytes.CopyTo(data, index);
        index = OscUtils.AlignIndex(bytes.Length);
    }
}
