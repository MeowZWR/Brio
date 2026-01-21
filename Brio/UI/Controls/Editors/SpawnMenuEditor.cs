using Brio.Entities.Camera;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.World;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public static class SpawnMenuEditor
{
    private const float MenuWidth = 220f;

    public static void DrawUnifiedSpawnMenu(
        ActorSpawnService? actorSpawnService = null,
        VirtualCameraManager? cameraManager = null,
        LightingService? lightingService = null)
    {
        using var popup = ImRaii.Popup("UnifiedSpawnMenuPopup");
        if(!popup.Success)
            return;

        var buttonSize = new Vector2(MenuWidth * ImGuiHelpers.GlobalScale, 0);

        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            // Actor spawn
            if(actorSpawnService != null)
            {
                ImGui.Text("角色");
                ImGui.Separator();

                if(ImBrio.DrawIconButton(FontAwesomeIcon.User, "生成新角色", buttonSize))
                {
                    actorSpawnService.CreateCharacter(out _, SpawnFlags.Default, true);
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.PlusSquare, "生成带有宠物栏的角色", buttonSize))
                {
                    actorSpawnService.CreateCharacter(out _, SpawnFlags.ReserveCompanionSlot, false);
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Cubes, "生成道具", buttonSize))
                {
                    actorSpawnService.SpawnNewProp(out _);
                    ImGui.CloseCurrentPopup();
                }
            }

            // Camera spawn
            if(cameraManager != null)
            {
                if(actorSpawnService != null)
                    ImGui.Spacing();

                ImGui.Text("相机");
                ImGui.Separator();

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Camera, "新Brio相机", buttonSize))
                {
                    cameraManager.CreateCamera(CameraType.Game);
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Video, "新自由相机", buttonSize))
                {
                    cameraManager.CreateCamera(CameraType.Free);
                    ImGui.CloseCurrentPopup();
                }
            }

            // Light spawn
            if(lightingService != null)
            {
                if(actorSpawnService != null || cameraManager != null)
                    ImGui.Spacing();

                ImGui.Text("灯光");
                ImGui.Separator();

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Lightbulb, "生成聚光灯", buttonSize))
                {
                    lightingService.SpawnLight(LightType.SpotLight);
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Circle, "生成区域光", buttonSize))
                {
                    lightingService.SpawnLight(LightType.AreaLight);
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Square, "生成平面光", buttonSize))
                {
                    lightingService.SpawnLight(LightType.FlatLight);
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }
}

