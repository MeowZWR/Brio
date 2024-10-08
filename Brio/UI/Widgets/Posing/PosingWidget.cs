﻿using Brio.Capabilities.Posing;
using Brio.Game.Posing;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Widgets.Posing;

internal class PosingWidget(PosingCapability capability) : Widget<PosingCapability>(capability)
{
    public override string HeaderName => "姿势";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.HasAdvanced | WidgetFlags.DefaultOpen;

    private readonly PosingTransformEditor _posingTransformEditor = new();

    private readonly BoneSearchControl _boneSearchEditor = new();


    public override void DrawBody()
    {
        DrawButtons();

        ImGui.Separator();

        DrawTransform();
    }

    private void DrawButtons()
    {
        var overlayOpen = Capability.OverlayOpen;
        if(ImBrio.FontIconButton("overlay", overlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, overlayOpen ? "关闭叠加层" : "开启叠加层"))
        {
            Capability.OverlayOpen = !overlayOpen;
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("import", FontAwesomeIcon.FileImport, "导入姿势"))
        {
            ImGui.OpenPopup("DrawImportPoseMenuPopup");
        }

        FileUIHelpers.DrawImportPoseMenuPopup(Capability);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("export", FontAwesomeIcon.FileExport, "导出姿势"))
            FileUIHelpers.ShowExportPoseModal(Capability);

        ImGui.SameLine();

        using(ImRaii.Disabled(Capability.Selected.Value is None))
        {
            if(ImBrio.FontIconButton("clear_selection", FontAwesomeIcon.MinusSquare, "清除选择"))
                Capability.ClearSelection();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("bone_search", FontAwesomeIcon.Search, "骨骼搜索"))
        {
            ImGui.OpenPopup("widget_bone_search_popup");
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("undo", FontAwesomeIcon.Backward, "撤销", Capability.HasUndoStack))
        {
            Capability.Undo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("redo", FontAwesomeIcon.Forward, "重做", Capability.HasRedoStack))
        {
            Capability.Redo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "重置姿势", Capability.HasOverride))
        {
            Capability.Reset(false, false);
        }

        using(var popup = ImRaii.Popup("widget_bone_search_popup", ImGuiWindowFlags.AlwaysAutoResize))
        {
            if(popup.Success)
            {
                _boneSearchEditor.Draw("widget_bone_search", Capability);
            }
        }
    }

    private void DrawTransform()
    {
        PosingEditorCommon.DrawSelectionName(Capability);

        _posingTransformEditor.Draw("posing_widget_transform", Capability, true);
    }

    public override void ToggleAdvancedWindow()
    {
        UIManager.Instance.ToggleGraphicalPosingWindow();
    }
}
