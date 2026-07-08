using Brio.Capabilities.World;
using Brio.Game.World.Interop;
using Brio.Input;
using Brio.Services;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class LightEditor
{
    public static unsafe void DrawAdvancedShadows(LightRenderingCapability Capability)
    {
        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->RenderLight : null;
        if(light == null) return;

        // Falloff Mode
        ImGui.Text("灯光衰减模式："u8);
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.BeginCombo("###falloffMode"u8, $"{light->FalloffType.ToString()}"))
        {
            foreach(var value in Enum.GetValues<FalloffType>())
            {
                if(ImGui.Selectable(value.ToString(), light->FalloffType == value))
                {
                    light->FalloffType = value;
                }
            }
            ImGui.EndCombo();
        }
        ImBrio.AttachToolTip("灯光衰减模式");

        // Shadows
        //

        ImGui.Text("角色阴影范围："u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowRange"u8, ref light->CharacterShadowRange, 0.1f, 0.001f, 1000.0f);

        ImGui.Text("阴影平面近端："u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowNear"u8, ref light->ShadowPlaneNear, 0.01f, 0.001f, 1000.0f);

        ImGui.Text("阴影平面远端："u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowFar"u8, ref light->ShadowPlaneFar, 0.01f, 0.001f, 1000.0f);
    }

    public static unsafe void DrawAdvancedSettings(LightRenderingCapability Capability)
    {
        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->RenderLight : null;
        if(light == null) return;

        ImBrio.VerticalPadding(5);
        ImGui.Text("角色阴影范围:"u8);


        ImBrio.VerticalPadding(5);
    }

    public static unsafe void DrawLightProperties(LightRenderingCapability Capability)
    {

        //
        // Hedder Buttons


        //ImGui.SameLine();

        //if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "Reset Light Properties", Capability.HasOverride))
        //{
        //    Capability.Reset();
        //}

        //
        // Body 

        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->RenderLight : null;
        if(light == null) return;

        if(Capability.SelectedLightType == -1)
        {
            switch(light->EmissionType)
            {
                case LightType.SpotLight:
                    Capability.SelectedLightType = 0;
                    break;
                case LightType.PointLight:
                    Capability.SelectedLightType = 1;
                    break;
                case LightType.FlatLight:
                    Capability.SelectedLightType = 2;
                    break;
                case LightType.WorldLight:
                    Capability.SelectedLightType = 3;
                    break;
            }
        }

        if(ImBrio.ButtonSelectorStrip("light_type", Vector2.Zero, ref Capability.SelectedLightType, ["聚光", "点光", "平面", "世界"]))
        {
            switch(Capability.SelectedLightType)
            {
                case 0:
                    light->EmissionType = LightType.SpotLight;
                    break;
                case 1:
                    light->EmissionType = LightType.PointLight;
                    break;
                case 2:
                    light->EmissionType = LightType.FlatLight;
                    break;
                case 3:
                    light->EmissionType = LightType.WorldLight;
                    break;
            }
        }

        switch(light->EmissionType)
        {
            case LightType.SpotLight:
                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightAngle"u8, ref light->SpotLightAngleDegrees, 0.0f, 180.0f, "%0.0f Degrees"u8);
                ImBrio.AttachToolTip("聚光灯角度");

                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightSmothing"u8, ref light->AngularFalloffDegrees, 0.0f, 180.0f, "%0.0f Degrees"u8);
                ImBrio.AttachToolTip("聚光灯平滑");
                break;

            case LightType.FlatLight:
                float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
                float padding = 10f;
                float full = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X - padding;
                float half = (full - spacing) / 2;

                ImBrio.CenterNextElementWithPadding(15);
                using(ImRaii.ItemWidth(half))
                {
                    ImGui.SliderAngle("###lightAngle_x"u8, ref light->FlatLightSkewAngleDegrees.X, -90, 90);
                    ImBrio.AttachToolTip("平面光 X");

                    ImGui.SameLine(0, spacing);

                    ImGui.SliderAngle("###lightAngle_y"u8, ref light->FlatLightSkewAngleDegrees.Y, -90, 90);
                    ImBrio.AttachToolTip("平面光 Y");
                }

                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightAngleSlider"u8, ref light->AngularFalloffDegrees, 0.0f, 180.0f, "%0.0f Degrees"u8);
                ImBrio.AttachToolTip("平面光衰减");
                break;
        }

        // Falloff Power
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###falloffPower"u8, ref light->FalloffFactor, 0.01f, 0.0f, 1000.0f);
        ImBrio.AttachToolTip("光照衰减类型");

        // Range
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.DragFloat("###lightRange"u8, ref light->Range, 0.1f, 0, 900))
            Capability.GameLight.NeedsUpdate = true;
        ImBrio.AttachToolTip("灯光范围");

        //

        ImBrio.VerticalPadding(5);
        ImBrio.SeparatorText("颜色与强度");

        var color = Vector3.SquareRoot(light->Color / 6);
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.ColorEdit3("###colorEdit3"u8, ref color, ImGuiColorEditFlags.Hdr))
        {
            light->Color = color * color * 6;
        }
        ImBrio.AttachToolTip("灯光颜色");

        var intensity = light->Intensity;
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.DragFloat("###intensity"u8, ref intensity, 0.01f, 0.0f, 100.0f))
        {
            light->Intensity = intensity;
        }
        ImBrio.AttachToolTip("强度");

        //

        ImBrio.VerticalPadding(5);
        ImBrio.SeparatorText("阴影与反射");

        var flag = light->LightFlags.HasFlag(LightFlags.Reflection);
        if(ImGui.Checkbox("启用材质反射"u8, ref flag))
        {
            light->LightFlags ^= LightFlags.Reflection;
        }

        bool[] bools =
        [
            light->LightFlags.HasFlag(LightFlags.CharaShadow),
            light->LightFlags.HasFlag(LightFlags.ObjectShadow),
            light->LightFlags.HasFlag(LightFlags.Dynamic),
        ];
        if(ImBrio.ToggleSelecterStrip("shadows_enable", Vector2.Zero, ref bools, ["角色", "物体", "动态"], "阴影"))
        {
            SetFlag(light, LightFlags.CharaShadow, bools[0]);
            SetFlag(light, LightFlags.ObjectShadow, bools[1]);
            SetFlag(light, LightFlags.Dynamic, bools[2]);

            static void SetFlag(LightRenderObject* light, LightFlags flag, bool enabled)
            {
                if(enabled) light->LightFlags |= flag;
                else light->LightFlags &= ~flag;
            }
        }
    }

    public static unsafe void DrawLightTransformHeader(LightTransformCapability Capability)
    {
        var overlayOpen = Capability.OverlayOpen;
        if(ImBrio.FontIconButton($"overlay_{Capability.Entity.Id}", overlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, overlayOpen ? "关闭叠加层" : "打开叠加层"))
        {
            Capability.OverlayOpen = !overlayOpen;
        }

        ImBrio.VerticalSeparator(24);

        if(ImBrio.ToggelFontIconButton($"save_{Capability.Entity.Id}", FontAwesomeIcon.BookBookmark, new Vector2(25, 0), false, tooltip: "灯光预设"))
        {
            ImGui.OpenPopup($"DrawPresetPopup");
        }

        FileUIHelpers.DrawPresetPopup(PresetType.Light, Capability.Entity);

        ImBrio.VerticalSeparator(24);

        if(ImBrio.FontIconButton($"undo_{Capability.Entity.Id}", FontAwesomeIcon.Reply, "撤销", Capability.CanUndo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Undo) && Capability.CanUndo))
        {
            Capability.Undo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton($"redo_{Capability.Entity.Id}", FontAwesomeIcon.Share, "重做", Capability.CanRedo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Redo) && Capability.CanRedo))
        {
            Capability.Redo();
        }

        ImBrio.VerticalSeparator(24);

        if(ImBrio.ToggelFontIconButton($"###togglegizmo_{Capability.Entity.Id}", FontAwesomeIcon.CompressArrowsAlt, Vector2.Zero, Capability.IsAdvancedGismoVisible, tooltip: Capability.IsAdvancedGismoVisible ? "禁用高级变换器" : "启用高级变换器"))
        {
            Capability.IsAdvancedGismoVisible = !Capability.IsAdvancedGismoVisible;
        }

        ImGui.SameLine();

        if(ImBrio.ToggelFontIconButton("togglelight", FontAwesomeIcon.Lightbulb, Vector2.Zero, Capability.GameLight.IsVisible, tooltip: Capability.GameLight.IsVisible ? "关闭灯光" : "打开灯光"))
        {
            Capability.GameLight.ToggleLight();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight($"reset_{Capability.Entity.Id}", FontAwesomeIcon.Undo, 1, "重置灯光变换", Capability.HasOverride))
        {
            Capability.Reset();
        }
    }
}
