using Brio.Capabilities.World;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace Brio.UI.Widgets.World.Lights;

public class LightLifetimeWidget(LightLifetimeCapability lightLifetimeCapability) : Widget<LightLifetimeCapability>(lightLifetimeCapability)
{
    public override string HeaderName => "生命周期";
    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "克隆灯光", Capability.CanClone))
        {
            Capability.Clone();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_move_to_camera", FontAwesomeIcon.CaretSquareDown, "移动到相机"))
        {
            Capability.MoveToCamera();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.HoldButton("lifetimewidget_destroy", "", FontAwesomeIcon.Trash, 1f, new(40, 0), centerTest: true, tooltip: "[长按销毁]", onlyIcon: true))
        {
            Capability.Destroy();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "重命名灯光"))
        {
            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.MenuItem($"重命名 {Capability.Entity.FriendlyName}###actorlifetime_rename"))
        {
            ImGui.CloseCurrentPopup();

            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }

        if(Capability.CanClone)
        {
            if(ImGui.MenuItem("克隆###lightlifetime_clone"))
            {
                Capability.Clone();
            }
        }

        if(ImGui.MenuItem("移动到相机###lightlifetime_move_to_camera"))
        {
            Capability.MoveToCamera();
        }

        var togglenText = Capability.GameLight.IsVisible ? $"Turn OFF {Capability.Entity.FriendlyName}" : $"Turn ON {Capability.Entity.FriendlyName}";
        if(ImGui.MenuItem($"{togglenText}###lightlifetime_toggle"))
        {
            Capability.GameLight.ToggleLight();
        }

        if(ImGui.MenuItem("打开灯光窗口###lightlifetime_lightwindow"))
        {
            Capability.OpenLightWindow();
        }

        if(Capability.CanDestroy)
        {
            ImGui.Separator();

            if(ImGui.BeginMenu("销毁###lightlifetime_destroy"))
            {
                if(ImGui.MenuItem("克隆###actorlifetime_clone"))
                {
                    Capability.Destroy();
                }

                ImGui.EndMenu();
            }
        }
    }
}
