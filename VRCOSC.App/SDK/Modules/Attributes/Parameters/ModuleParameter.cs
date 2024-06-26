﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using VRCOSC.App.Utils;

namespace VRCOSC.App.SDK.Modules.Attributes.Parameters;

public class ModuleParameter : ModuleAttribute
{
    /// <summary>
    /// The metadata for this <see cref="ModuleParameter"/>
    /// </summary>
    public new ModuleParameterMetadata Metadata => (ModuleParameterMetadata)base.Metadata;

    public Observable<string> Name { get; private set; } = null!;

    private readonly string defaultName;

    public override void PreDeserialise()
    {
        Name = new Observable<string>(defaultName);
        Name.Subscribe(_ => RequestSerialisation?.Invoke());
    }

    public override bool IsDefault() => Name.IsDefault;
    public override void SetDefault() => Name.SetDefault();
    public override object GetRawValue() => Name.Value;

    public override bool Deserialise(object ingestValue)
    {
        if (ingestValue is not string stringIngestValue) return false;

        Name.Value = stringIngestValue;
        return true;
    }

    public ModuleParameter(ModuleParameterMetadata metadata, string defaultName)
        : base(metadata)
    {
        this.defaultName = defaultName;
    }
}
