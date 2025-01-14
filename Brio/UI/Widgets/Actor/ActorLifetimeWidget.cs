using Brio.Capabilities.Actor;
using Brio.UI.Controls;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using ImGuiNET;

namespace Brio.UI.Widgets.Actor;

internal class ActorLifetimeWidget(ActorLifetimeCapability capability) : Widget<ActorLifetimeCapability>(capability)
{
    public override string HeaderName => "Lifetime";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("lifetimewidget_spawn_prop", FontAwesomeIcon.Cubes, "生成道具"))
        {
            Capability.SpawnNewProp(false);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "生成新角色"))
        {
            Capability.SpawnNewActor(false, false, true);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_spawnnewwithcompanionslot", FontAwesomeIcon.PlusSquare, "生成新角色且带有宠物栏"))
        {
            Capability.SpawnNewActor(false, true, false);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "克隆", Capability.CanClone))
        {
            Capability.Clone(false);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_destroy", FontAwesomeIcon.Trash, "删除", Capability.CanDestroy))
        {
            Capability.Destroy();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_target", FontAwesomeIcon.Bullseye, "目标"))
        {
            Capability.Target();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "重命名"))
        {
            RenameActorModal.Open(Capability.Actor);
        }
    }

    public override void DrawPopup()
    {
        if(Capability.CanClone)
        {
            if(ImGui.MenuItem("克隆###actorlifetime_clone"))
            {
                Capability.Clone(true);
            }
        }

        if(Capability.CanDestroy)
        {
            if(ImGui.MenuItem("删除###actorlifetime_destroy"))
            {
                Capability.Destroy();
            }
        }

        if(ImGui.MenuItem("选中###actorlifetime_target"))
        {
            Capability.Target();
        }

        if(ImGui.MenuItem($"重命名{Capability.Actor.FriendlyName}###actorlifetime_rename"))
        {
            ImGui.CloseCurrentPopup();

            RenameActorModal.Open(Capability.Actor);
        }
    }
}
