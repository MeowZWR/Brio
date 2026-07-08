using Brio.Capabilities.World;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;

namespace Brio.UI.Widgets.World.Lights;

public class LightDebugWidget(LightDebugCapability capability) : Widget<LightDebugCapability>(capability)
{
    public override string HeaderName => "调试";

    public override WidgetFlags Flags => Capability.IsDebug ? WidgetFlags.DrawBody : WidgetFlags.None;

    public unsafe override void DrawBody()
    {
        ImGui.Text("集体动作状态");
        ImGui.SameLine();
        if(Capability.LightingService.CurrentGPoseState is not null)
            Capability.DynamisIPC.DrawPointer((nint)Capability.LightingService.CurrentGPoseState);
        else
            ImGui.TextColored(ImGuiColors.DalamudRed, "CurrentGPoseState is NUll!");

        ImGui.Text("Brio 灯光");
        ImGui.SameLine();
        Capability.DynamisIPC.DrawPointer(Capability.GameLight.Address);

        ImGui.Separator();

        ImGui.Text("渲染灯光");
        ImGui.SameLine();
        Capability.DynamisIPC.DrawPointer((nint)Capability.GameLight.GameLight->RenderLight);
    }
}
