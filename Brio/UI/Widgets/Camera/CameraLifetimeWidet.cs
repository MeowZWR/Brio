using Brio.Capabilities.Camera;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.Camera;

public class CameraLifetimeWidget(CameraLifetimeCapability capability) : Widget<CameraLifetimeCapability>(capability)
{
    public override string HeaderName => "生命周期";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImBrio.FontIconButton("CameraLifetime_clone", FontAwesomeIcon.Clone, "克隆相机"))
            {
                Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_target", FontAwesomeIcon.LocationCrosshairs, "设为活动相机"))
            {
                Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
            }

            ImBrio.VerticalSeparator(24, 1);

            using(ImRaii.Disabled(Capability.CameraEntity.CameraID == 0))
            {
                if(ImBrio.HoldButton("CameraLifetime_destroy", "", FontAwesomeIcon.Trash, 1f, centerTest: true, tooltip: "[长按销毁]", onlyIcon: true))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImBrio.VerticalSeparator(24, 1);

                if(ImBrio.FontIconButton("CameraLifetime_rename", FontAwesomeIcon.Signature, "重命名"))
                {
                    ModalManager.Instance.OpenRenameModal(Capability.Entity);
                }
            }

            ImGui.SameLine();

            var isLocked = Capability.Entity.IsLocked;
            var lockIcon = isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
            if(ImBrio.ToggelFontIconButton("CameraLifetime_lock", lockIcon, new Vector2(25, 0), isLocked, tooltip: isLocked ? "已锁定" : "未锁定"))
            {
                Capability.Entity.IsLocked = !Capability.Entity.IsLocked;
            }
        }
    }

    public override void DrawPopup()
    {
        if(Capability.IsAllowed == false)
            return;

        using(ImRaii.Disabled(Capability.CameraEntity.IsDefaultCamera))
        {
            if(ImGui.MenuItem($"重命名 {Capability.CameraEntity.FriendlyName}###CameraLifetime_rename"))
            {
                ImGui.CloseCurrentPopup();

                ModalManager.Instance.OpenRenameModal(Capability.Entity);
            }
        }

        if(ImGui.MenuItem("克隆###CameraLifetime_clone"))
        {
            Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
        }

        if(ImGui.MenuItem("目标###CameraLifetime_target"))
        {
            Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
        }

        var lockLabel = Capability.Entity.IsLocked ? "解锁" : "锁定";
        if(ImGui.MenuItem($"{lockLabel}###CameraLifetime_lock"))
        {
            Capability.Entity.IsLocked = !Capability.Entity.IsLocked;
        }

        if(Capability.CanDestroy)
        {
            ImGui.Separator();

            if(ImGui.BeginMenu("销毁###CameraLifetime_destroy"))
            {
                if(ImGui.MenuItem("确认销毁###CameraLifetime_destroy_confirm"))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImGui.EndMenu();
            }
        }
    }
}
