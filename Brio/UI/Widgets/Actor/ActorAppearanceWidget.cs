using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

public class ActorAppearanceWidget(ActorAppearanceCapability capability) : Widget<ActorAppearanceCapability>(capability)
{
    public override string HeaderName => "外观";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons | WidgetFlags.HasAdvanced;

    public override void DrawBody()
    {
        DrawLoadAppearance();

        float size = 35 * ((Capability.HasCustomizePlusIntegration ? 1 : 0) + (Capability.HasPenumbraIntegration ? 1 : 0) + (Capability.HasGlamourerIntegration ? 1 : 0)) * ImGuiHelpers.GlobalScale;

        if(size != 0)
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8f))
            using(var child = ImRaii.Child($"###appearance_child", new Vector2(-1, size), true, ImGuiWindowFlags.NoScrollbar))
            {
                if(child.Success)
                {
                    drawBody();
                }
            }
        }
        else
        {
            drawBody();
        }

        void drawBody()
        {
            AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(Capability);
            AppearanceEditorCommon.DrawGlamourerDesignSwitcher(Capability);
            AppearanceEditorCommon.DrawCustomizePlusProfileSwitcher(Capability);
        }
    }

    private void DrawLoadAppearance()
    {
        if(ImBrio.FontIconButton("load_npc", FontAwesomeIcon.PersonArrowDownToLine, "加载 NPC 外观"))
        {
            AppearanceEditorCommon.ResetNPCSelector();
            ImGui.OpenPopup("widget_npc_selector");
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.FontIconButton("import_charafile", FontAwesomeIcon.FileDownload, "导入角色"))
            FileUIHelpers.ShowImportCharacterModal(Capability, AppearanceImportOptions.All);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("export_charafile", FontAwesomeIcon.Save, "保存角色文件"))
            FileUIHelpers.ShowExportCharacterModal(Capability);

        ImBrio.VerticalSeparator(24, 1);

        using(ImRaii.Disabled(Capability.CanMCDF is false))
        {
            using(ImRaii.Disabled(Capability.IsSelf || Capability.IsAnyMCDFLoading))
            {
                if(ImBrio.FontIconButton("load_mcdf", FontAwesomeIcon.CloudDownloadAlt, "加载 MCDF"))
                {
                    FileUIHelpers.ShowImportMCDFModal(Capability);
                }
                ImGui.SameLine();
            }
            if(Capability.IsSelf)
                ImBrio.AttachToolTip("无法对玩家角色加载 MCDF。请先生成一个角色再加载 MCDF。");
            if(Capability.IsAnyMCDFLoading)
                ImBrio.AttachToolTip("正在加载另一个 MCDF，请等待完成。");

            using(ImRaii.Disabled(Capability.HasMCDF))
            {
                if(ImBrio.FontIconButton("save_mcdf", FontAwesomeIcon.CloudUploadAlt, "保存 MCDF"))
                {
                    FileUIHelpers.ShowExportMCDFModal(Capability);
                }
            }
            if(Capability.HasMCDF)
                ImBrio.AttachToolTip("无法在您的玩家角色上加载 MCDF。生成一个角色来加载 MCDF。");
        }

        ImBrio.VerticalSeparator(24, 1);

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
