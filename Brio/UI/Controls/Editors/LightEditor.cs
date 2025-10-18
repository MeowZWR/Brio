using Brio.Capabilities.World;
using Brio.Core;
using Brio.Game.World;
using Brio.Input;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class LightEditor
{
    public unsafe static void DrawSpawnMenu(LightingService lightingService)
    {
        using var popup = ImRaii.Popup("DrawLightSpawnMenuPopup");
        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                if(ImGui.Button("生成聚光灯"u8, new(125 * ImGuiHelpers.GlobalScale, 0)))
                {
                    lightingService.SpawnLight(LightType.SpotLight);
                }

                if(ImGui.Button("生成点光源"u8, new(125 * ImGuiHelpers.GlobalScale, 0)))
                {
                    lightingService.SpawnLight(LightType.AreaLight);
                }

                if(ImGui.Button("生成平面光"u8, new(125 * ImGuiHelpers.GlobalScale, 0)))
                {
                    lightingService.SpawnLight(LightType.FlatLight);
                }
            }
        }
    }

    public static unsafe void DrawAdvancedShadows(LightRenderingCapability Capability)
    {
        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->LightRenderObject : null;
        if(light == null) return;

        ImGui.Text("角色阴影范围:"u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowRange"u8, ref light->CharacterShadowRange, 0.1f, 0.001f, 1000.0f);

        ImGui.Text("阴影平面近端:"u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowNear"u8, ref light->ShadowPlaneNear, 0.01f, 0.001f, 1000.0f);

        ImGui.Text("阴影平面远端:"u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowFar"u8, ref light->ShadowPlaneFar, 0.01f, 0.001f, 1000.0f);
    }

    public static unsafe void DrawAdvancedSettings(LightRenderingCapability Capability)
    {
        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->LightRenderObject : null;
        if(light == null) return;

        ImBrio.VerticalPadding(5);
        ImGui.Text("衰减模式 / 功率与光照范围"u8);

        // Falloff Mode
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.BeginCombo("###falloffMode"u8, $"{Localize.Get($"falloff_type.{light->FalloffType}")}"))
        {
            foreach(var value in Enum.GetValues<FalloffType>())
            {
                if(ImGui.Selectable(Localize.Get($"falloff_type.{value}"), light->FalloffType == value))
                {
                    light->FalloffType = value;
                }
            }
            ImGui.EndCombo();
        }
        ImBrio.AttachToolTip("光照衰减类型");

        // Falloff Power
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###falloffPower"u8, ref light->Falloff, 0.01f, 0.0f, 1000.0f);
        ImBrio.AttachToolTip("光照衰减功率");

        // Range
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.DragFloat("###lightRange"u8, ref light->Range, 0.1f, 0, 900))
            Capability.GameLight.NeedsUpdate = true;
        ImBrio.AttachToolTip("光照范围");

        ImBrio.VerticalPadding(5);
    }

    public static unsafe void DrawLightProperties(LightRenderingCapability Capability)
    {

        //
        // Hedder Buttons

        if(ImBrio.ToggelFontIconButton("togglelight", FontAwesomeIcon.Lightbulb, Vector2.Zero, Capability.GameLight.IsVisible, hoverText: Capability.GameLight.IsVisible ? "关闭灯光" : "开启灯光"))
        {
            Capability.GameLight.ToggleLight();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "重置灯光属性", Capability.HasOverride))
        {
            Capability.Reset();
        }

        //
        // Body 

        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->LightRenderObject : null;
        if(light == null) return;

        ImGui.Separator();
        ImGui.Text("灯光类型"u8);

        if(Capability.SelectedLightType == -1)
        {
            switch(light->EmissionType)
            {
                case LightType.SpotLight:
                    Capability.SelectedLightType = 0;
                    break;
                case LightType.AreaLight:
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

        if(ImBrio.ButtonSelectorStrip("light_type", Vector2.Zero, ref Capability.SelectedLightType, ["聚光灯", "点光源", "平面光", "环境光"]))
        {
            switch(Capability.SelectedLightType)
            {
                case 0:
                    light->EmissionType = LightType.SpotLight;
                    break;
                case 1:
                    light->EmissionType = LightType.AreaLight;
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
                ImGui.SliderFloat("###lightAngle"u8, ref light->LightAngle, 0.0f, 180.0f, "%0.0f 度"u8);
                ImBrio.AttachToolTip("聚光灯角度");

                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightSmothing"u8, ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f 度"u8);
                ImBrio.AttachToolTip("聚光灯平滑度");
                break;

            case LightType.FlatLight:
                using(ImRaii.ItemWidth((ImGui.CalcItemWidth() / 2) - ImGui.GetStyle().ItemInnerSpacing.X))
                {
                    ImGui.SliderAngle("###lightAngle_x"u8, ref light->Angle.X, -90, 90);
                    ImBrio.AttachToolTip("平面光X轴");

                    ImGui.SameLine();

                    ImGui.SliderAngle("###lightAngle_y"u8, ref light->Angle.Y, -90, 90);
                    ImBrio.AttachToolTip("平面光Y轴");
                }

                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightAngleSlider"u8, ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f 度"u8);
                ImBrio.AttachToolTip("平面光衰减");
                break;
        }

        //

        ImBrio.VerticalPadding(5);
        ImGui.Separator();
        ImGui.Text("颜色与强度"u8);

        var color = Vector3.SquareRoot(light->Color / 6);
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.ColorEdit3("###colorEdit3"u8, ref color, ImGuiColorEditFlags.Hdr))
        {
            light->Color = color * color * 6;
        }
        ImBrio.AttachToolTip("灯光颜色");

        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###intensity"u8, ref light->Intensity, 0.01f, 0.0f, 100.0f);
        ImBrio.AttachToolTip("强度");

        //

        ImBrio.VerticalPadding(5);
        ImGui.Separator();
        ImGui.Text("阴影与反射"u8);

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

    public static unsafe void DrawLightTransform(LightTransformCapability Capability, ref bool activeState)
    {
        //
        // Hedder Buttons

        var overlayOpen = Capability.OverlayOpen;
        if(ImBrio.FontIconButton($"overlay_{Capability.Entity.Id}", overlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, overlayOpen ? "关闭叠加层" : "打开叠加层"))
        {
            Capability.OverlayOpen = !overlayOpen;
        }

        ImGui.SameLine();

        if(ImBrio.ToggelFontIconButton($"###togglegizmo_{Capability.Entity.Id}", FontAwesomeIcon.Crosshairs, Vector2.Zero, Capability.IsGismoVisible, hoverText: Capability.IsGismoVisible ? "禁用强制显示小工具" : "强制显示小工具"))
        {
            Capability.IsGismoVisible = !Capability.IsGismoVisible;
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton($"undo_{Capability.Entity.Id}", FontAwesomeIcon.Backward, "撤销", Capability.CanUndo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Undo) && Capability.CanUndo))
        {
            Capability.Undo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton($"redo_{Capability.Entity.Id}", FontAwesomeIcon.Forward, "重做", Capability.CanRedo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Redo) && Capability.CanRedo))
        {
            Capability.Redo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight($"reset_{Capability.Entity.Id}", FontAwesomeIcon.Undo, 1, "重置灯光变换", Capability.HasOverride))
        {
            Capability.Reset();
        }

        //
        // Body 

        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->LightRenderObject : null;
        if(light == null) return;

        using var child1 = ImRaii.Child("LightPropertiesChild", new Vector2(0, (35 * 3) * ImGuiHelpers.GlobalScale), true);
        if(child1.Success)
        {
            bool didChange = false;
            bool anyActive = false;

            if(Capability.rotation == Vector3.Zero)
                Capability.rotation = (Capability.Transform.Rotation = light->Transform->Rotation).EulerAngles;
            if(Capability.position == Vector3.Zero)
                Capability.position = (Capability.Transform.Position = light->Transform->Position);
            if(Capability.scale == Vector3.Zero)
                Capability.scale = (Capability.Transform.Scale = light->Transform->Scale);

            ImBrio.VerticalPadding(2);
            (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transform_Light_Position_0", ref Capability.position, 0.1f, FontAwesomeIcon.ArrowsUpDownLeftRight, "位置", enableExpanded: false);
            ImBrio.VerticalPadding(2);
            (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transform_Light_Rotation_0", ref Capability.rotation, 1f * 10, FontAwesomeIcon.ArrowsSpin, "旋转", enableExpanded: false);
            ImBrio.VerticalPadding(2);
            (var sdidChange, var sanyActive) = ImBrio.DragFloat3($"###_transform_Light_Scale_0", ref Capability.scale, 1f, FontAwesomeIcon.ExpandAlt, "缩放", enableExpanded: false);

            didChange |= pdidChange |= rdidChange |= sdidChange;
            anyActive |= panyActive |= ranyActive |= sanyActive;

            if(anyActive)
            {
                Capability.IsTransformDraggingActive = true;
                activeState = true;

                light->Transform->Position = Capability.Transform.Position = Capability.position;
                light->Transform->Rotation = Capability.Transform.Rotation = Capability.rotation.ToEulerAngles();
                light->Transform->Scale = Capability.Transform.Scale = Capability.scale;
            }
            else if(Capability.IsTransformDraggingActive && activeState)
            {
                Capability.IsTransformDraggingActive = false;
                activeState = false;

                Capability.Transform.Position = Capability.position;
                Capability.Transform.Rotation = Capability.rotation.ToEulerAngles();
                Capability.Transform.Scale = Capability.scale;
                Capability.Snapshot();
            }
        }
    }
}
