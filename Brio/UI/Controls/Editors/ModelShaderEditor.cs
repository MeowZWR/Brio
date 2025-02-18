using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Interop;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using ImGuiNET;

namespace Brio.UI.Controls.Editors;

public class ModelShaderEditor()
{
    private static float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXX").X;
    private static float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X * 2f;

    private ActorAppearanceCapability _capability = null!;

    public bool Draw(BrioHuman.ShaderParams original, ref ModelShaderOverride apply, ActorAppearanceCapability capability)
    {
        _capability = capability;

        bool didChange = false;

        didChange |= DrawReset(original, ref apply);
        didChange |= DrawMuscleTone(original, ref apply);
        didChange |= DrawBodyColors(original, ref apply);
        didChange |= DrawHairColors(original, ref apply);
        didChange |= DrawOtherColors(original, ref apply);

        return didChange;
    }

    private bool DrawReset(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        var resetTo = ImGui.GetCursorPos();
        bool shaderChange = apply.HasOverride;
        if(ImBrio.FontIconButtonRight("reset_shaders", FontAwesomeIcon.Undo, 1, "重置着色器", shaderChange))
        {
            apply.Reset();
            _ = _capability.Redraw();
        }
        ImGui.SetCursorPos(resetTo);

        return false;
    }

    private unsafe bool DrawMuscleTone(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        bool didChange = false;

        ImGui.SetNextItemWidth(MaxItemWidth);
        if(ImGui.SliderFloat("###muscle", ref original.MuscleTone, 0.0f, 2.0f, "%.2f"))
        {
            apply.MuscleTone = original.MuscleTone;
            didChange |= true;
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("肌肉");

        return didChange;
    }

    private bool DrawBodyColors(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        bool didChange = false;

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.SkinColor, "skinColor", "皮肤颜色"))
        {
            apply.SkinColor = original.SkinColor;
            didChange |= true;
        }
        ImGui.SameLine();
        if(AppearanceEditorCommon.DrawExtendedColor(ref original.SkinGloss, "skinGloss", "皮肤光泽"))
        {
            apply.SkinGloss = original.SkinGloss;
            didChange |= true;
        }
        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.MouthColor, "mouthColor", "嘴唇颜色"))
        {
            apply.MouthColor = original.MouthColor;
            didChange |= true;
        }
        ImGui.SameLine();

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("身体");

        return didChange;
    }

    private bool DrawOtherColors(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        bool didChange = false;

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.LeftEyeColor, "leftEyeColor", "左眼颜色"))
        {
            apply.LeftEyeColor = original.LeftEyeColor;
            didChange |= true;
        }
        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.RightEyeColor, "rightEyeColor", "右眼颜色"))
        {
            apply.RightEyeColor = original.RightEyeColor;
            didChange |= true;
        }

        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.FeatureColor, "featureColor", "特征颜色"))
        {
            apply.FeatureColor = original.FeatureColor;
            didChange |= true;
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("其他");

        return didChange;
    }

    private bool DrawHairColors(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {

        bool didChange = false;

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.HairColor, "hairColor", "头发颜色"))
        {
            apply.HairColor = original.HairColor;
            didChange |= true;
        }
        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.HairHighlight, "hairHighlight", "头发挑染"))
        {
            apply.HairHighlight = original.HairHighlight;
            didChange |= true;
        }
        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.HairGloss, "hairGloss", "头发光泽"))
        {
            apply.HairGloss = original.HairGloss;
            didChange |= true;
        }
        ImGui.SameLine();

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("头发");

        return didChange;
    }

}
