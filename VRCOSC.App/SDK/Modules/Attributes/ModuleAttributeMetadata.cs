﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

namespace VRCOSC.App.SDK.Modules.Attributes;

public class ModuleAttributeMetadata
{
    /// <summary>
    /// The title for this <see cref="ModuleAttribute"/>
    /// </summary>
    public virtual string Title { get; }

    /// <summary>
    /// The description for this <see cref="ModuleAttribute"/>
    /// </summary>
    public string Description { get; }

    protected ModuleAttributeMetadata(string title, string description)
    {
        Title = title;
        Description = description;
    }
}
