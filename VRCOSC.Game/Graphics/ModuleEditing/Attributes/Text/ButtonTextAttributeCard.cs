﻿using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;
using VRCOSC.Game.Graphics.UI.Button;
using VRCOSC.Game.Modules;

namespace VRCOSC.Game.Graphics.ModuleEditing.Attributes.Text;

public class ButtonTextAttributeCard : TextAttributeCard
{
    private readonly ModuleAttributeSingleWithButton attributeSingleWithButton;

    public ButtonTextAttributeCard(ModuleAttributeSingleWithButton attributeData)
        : base(attributeData)
    {
        attributeSingleWithButton = attributeData;
    }

    protected override Drawable CreateContent()
    {
        return new GridContainer
        {
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
            RelativeSizeAxes = Axes.X,
            Height = 40,
            ColumnDimensions = new[]
            {
                new Dimension(),
                new Dimension()
            },
            Content = new[]
            {
                new[]
                {
                    base.CreateContent(),
                    new TextButton
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.9f),
                        Text = "Obtain Access Token",
                        Masking = true,
                        CornerRadius = 5,
                        Action = attributeSingleWithButton.ButtonAction,
                        BackgroundColour = VRCOSCColour.GreenLight
                    }
                }
            }
        };
    }
}