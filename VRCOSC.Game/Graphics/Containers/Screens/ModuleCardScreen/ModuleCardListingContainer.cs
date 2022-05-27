﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using VRCOSC.Game.Graphics.Drawables;
using VRCOSC.Game.Modules;

namespace VRCOSC.Game.Graphics.Containers.Screens.ModuleCardScreen;

public class ModuleCardListingContainer : Container
{
    private const float footer_height = 60;

    [Resolved]
    private ModuleManager ModuleManager { get; set; }

    [BackgroundDependencyLoader]
    private void load()
    {
        VRCOSCScrollContainer<ModuleCardGroupContainer> moduleCardGroupScroll;

        InternalChildren = new Drawable[]
        {
            new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Colour = VRCOSCColour.Gray4
            },
            moduleCardGroupScroll = new VRCOSCScrollContainer<ModuleCardGroupContainer>
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Bottom = footer_height
                }
            },
            new LineSeparator
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                RelativeSizeAxes = Axes.X,
                Size = new Vector2(0.95f, 5),
                Colour = Colour4.Black.Opacity(0.5f),
                Y = -footer_height
            },
            new ModuleCardListingFooter
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                RelativeSizeAxes = Axes.X,
                Height = footer_height,
                Padding = new MarginPadding(5)
            }
        };

        ModuleManager.Modules.ForEach(pair =>
        {
            var (moduleType, modules) = pair;

            moduleCardGroupScroll.Add(new ModuleCardGroupContainer
            {
                ModuleType = moduleType,
                Modules = modules
            });
        });
    }
}
