using Brio.Entities;
using Brio.Entities.Actor;
using ImGuiNET;
using System;

namespace Brio.UI.Controls.Stateless;

internal partial class ImBrio
{
    public static void DrawApplyToActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(entityManager.SelectedEntity is null || entityManager.SelectedEntity is not ActorEntity selectedActor)
        {
            ImGui.BeginDisabled();
            ImGui.Button($"选择一个参与者");
            ImGui.EndDisabled();
        }
        else
        {
            if(ImGui.Button($"应用到{selectedActor.FriendlyName}"))
            {
                callback?.Invoke(selectedActor);
            }
        }
    }
}
