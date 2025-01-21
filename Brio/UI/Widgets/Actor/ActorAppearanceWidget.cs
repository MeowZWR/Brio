﻿using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Brio.UI.Widgets.Actor;

internal class ActorAppearanceWidget(ActorAppearanceCapability capability) : Widget<ActorAppearanceCapability>(capability)
{
    public override string HeaderName => "外观";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons | WidgetFlags.HasAdvanced | WidgetFlags.CanHide;

    public override void DrawBody()
    {
        DrawLoadAppearance();
        AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(Capability);
    }

    private void DrawLoadAppearance()
    {
        if(ImBrio.FontIconButton("load_npc", FontAwesomeIcon.PersonArrowDownToLine, "加载 NPC 外观"))
        {
            AppearanceEditorCommon.ResetNPCSelector();
            ImGui.OpenPopup("widget_npc_selector");
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("import_charafile", FontAwesomeIcon.FileImport, "导入角色"))
            FileUIHelpers.ShowImportCharacterModal(Capability, AppearanceImportOptions.Default);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("export_charafile", FontAwesomeIcon.FileExport, "导出角色"))
            FileUIHelpers.ShowExportCharacterModal(Capability);

        ImGui.SameLine();

        if(Capability.CanMcdf)
        {
            if(ImBrio.FontIconButton("load_mcdf", FontAwesomeIcon.CloudDownloadAlt, "加载月海角色数据（MCDF）"))
            {
                FileUIHelpers.ShowImportMCDFModal(Capability);
            }
            ImGui.SameLine();
        }

        if(ImBrio.FontIconButton("advanced_appearance", FontAwesomeIcon.UserEdit, "高级"))
            ToggleAdvancedWindow();

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset_appearance", FontAwesomeIcon.Undo, 1, "重置", Capability.IsAppearanceOverridden))
            _ = Capability.ResetAppearance();

        using(var popup = ImRaii.Popup("widget_npc_selector"))
        {
            if(popup.Success)
            {
                if(AppearanceEditorCommon.DrawNPCSelector(Capability, AppearanceImportOptions.Default))
                    ImGui.CloseCurrentPopup();
            }
        }
    }

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("redrawwidget_redraw", FontAwesomeIcon.PaintBrush, "重绘"))
        {
            _ = Capability.Redraw();
        }
    }

    public override void ToggleAdvancedWindow()
    {
        UIManager.Instance.ToggleAppearanceWindow();
    }
}
