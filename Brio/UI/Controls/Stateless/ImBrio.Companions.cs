﻿using Brio.Game.Types;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    public static bool BorderedGameIcon(string id, CompanionRowUnion union, bool showText = true, ImGuiButtonFlags flags = ImGuiButtonFlags.MouseButtonLeft, Vector2? size = null)
    {
        var (description, icon) = union.Match(
           companion => ($"{companion.Singular}\n{companion.RowId}\n模型: {companion.Model.Row}", companion.Icon),
           mount => ($"{mount.Singular}\n{mount.RowId}\n模型: {mount.ModelChara.Row}", mount.Icon),
           ornament => ($"{ornament.Singular}\n{ornament.RowId}\n模型: {ornament.Model}", ornament.Icon),
           none => ("None", (uint)0)
       );

        bool wasClicked = false;

        if(!showText)
        {
            description = string.Empty;
        }

        var placeholderIcon = union.Match(
                companion => "Images.Companion.png",
                mount => "Images.Mount.png",
                ornament => "Images.Ornament.png",
                none => "Images.Companion.png"
            );

        wasClicked = BorderedGameIcon(id, icon, placeholderIcon, description, flags, size);

        return wasClicked;
    }
}
