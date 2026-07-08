using Brio.Capabilities.World;
using Brio.Game.World;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.World;

public class DebugEnvironmentWidget(DebugEnvironmentCapability capability) : Widget<DebugEnvironmentCapability>(capability)
{
    public override string HeaderName => "调试";

    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    override unsafe public void DrawBody()
    {
        if(ImBrio.ToggelButton("isParticleSystemEnabled", Capability.Environment.isParticleSystemEnabled))
            Capability.Environment.isParticleSystemEnabled = !Capability.Environment.isParticleSystemEnabled;

        if(ImBrio.ToggelButton("IsEnvironmentOverride", Capability.Environment.IsEnvironmentOverride))
            Capability.Environment.IsEnvironmentOverride = !Capability.Environment.IsEnvironmentOverride;

        if(ImBrio.ToggelButton("customParticlesEnabled", Capability.Environment.customParticlesEnabled))
            Capability.Environment.customParticlesEnabled = !Capability.Environment.customParticlesEnabled;

        var env = BrioEnvManager.Instance();
        if(env == null) return;

        ImBrio.VerticalPadding(5);

        ImGui.SliderFloat("###11"u8, ref env->EnvState.EnvironmentLighting.Unknown1, 0.0f, 100f);
        ImBrio.AttachToolTip("EnvironmentLighting.Unknown1");
        ImGui.SliderFloat("###21"u8, ref env->EnvState.EnvironmentLighting.Unknown2, 0.0f, 100f);
        ImBrio.AttachToolTip("EnvironmentLighting.Unknown2");
        ImGui.SliderFloat("###31"u8, ref env->EnvState.EnvironmentLighting.LightDistance, 0.0f, 100f);
        ImBrio.AttachToolTip("EnvironmentLighting.LightDistance");
        ImGui.SliderFloat("###41"u8, ref env->EnvState.EnvironmentLighting.Unknown4, 0.0f, 100f);
        ImBrio.AttachToolTip("EnvironmentLighting.Unknown4");

        ImBrio.VerticalPadding(3);

        ImGui.SliderFloat("###13"u8, ref env->EnvState.Rain.Unknown1, 0.0f, 100f);
        ImBrio.AttachToolTip("Rain.Unknown1");
        ImGui.SliderFloat("###23"u8, ref env->EnvState.Rain.Unknown2, 0.0f, 100f);
        ImBrio.AttachToolTip("Rain.Unknown2");
        ImGui.SliderFloat("###33"u8, ref env->EnvState.Rain.Unknown3, 0.0f, 100f);
        ImBrio.AttachToolTip("Rain.LightDistance");

        ImBrio.VerticalPadding(3);

        ImGui.SliderFloat("###14"u8, ref env->EnvState.Particles.Unknown1, 0.0f, 100f);
        ImBrio.AttachToolTip("Particles.Unknown1");

        ImBrio.VerticalPadding(5);

        ImGui.Text("环境管理器："u8);
        Capability.DynamisIPC.DrawPointer(&env);

        ImGui.Text("环境状态："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState);

        ImBrio.VerticalPadding(5);

        ImGui.Text("环境光照："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState.EnvironmentLighting);
        ImGui.Text("星星："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState.Stars);
        ImGui.Text("雾："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState.Fog);
        ImGui.Text("云层："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState.Clouds);
        ImGui.Text("雨："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState.Rain);
        ImGui.Text("粒子："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState.Particles);
        ImGui.Text("风："u8);
        Capability.DynamisIPC.DrawPointer(&env->EnvState.Wind);
    }
}
