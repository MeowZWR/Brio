using Brio.Capabilities.World;
using Brio.Game.World;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class EnvironmentEditorWidget(EnvironmentEditorCapability capability) : Widget<EnvironmentEditorCapability>(capability)
{
    public override string HeaderName => "环境";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

    int selected = 0;
    private readonly TextureSelector _textureSelector = new("particle_texture_selector", TextureType.Particle);

    public unsafe override void DrawBody()
    {
        ImBrio.ButtonSelectorStrip("environment_filters_selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["粒子", "雨", "风", "雾"]);

        var env = BrioEnvManager.Instance();
        if(env == null) return;

        Vector2 unlockPos;
        Vector2 preservedPOS;

        switch(selected)
        {
            case 0:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("粒子纹理："u8);

                var isParticles = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Particles);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetParticles", FontAwesomeIcon.Redo, 1, "重置粒子", bordered: false, enabled: isParticles))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Particles;
                ImGui.SetCursorPos(preservedPOS);

                var path = $"bgcommon/nature/dust/texture/dust_{Math.Max(0, env->EnvState.Particles.TextureId - 2):D3}.tex";
                if(ImBrio.BorderedGameTex("##particleTexturePreview", path))
                {
                    _textureSelector.Select(new TextureId(env->EnvState.Particles.TextureId));
                    ImGui.OpenPopup("particle_texture_selector"u8);
                }
                ImBrio.AttachToolTip("点击打开纹理选择器");

                bool didParticlesChange = false;

                using(var popup = ImRaii.Popup("particle_texture_selector"u8))
                {
                    if(popup.Success)
                    {
                        _textureSelector.Draw();

                        if(_textureSelector.SoftSelectionChanged && _textureSelector.SoftSelected != null)
                        {
                            env->EnvState.Particles.TextureId = _textureSelector.SoftSelected.Id;
                            Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Particles;
                            didParticlesChange = true;
                        }

                        if(_textureSelector.SelectionChanged)
                            ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();
                ImBrio.CenterNextElementWithPadding(10);
                ImBrio.VerticalPadding(5);
                didParticlesChange |= ImGui.InputUInt("###particleTexture"u8, ref env->EnvState.Particles.TextureId);
                ImBrio.AttachToolTip("粒子纹理 ID");

                ImBrio.VerticalPadding(5);
                ImGui.Text("粒子属性："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleIntensity"u8, ref env->EnvState.Particles.Intensity, 0.0f, 1.0f);
                ImBrio.AttachToolTip("粒子密度");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSize"u8, ref env->EnvState.Particles.Size, 0.0f, 20.0f);
                ImBrio.AttachToolTip("粒子大小");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.ColorEdit4("###particleColor"u8, ref env->EnvState.Particles.Color);
                ImBrio.AttachToolTip("粒子颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleGlow"u8, ref env->EnvState.Particles.Glow, 0.0f, 10.0f);
                ImBrio.AttachToolTip("粒子发光度");

                ImBrio.VerticalPadding(5);
                ImGui.Text("粒子子属性："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSpread"u8, ref env->EnvState.Particles.Spread, 0.0f, 10.0f);
                ImBrio.AttachToolTip("粒子扩散度/范围");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleWeight"u8, ref env->EnvState.Particles.Weight, 0.0f, 10.0f);
                ImBrio.AttachToolTip("粒子重量");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSpeed"u8, ref env->EnvState.Particles.Speed, 0.0f, 1.0f);
                ImBrio.AttachToolTip("粒子运动速度");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSpin"u8, ref env->EnvState.Particles.Spin, 0.05f, 5.0f);
                ImBrio.AttachToolTip("粒子旋转速度");

                ImBrio.VerticalPadding(3);

                if(didParticlesChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Particles;

                break;
            case 1:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("雨水属性："u8);

                var isRain = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Rain);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetRain", FontAwesomeIcon.Redo, 1, "重置雨水", bordered: false, enabled: isRain))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Rain;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didRainChange = ImGui.SliderFloat("###rainIntensity"u8, ref env->EnvState.Rain.Intensity, 0.0f, 1.0f);
                ImBrio.AttachToolTip("雨量强度");

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainThickness"u8, ref env->EnvState.Rain.Size, 0.0f, 1.0f);
                ImBrio.AttachToolTip("雨丝粗细");

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainWeight"u8, ref env->EnvState.Rain.Weight, 0.0f, 10.0f);
                ImBrio.AttachToolTip("雨水重量");

                ImBrio.VerticalPadding(5);
                ImGui.Text("颜色："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.ColorEdit4("###rainColor"u8, ref env->EnvState.Rain.Color);
                ImBrio.AttachToolTip("雨水颜色");

                ImBrio.VerticalPadding(5);
                ImGui.Text("进阶设置："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainScattering"u8, ref env->EnvState.Rain.Scatter, 0.0f, 10.0f);
                ImBrio.AttachToolTip("雨水散射");

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainRaindrops"u8, ref env->EnvState.Rain.Raindrops, 0.0f, 1.0f);
                ImBrio.AttachToolTip("雨滴效果");

                ImBrio.VerticalPadding(3);

                if(didRainChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Rain;

                break;
            case 2:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("风：方向 / 角度 / 速度"u8);

                var isWind = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Wind);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetWind", FontAwesomeIcon.Redo, 1, "重置风", bordered: false, enabled: isWind))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Wind;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didWindChange = ImBrio.SliderAngle("###windDirectionu", ref env->EnvState.Wind.Direction, 0.0f, MathF.PI);
                ImBrio.AttachToolTip("风向");

                ImBrio.CenterNextElementWithPadding(15);
                didWindChange |= ImBrio.SliderAngle("###windAngle", ref env->EnvState.Wind.Angle, 0.0f, 180.0f);
                ImBrio.AttachToolTip("风力角度");

                ImBrio.CenterNextElementWithPadding(15);
                didWindChange |= ImGui.SliderFloat("###windSpeed"u8, ref env->EnvState.Wind.Speed, -30.0f, 100f);
                ImBrio.AttachToolTip("风速");

                ImBrio.VerticalPadding(3);

                if(didWindChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Wind;

                break;
            case 3:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("雾气属性："u8);

                var isFog = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Fog);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetFog", FontAwesomeIcon.Redo, 1, "重置雾气", bordered: false, enabled: isFog))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Fog;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didFogChange = ImGui.ColorEdit4("###fogColor"u8, ref env->EnvState.Fog.Color);
                ImBrio.AttachToolTip("雾气颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###fogDistance"u8, ref env->EnvState.Fog.Distance, 0.0f, 1000f);
                ImBrio.AttachToolTip("雾气起始距离");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###fogThickness"u8, ref env->EnvState.Fog.Thickness, 0.0f, 50f);
                ImBrio.AttachToolTip("雾气厚度/浓度");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###fogOpacity"u8, ref env->EnvState.Fog.FogOpacity, 0.0f, 10f);
                ImBrio.AttachToolTip("雾气不透明度");

                ImBrio.VerticalPadding(5);
                ImGui.Text("天空不透明度与平滑度"u8);

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###skyOpacity"u8, ref env->EnvState.Fog.SkyOpacity, 0.0f, 10f);
                ImBrio.AttachToolTip("天空不透明度");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###skySmoothness"u8, ref env->EnvState.Fog.SkySmoothness, 0.0f, 1000f);
                ImBrio.AttachToolTip("天空平滑度");

                ImBrio.VerticalPadding(3);

                if(didFogChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Fog;

                break;
        }
    }
}
