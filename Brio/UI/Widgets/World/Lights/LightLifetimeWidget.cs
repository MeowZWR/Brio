using Brio.Capabilities.World;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.World;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace Brio.UI.Widgets.World.Lights;

public class LightLifetimeWidget : Widget<LightLifetimeCapability>
{
    private readonly ActorSpawnService _actorSpawnService;
    private readonly VirtualCameraManager _cameraManager;
    private readonly LightingService _lightingService;

    public LightLifetimeWidget(LightLifetimeCapability lightLifetimeCapability, ActorSpawnService actorSpawnService, VirtualCameraManager cameraManager, LightingService lightingService) : base(lightLifetimeCapability)
    {
        _actorSpawnService = actorSpawnService;
        _cameraManager = cameraManager;
        _lightingService = lightingService;
    }

    public override string HeaderName => "Lifetime";
    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "新建灯光"))
        {
            ImGui.OpenPopup("UnifiedSpawnMenuPopup");
        }
        SpawnMenuEditor.DrawUnifiedSpawnMenu(_actorSpawnService, _cameraManager, _lightingService);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "克隆灯光", Capability.CanClone))
        {
            Capability.Clone();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_destroy", FontAwesomeIcon.Trash, "删除灯光", Capability.CanDestroy))
        {
            Capability.Destroy();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "重命名灯光"))
        {
            RenameActorModal.Open(Capability.Entity);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight($"lifetimewidget_openAdvaned", FontAwesomeIcon.SquareArrowUpRight, 1, Capability.IsLightWindowOpen ? "关闭灯光窗口" : "打开灯光窗口"))
        {
            Capability.ToggleLightWindow();
        }
    }

    public override void DrawPopup()
    {
        if(Capability.CanClone)
        {
            if(ImGui.MenuItem("克隆###actorlifetime_clone"))
            {
                Capability.Clone();
            }
        }

        if(Capability.CanDestroy)
        {
            if(ImGui.MenuItem("删除###actorlifetime_destroy"))
            {
                Capability.Destroy();
            }
        }

        if(ImGui.MenuItem($"重命名 {Capability.Entity.FriendlyName}###actorlifetime_rename"))
        {
            ImGui.CloseCurrentPopup();

            RenameActorModal.Open(Capability.Entity);
        }

        if(ImGui.MenuItem("打开灯光窗口###actorlifetime_lightwindow"))
        {
            Capability.OpenLightWindow();
        }
    }
}
