﻿using Brio.Entities;
using Brio.Entities.Actor;
using ImGuiNET;
using System;
using System.Numerics;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Controls.Stateless;

public partial class ImBrio
{
    public static void DrawApplyToActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(entityManager.SelectedEntity is null || entityManager.SelectedEntity is not ActorEntity selectedActor)
        {
            DrawSpawnActor(entityManager, callback);
            
            return;
        }

        if(ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl))
        {
            DrawSpawnActor(entityManager, callback);
        }
        else
        {
            if(ImGui.Button($"应用到{selectedActor.FriendlyName}"))
            {
                callback?.Invoke(selectedActor);
            }


            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("按住Ctrl生成新角色");
        }

    }

    private static void DrawSpawnActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(!Brio.TryGetService(out ActorSpawnService spawnService))
        {
            using var _ = ImRaii.Disabled(true);
            ImGui.Button("无法生成");
        }


        if(ImGui.Button("生成为新角色"))
        {
            if(!spawnService.CreateCharacter(out var character, disableSpawnCompanion: true))
            {
                Brio.Log.Error("无法生成角色");
                return;
            }

            unsafe bool IsReadyToDraw() => character.Native()->IsReadyToDraw();

            Brio.Framework.RunUntilSatisfied(
                IsReadyToDraw,
                (_) =>
                {
                    var entity = entityManager.GetEntity(new EntityId(character));
                    if(entity is not ActorEntity actorEntity)
                    {
                        Brio.Log.Error($"无法获取角色实体：{entity?.GetType()} {entity}");
                        return;
                    }

                    callback?.Invoke(actorEntity);
                },
                100,
                dontStartFor: 2
            );
        }
    }
}
