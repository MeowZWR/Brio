using Brio.Capabilities.World;
using Brio.Config;
using Brio.Entities;
using Brio.Entities.World;
using Brio.Game.GPose;
using Brio.Game.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Linq;

namespace Brio.UI.Windows.Specialized;

public class LightWindow : Window, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configService;
    private readonly LightingService _lightingService;

    public LightWindow(EntityManager entityManager, LightingService lightingService, GPoseService gPoseService, ConfigurationService configService) : base($"{Brio.Name} - 灯光###brio_light_window")
    {
        Namespace = "brio_light_namespace";

        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _configService = configService;
        _lightingService = lightingService;

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = new(250, 300),
            MaximumSize = new(355, 750)
        };
        this.SizeConstraints = constraints;

        this.AllowBackgroundBlur = false;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override bool DrawConditions()
    {
        return base.DrawConditions();
    }

    private readonly ITransformableEditor _lightTransformEditor = new();

    public override void Draw()
    {
        ImBrio.BlurWindow();

        ImBrio.VerticalPadding(2);

        if(_configService.Configuration.Posing.AutoSelectLightWhenClickingOnALight && _entityManager.SelectedEntity is LightEntity lightEntity)
        {
            if(lightEntity != _lightingService.SelectedLightEntity)
            {
                _lightingService.SelectedLightEntity = lightEntity;
            }
        }

        ImGui.Text("选择要编辑的灯光:");
        ImBrio.CenterNextElementWithPadding(15);
        using(ImRaii.Disabled(_lightingService.SpawnedLightEntitiesCount == 0))
            if(ImGui.BeginCombo("###setlight"u8, $"{_lightingService.SelectedLightEntity?.FriendlyName}"))
            {
                foreach(var value in _lightingService.SpawnedLightEntities)
                {
                    if(ImGui.Selectable($"选择灯光: [ {value.FriendlyName} ]"))
                    {
                        _lightingService.SelectedLightEntity = value;
                    }
                }
                ImGui.EndCombo();
            }
            else
                WindowName = $"{Brio.Name} - 灯光###brio_light_window";

        ImBrio.AttachToolTip("当前灯光");

        ImBrio.VerticalPadding(5);

        ImGui.Separator();

        if(_lightingService.SelectedLightEntity is null || _lightingService.SelectedLightEntity.GameLight.IsValid == false)
        {
            _lightingService.SelectedLightEntity = _lightingService.SpawnedLightEntitiesCount > 0
                ? _lightingService.SpawnedLightEntities.First()
                : null;
        }

        //
        // Hedder

        if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "生成新灯光..."))
        {
            SpawnMenu.OpenUnifiedSpawnMenu();
        }

        ImBrio.VerticalSeparator(25);

        LightLifetimeCapability? light = null;
        if(!_lightingService.SelectedLightEntity?.TryGetCapability<LightLifetimeCapability>(out light) ?? false)
            WindowName = $"{Brio.Name} - 灯光###brio_light_window";
        else

            WindowName = $"{Brio.Name} - LIGHT - {light?.Entity.FriendlyName}###brio_light_window";

        using(ImRaii.Disabled(_lightingService!.SelectedLightEntity is null))
        {
            if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "克隆灯光", light?.CanClone ?? false))
            {
                light!.Clone();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("lifetimewidget_move", FontAwesomeIcon.ArrowUp, "移动到相机"))
            {
                light!.MoveToCamera();
            }

            ImBrio.VerticalSeparator(25);

            if(ImBrio.FontIconButton("lifetimewidget_destroy", FontAwesomeIcon.Trash, "销毁灯光", light?.CanDestroy ?? false))
            {
                light!.Destroy();
            }

            ImBrio.VerticalSeparator(25);

            if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "重命名灯光"))
            {
                ModalManager.Instance.OpenRenameModal(light!.Entity);
            }
        }

        if(_lightingService.SelectedLightEntity is null || _lightingService.SelectedLightEntity.GameLight.IsValid == false)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "没有可用的有效灯光。");
            return;
        }

        if(!_lightingService.SelectedLightEntity.TryGetCapability<LightTransformCapability>(out var lightGizmo))
        {
            return;
        }
        if(!_lightingService.SelectedLightEntity.TryGetCapability<LightRenderingCapability>(out var lightRender))
        {
            return;
        }

        //
        // Body

        if(ImGui.CollapsingHeader("灯光变换"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            LightEditor.DrawLightTransformHeader(lightGizmo);
            _lightTransformEditor.Draw($"light_transform_{lightGizmo.Entity.Id}", lightGizmo.Light, 0.1f);
        }

        if(ImGui.CollapsingHeader("灯光变换"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            LightEditor.DrawLightProperties(lightRender);
        }

        ImBrio.VerticalPadding(5);

        if(ImGui.CollapsingHeader("高级阴影设置"u8, ImGuiTreeNodeFlags.None))
        {
            LightEditor.DrawAdvancedShadows(lightRender);
        }
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(!newState)
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
