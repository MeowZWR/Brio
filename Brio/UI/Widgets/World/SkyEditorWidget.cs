using Brio.Capabilities.World;
using Brio.Game.World;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class SkyEditorWidget(SkyEditorCapability skyEditorCapability) : Widget<SkyEditorCapability>(skyEditorCapability)
{
    public override string HeaderName => "环境光";
    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    int selected = 0;
    private readonly TextureSelector _skyTextureSelector = new("sky_texture_selector", TextureType.Sky, 450);
    private readonly TextureSelector _cloudTextureSelector = new("cloud_texture_selector", TextureType.Cloud, 75);
    private readonly TextureSelector _cloudSideTextureSelector = new("cloud_side_texture_selector", TextureType.CloudSide, 75);

    public unsafe override void DrawBody()
    {
        var env = BrioEnvManager.Instance();
        if(env == null) return;

        ImBrio.VerticalPadding(3);

        ImBrio.ButtonSelectorStrip("stars_filters_selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Sky", "Stars", "Clouds", "Indoors"]);

        switch(selected)
        {
            case 1:
                ImBrio.VerticalPadding(3);

                if(ImBrio.SeparatorTextButton("Stars", FontAwesomeIcon.Redo, "Reset All Stars Properties",
                    Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Stars)))
                {
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Stars;
                }

                ImBrio.CenterNextElementWithPadding(15);
                var didSkyChange2 = ImGui.SliderFloat("###starcount"u8, ref env->EnvState.Stars.StarCount, 0.0f, 20.0f);
                ImBrio.AttachToolTip("星星数量");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###starcountIntensity"u8, ref env->EnvState.Stars.StarIntensity, 0.0f, 2.5f);
                ImBrio.AttachToolTip("星星亮度");

                ImBrio.VerticalPadding(5);
                ImBrio.SeparatorText("月亮颜色与亮度");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.ColorEdit4("###moonColor"u8, ref env->EnvState.Stars.MoonColor);
                ImBrio.AttachToolTip("月亮颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###MoonBrightness"u8, ref env->EnvState.Stars.MoonBrightness, 0.0f, 1.0f);
                ImBrio.AttachToolTip("月亮亮度");

                ImBrio.VerticalPadding(5);
                ImBrio.SeparatorText("星座属性");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###constellationCount"u8, ref env->EnvState.Stars.ConstellationCount, 0.0f, 10.0f);
                ImBrio.AttachToolTip("星座数量");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###constellationsIntensity"u8, ref env->EnvState.Stars.ConstellationIntensity, 0.0f, 2.5f);
                ImBrio.AttachToolTip("星座亮度");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###galaxyIntensity"u8, ref env->EnvState.Stars.GalaxyIntensity, 0.0f, 10.0f);
                ImBrio.AttachToolTip("银河亮度");

                ImBrio.VerticalPadding(3);

                if(didSkyChange2)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Stars;

                break;
            case 0:
                ImBrio.VerticalPadding(3);

                if(ImBrio.SeparatorTextButton("Sky", FontAwesomeIcon.Redo, "Reset All Sky Properties",
                    Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Sky)))
                {
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Sky;
                }

                if(ImBrio.BorderedGameTex("##skyTexturePreview", _skyTextureSelector.GetTexturePath(env->EnvState.SkyTextureID)))
                {
                    _skyTextureSelector.Select(new TextureId(env->EnvState.SkyTextureID));
                    ImGui.OpenPopup("sky_texture_selector"u8);
                }
                ImBrio.AttachToolTip("点击打开纹理选择器");

                var didSkyChange = false;

                using(var popup = ImRaii.Popup("sky_texture_selector"u8))
                {
                    if(popup.Success)
                    {
                        _skyTextureSelector.Draw();

                        if(_skyTextureSelector.SoftSelectionChanged && _skyTextureSelector.SoftSelected != null)
                        {
                            env->EnvState.SkyTextureID = _skyTextureSelector.SoftSelected.Id;
                            Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Sky;
                            didSkyChange = true;
                        }

                        if(_skyTextureSelector.SelectionChanged)
                            ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();
                ImBrio.CenterNextElementWithPadding(10);
                ImBrio.VerticalPadding(5);
                didSkyChange |= ImGui.InputUInt("###SkyTextureID"u8, ref env->EnvState.SkyTextureID);
                ImBrio.AttachToolTip("天空纹理 ID");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange |= ImGui.SliderFloat("###fogSunVisibility"u8, ref env->EnvState.Fog.SunVisibility, 0.0f, 1f);
                ImBrio.AttachToolTip("太阳可见度");

                if(didSkyChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Sky;

                ImBrio.VerticalPadding(5);

                if(ImBrio.SeparatorTextButton("Ambient Lighting", FontAwesomeIcon.Redo, "Reset All Lighting Properties",
                    Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.EnvironmentLighting)))
                {
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.EnvironmentLighting;
                }

                ImBrio.VerticalPadding(2);
                ImBrio.SeparatorText("色温与饱和度");

                ImBrio.CenterNextElementWithPadding(15);
                var didSkyChange3 = ImGui.SliderFloat("###temperatureColor"u8, ref env->EnvState.EnvironmentLighting.AmbientTemperature, -2.5f, 2.5f);
                ImBrio.AttachToolTip("环境光色温");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange3 |= ImGui.SliderFloat("###saturationColor"u8, ref env->EnvState.EnvironmentLighting.AmbientSaturation, 0.0f, 5.0f);
                ImBrio.AttachToolTip("环境光饱和度");

                ImBrio.VerticalPadding(5);
                ImBrio.SeparatorText("环境光颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange3 |= ImGui.ColorEdit3("##ambientColor"u8, ref env->EnvState.EnvironmentLighting.AmbientColor);
                ImBrio.AttachToolTip("环境光颜色");

                ImBrio.VerticalPadding(5);
                ImBrio.SeparatorText("日光与月光颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange3 |= ImGui.ColorEdit3("###sunlightColor"u8, ref env->EnvState.EnvironmentLighting.SunlightColor);
                ImBrio.AttachToolTip("日光颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange3 |= ImGui.ColorEdit3("###moonlightColor"u8, ref env->EnvState.EnvironmentLighting.MoonlightColor);
                ImBrio.AttachToolTip("月光颜色");

                ImBrio.VerticalPadding(3);

                if(didSkyChange3)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.EnvironmentLighting;

                break;
            case 2:

                ImBrio.VerticalPadding(3);

                if(ImBrio.SeparatorTextButton("Cloud", FontAwesomeIcon.Redo, "Reset All Cloud Properties",
                    Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Clouds)))
                {
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Clouds;
                }

                if(ImBrio.BorderedGameTex("##cloudTexturePreview", _cloudTextureSelector.GetTexturePath(env->EnvState.Clouds.CloudTexture)))
                {
                    _cloudTextureSelector.Select(new TextureId(env->EnvState.Clouds.CloudTexture));
                    ImGui.OpenPopup("cloud_texture_selector"u8);
                }
                ImBrio.AttachToolTip("点击更改云层纹理");

                var didSkyChange4 = false;

                using(var popup = ImRaii.Popup("cloud_texture_selector"u8))
                {
                    if(popup.Success)
                    {
                        _cloudTextureSelector.Draw();

                        if(_cloudTextureSelector.SoftSelectionChanged && _cloudTextureSelector.SoftSelected != null)
                        {
                            env->EnvState.Clouds.CloudTexture = _cloudTextureSelector.SoftSelected.Id;
                            Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Clouds;
                            didSkyChange4 = true;
                        }

                        if(_cloudTextureSelector.SelectionChanged)
                            ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();
                ImBrio.CenterNextElementWithPadding(10);
                ImBrio.VerticalPadding(5);
                didSkyChange4 |= ImGui.InputUInt("###CloudTexture"u8, ref env->EnvState.Clouds.CloudTexture);
                ImBrio.AttachToolTip("云层纹理 ID");

                if(ImBrio.BorderedGameTex("##cloudSideTexturePreview", _cloudSideTextureSelector.GetTexturePath(env->EnvState.Clouds.CloudSideTexture)))
                {
                    _cloudSideTextureSelector.Select(new TextureId(env->EnvState.Clouds.CloudSideTexture));
                    ImGui.OpenPopup("cloud_side_texture_selector"u8);
                }
                ImBrio.AttachToolTip("点击更改云层侧面纹理");

                using(var popup = ImRaii.Popup("cloud_side_texture_selector"u8))
                {
                    if(popup.Success)
                    {
                        _cloudSideTextureSelector.Draw();

                        if(_cloudSideTextureSelector.SoftSelectionChanged && _cloudSideTextureSelector.SoftSelected != null)
                        {
                            env->EnvState.Clouds.CloudSideTexture = _cloudSideTextureSelector.SoftSelected.Id;
                            Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Clouds;
                            didSkyChange4 = true;
                        }

                        if(_cloudSideTextureSelector.SelectionChanged)
                            ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();
                ImBrio.CenterNextElementWithPadding(10);
                ImBrio.VerticalPadding(5);
                didSkyChange4 |= ImGui.InputUInt("###CloudSideTexture"u8, ref env->EnvState.Clouds.CloudSideTexture);
                ImBrio.AttachToolTip("云层侧面纹理 ID");

                ImBrio.SeparatorText("云层颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.ColorEdit3("###leftCloudColor", ref env->EnvState.Clouds.CloudColor1);
                ImBrio.AttachToolTip("云层颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.ColorEdit3("###rightcloudColor", ref env->EnvState.Clouds.CloudColor2);
                ImBrio.AttachToolTip("云层侧面颜色");

                ImBrio.VerticalPadding(5);
                ImBrio.SeparatorText("云层其他");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.SliderFloat("###gradientStop", ref env->EnvState.Clouds.ShadowStop, 0.0f, 2.0f);
                ImBrio.AttachToolTip("阴影截止");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.SliderFloat("###cloudHeight", ref env->EnvState.Clouds.CloudHeight, 0.0f, 2.0f);
                ImBrio.AttachToolTip("云层高度");

                ImBrio.VerticalPadding(3);

                if(didSkyChange4)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Clouds;

                break;
            case 3:

                ImBrio.VerticalPadding(3);

                if(ImBrio.SeparatorTextButton("Interior Brightness", FontAwesomeIcon.Redo, "Reset All Sky Properties", Capability.IsInside))
                {
                    Capability.ResetIndoorLighting();
                }

                ImBrio.CenterNextElementWithPadding(10);

                float currentLight = Capability.IndoorLight;
                using(ImRaii.Disabled(Capability.IsInside == false))
                    if(ImGui.SliderFloat("###brightness", ref currentLight, 0.0f, 1.0f))
                    {
                        Capability.IndoorLight = currentLight;
                    }
                if(Capability.IsInside == false)
                    ImBrio.AttachToolTip("必须在房屋内才能调节室内亮度。");
                else
                    ImBrio.AttachToolTip("调节室内光照亮度。");

                ImBrio.VerticalPadding(3);

                break;
        }
    }
}
