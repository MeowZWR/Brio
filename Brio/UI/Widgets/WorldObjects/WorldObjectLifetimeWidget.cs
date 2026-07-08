using Brio.Capabilities.WorldObjects;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace Brio.UI.Widgets.WorldObjects;

public class WorldObjectLifetimeWidget(WorldObjectLifetimeCapability capability) : Widget<WorldObjectLifetimeCapability>(capability)
{
    public override string HeaderName => "生命周期";
    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("bglifetime_clone", FontAwesomeIcon.Clone, "克隆", Capability.CanClone))
        {
            Capability.Clone();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("bglifetime_movetocamera", FontAwesomeIcon.CaretSquareDown, "移动到相机"))
        {
            Capability.MoveToCamera();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.HoldButton("bglifetime_destroy", "", FontAwesomeIcon.Trash, 1f, new(40, 0), centerTest: true, tooltip: "[长按销毁]", onlyIcon: true))
        {
            Capability.Destroy();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.FontIconButton("bglifetime_rename", FontAwesomeIcon.Signature, "重命名"))
        {
            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.MenuItem($"重命名 {Capability.Entity.FriendlyName}###bglifetime_popup_rename"))
        {
            ImGui.CloseCurrentPopup();

            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }

        if(Capability.CanClone && ImGui.MenuItem("克隆###bglifetime_popup_clone"))
            Capability.Clone();

        if(ImGui.MenuItem("移动到相机###bglifetime_popup_move"))
            Capability.MoveToCamera();

        if(Capability.CanDestroy)
        {
            ImGui.Separator();

            if(ImGui.BeginMenu("销毁###bglifetime_popup_destroy"))
            {
                if(ImGui.MenuItem("确认销毁###bglifetime_popup_destroy_confirm"))
                    Capability.Destroy();

                ImGui.EndMenu();
            }
        }
    }
}
