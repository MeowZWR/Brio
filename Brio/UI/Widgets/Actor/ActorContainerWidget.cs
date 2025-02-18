﻿using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

public class ActorContainerWidget(ActorContainerCapability capability) : Widget<ActorContainerCapability>(capability)
{
    public override string HeaderName => "角色";
    public override WidgetFlags Flags
    {
        get
        {
            WidgetFlags flags = WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons;

            if(Capability.CanControlCharacters)
                flags |= WidgetFlags.DrawPopup | WidgetFlags.CanHide;

            return flags;
        }
    }

    private ActorEntity? _selectedActor;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(!Capability.CanControlCharacters))
        {
            bool hasSelection = _selectedActor != null;

            if(ImBrio.FontIconButton("containerwidget_spawnbasic", FontAwesomeIcon.Plus, "生成"))
            {
                Capability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_spawnattachments", FontAwesomeIcon.PlusSquare, "生成且带有宠物栏"))
            {
                Capability.CreateCharacter(true, true);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_clone", FontAwesomeIcon.Clone, "克隆", hasSelection))
            {
                Capability.CloneActor(_selectedActor!, false);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_destroy", FontAwesomeIcon.Trash, "销毁", hasSelection))
            {
                Capability.DestroyCharacter(_selectedActor!);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_target", FontAwesomeIcon.Bullseye, "目标", hasSelection))
            {
                Capability.Target(_selectedActor!);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_selectinhierarchy", FontAwesomeIcon.FolderTree, "在层次结构中选择", hasSelection))
            {
                Capability.SelectInHierarchy(_selectedActor!);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_destroyall", FontAwesomeIcon.Bomb, "全部销毁"))
            {
                Capability.DestroyAll();
            }
        }
    }

    public override void DrawBody()
    {
        if(ImGui.BeginListBox($"###actorcontainerwidget_{Capability.Entity.Id}_list", new Vector2(-1, 150)))
        {
            foreach(var child in Capability.Entity.Children)
            {
                if(child is ActorEntity actorEntity)
                {
                    bool isSelected = actorEntity.Equals(_selectedActor);
                    if(ImGui.Selectable($"{child.FriendlyName}###actorcontainerwidget_{Capability.Entity.Id}_item_{actorEntity.Id}", isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _selectedActor = actorEntity;
                    }
                }
            }

            ImGui.EndListBox();
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.MenuItem("生成###containerwidgetpopup_spawnbasic"))
        {
            Capability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
        }

        if(ImGui.MenuItem("生成（带宠物）###containerwidgetpopup_spawncompanion"))
        {
            Capability.CreateCharacter(true, true);
        }

        if(ImGui.MenuItem("全部销毁###containerwidgetpopup_destroyall"))
        {
            Capability.DestroyAll();
        }
    }
}
