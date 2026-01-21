using Brio.Capabilities.Camera;
using Brio.Game.Actor;
using Brio.Game.World;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Camera;

public class CameraLifetimeWidget : Widget<CameraLifetimeCapability>
{
    private readonly ActorSpawnService _actorSpawnService;
    private readonly LightingService _lightingService;

    public CameraLifetimeWidget(CameraLifetimeCapability capability, ActorSpawnService actorSpawnService, LightingService lightingService) : base(capability)
    {
        _actorSpawnService = actorSpawnService;
        _lightingService = lightingService;
    }

    public override string HeaderName => "生命周期";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImBrio.FontIconButton("CameraLifetime_spawnnew", FontAwesomeIcon.Plus, "新建相机"))
            {
                ImGui.OpenPopup("UnifiedSpawnMenuPopup");
            }
            SpawnMenuEditor.DrawUnifiedSpawnMenu(_actorSpawnService, Capability.VirtualCameraManager, _lightingService);

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_clone", FontAwesomeIcon.Clone, "克隆相机"))
            {
                Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
            }

            ImGui.SameLine();

            using(ImRaii.Disabled(Capability.CameraEntity.CameraID == 0))
            {
                if(ImBrio.FontIconButton("CameraLifetime_destroy", FontAwesomeIcon.Trash, "销毁相机", Capability.CanDestroy))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImGui.SameLine();

                if(ImBrio.FontIconButton("CameraLifetime_rename", FontAwesomeIcon.Signature, "重命名"))
                {
                    RenameActorModal.Open(Capability.Entity);
                }
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_target", FontAwesomeIcon.Bullseye, "选中相机"))
            {
                Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
            }

        }
    }

    public override void DrawPopup()
    {
        if(Capability.IsAllowed == false)
            return;

        if(ImGui.MenuItem("选中###CameraLifetime_target"))
        {
            Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
        }

        if(ImGui.MenuItem("克隆###CameraLifetime_clone"))
        {
            Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
        }

        if(Capability.CanDestroy)
        {
            if(ImGui.BeginMenu("销毁###actorlifetime_destroy"))
            {
                if(ImGui.MenuItem("Confirm Destruction###CameraLifetime_destroy_confirm"))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImGui.EndMenu();
            }


            if(ImGui.MenuItem($"重命名 {Capability.CameraEntity.FriendlyName}###CameraLifetime_rename"))
            {
                ImGui.CloseCurrentPopup();

                RenameActorModal.Open(Capability.Entity);
            }
        }
    }
}
