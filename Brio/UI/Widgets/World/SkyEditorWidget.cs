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
    public override string HeaderName => "天空";
    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    int selected = 0;
    private readonly TextureSelector _skyTextureSelector = new("sky_texture_selector", TextureType.Sky);
    private readonly TextureSelector _cloudTextureSelector = new("cloud_texture_selector", TextureType.Cloud);
    private readonly TextureSelector _cloudSideTextureSelector = new("cloud_side_texture_selector", TextureType.CloudSide);

    public unsafe override void DrawBody()
    {
        var env = BrioEnvManager.Instance();
        if(env == null) return;

        Vector2 unlockPos;
        Vector2 preservedPOS;

        ImBrio.VerticalPadding(3);

        ImBrio.ButtonSelectorStrip("stars_filters_selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["天空", "星辰", "云层"]);

        switch(selected)
        {
            case 1:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("星辰数量与亮度："u8);

                var isStars = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Stars);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetStars", FontAwesomeIcon.Redo, 1, "重置星辰设置", bordered: false, enabled: isStars))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Stars;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didSkyChange2 = ImGui.SliderFloat("###starcount"u8, ref env->EnvState.Stars.StarCount, 0.0f, 20.0f);
                ImBrio.AttachToolTip("星辰数量");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###starcountIntensity"u8, ref env->EnvState.Stars.StarIntensity, 0.0f, 2.5f);
                ImBrio.AttachToolTip("星辰亮度");

                ImBrio.VerticalPadding(5);
                ImGui.Text("月亮颜色与月亮亮度："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.ColorEdit4("###moonColor"u8, ref env->EnvState.Stars.MoonColor);
                ImBrio.AttachToolTip("月亮颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###MoonBrightness"u8, ref env->EnvState.Stars.MoonBrightness, 0.0f, 1.0f);
                ImBrio.AttachToolTip("月亮亮度");

                ImBrio.VerticalPadding(5);
                ImGui.Text("星座："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###constellationCount"u8, ref env->EnvState.Stars.ConstellationCount, 0.0f, 10.0f);
                ImBrio.AttachToolTip("星座数量");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###constellationsIntensity"u8, ref env->EnvState.Stars.ConstellationIntensity, 0.0f, 2.5f);
                ImBrio.AttachToolTip("星座亮度");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange2 |= ImGui.SliderFloat("###galaxyIntensity"u8, ref env->EnvState.Stars.GalaxyIntensity, 0.0f, 10.0f);
                ImBrio.AttachToolTip("银河亮度/强度");

                ImBrio.VerticalPadding(3);

                if(didSkyChange2)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Stars;

                break;
            case 0:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("天空："u8);

                var isSky = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Sky);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetSky", FontAwesomeIcon.Redo, 1, "重置天空", bordered: false, enabled: isSky))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Sky;
                ImGui.SetCursorPos(preservedPOS);

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

                ImGui.Separator();
                ImBrio.VerticalPadding(5);
                ImGui.Text("环境光照："u8);

                ImBrio.VerticalPadding(2);
                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("色温与饱和度："u8);

                var isLighting = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.EnvironmentLighting);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetLight", FontAwesomeIcon.Redo, 1, "重置光照设置", bordered: false, enabled: isLighting))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.EnvironmentLighting;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didSkyChange3 = ImGui.SliderFloat("###temperatureColor"u8, ref env->EnvState.EnvironmentLighting.AmbientTemperature, -2.5f, 2.5f);
                ImBrio.AttachToolTip("环境色温");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange3 |= ImGui.SliderFloat("###saturationColor"u8, ref env->EnvState.EnvironmentLighting.AmbientSaturation, 0.0f, 5.0f);
                ImBrio.AttachToolTip("环境饱和度");

                ImBrio.VerticalPadding(5);
                ImGui.Text("环境颜色："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange3 |= ImGui.ColorEdit3("##ambientColor"u8, ref env->EnvState.EnvironmentLighting.AmbientColor);
                ImBrio.AttachToolTip("环境颜色");

                ImBrio.VerticalPadding(5);
                ImGui.Text("日光与月光颜色："u8);

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

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("云层："u8);

                var isClouds = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Clouds);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetClouds", FontAwesomeIcon.Redo, 1, "重置云层", bordered: false, enabled: isClouds))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Clouds;
                ImGui.SetCursorPos(preservedPOS);

                if(ImBrio.BorderedGameTex("##cloudTexturePreview", _cloudTextureSelector.GetTexturePath(env->EnvState.Clouds.CloudTexture)))
                {
                    _cloudTextureSelector.Select(new TextureId(env->EnvState.Clouds.CloudTexture));
                    ImGui.OpenPopup("cloud_texture_selector"u8);
                }
                ImBrio.AttachToolTip("点击选择云层纹理");

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
                ImBrio.AttachToolTip("点击更改云侧（云边缘或远景云）纹理");

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
                ImBrio.AttachToolTip("云侧纹理 ID");

                ImGui.Text("云层颜色："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.ColorEdit3("###leftCloudColor", ref env->EnvState.Clouds.CloudColor1);
                ImBrio.AttachToolTip("云层主体颜色");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.ColorEdit3("###rightcloudColor", ref env->EnvState.Clouds.CloudColor2);
                ImBrio.AttachToolTip("云侧/边缘颜色");

                ImBrio.VerticalPadding(5);
                ImGui.Text("其他云层属性："u8);

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.SliderFloat("###gradientStop", ref env->EnvState.Clouds.ShadowStop, 0.0f, 2.0f);
                ImBrio.AttachToolTip("阴影截止点");

                ImBrio.CenterNextElementWithPadding(15);
                didSkyChange4 |= ImGui.SliderFloat("###cloudHeight", ref env->EnvState.Clouds.CloudHeight, 0.0f, 2.0f);
                ImBrio.AttachToolTip("云层高度");

                ImBrio.VerticalPadding(3);

                if(didSkyChange4)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Clouds;

                break;
        }
    }
}
