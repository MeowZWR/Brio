﻿using Brio.Capabilities.Debug;
using Brio.UI.Widgets.Core;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using ImGuiNET;

namespace Brio.UI.Widgets.Debug;

public class DebugWidget(DebugCapability capability, IClientState _clientState) : Widget<DebugCapability>(capability)
{
    public override string HeaderName => "调试";

    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    public override void DrawBody()
    {
        using(var bar = ImRaii.TabBar("DebugTabBar"))
        {
            if(bar.Success)
            {
                using(var item = ImRaii.TabItem("GPose"))
                {
                    if(item.Success)
                        DrawGPose();
                }

                using(var item = ImRaii.TabItem("Addresses"))
                {
                    if(item.Success)
                        DrawAddresses();
                }

                using(var item = ImRaii.TabItem("Misc"))
                {
                    if(item.Success)
                        DrawMisc();
                }
            }
        }
    }

    private void DrawGPose()
    {
        bool fakeGPose = Capability.FakeGPose;
        if(ImGui.Checkbox("Fake GPose", ref fakeGPose))
        {
            Capability.FakeGPose = fakeGPose;
        }

        if(ImGui.Button("Enter GPose"))
        {
            Capability.EnterGPose();
        }

        ImGui.SameLine();

        if(ImGui.Button("Exit GPose"))
        {
            Capability.ExitGPose();
        }
    }

    private void DrawAddresses()
    {
        foreach(var (desc, addr) in Capability.GetInterestingAddresses())
        {
            string addrStr = addr.ToString("X");
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(addrStr).X);
            ImGui.InputText(desc, ref addrStr, 16, ImGuiInputTextFlags.ReadOnly);
        }
    }
    private void DrawMisc()
    {
        var io = ImGui.GetIO();

        ImGui.Text($"MapId - {_clientState.MapId}");
        ImGui.Text($"TerritoryType - {_clientState.TerritoryType}");
        ImGui.Text($"CurrentWorld - {_clientState.LocalPlayer?.CurrentWorld.Value.Name}");
        ImGui.Text($"HomeWorld - {_clientState.LocalPlayer?.HomeWorld.Value.Name}");

        ImGui.Text(io.Framerate.ToString("F2") + " FPS");
    }
}
