using Brio.Capabilities.Debug;
using Brio.Game.World;
using Brio.IPC;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

namespace Brio.UI.Widgets.Debug;

public class DebugWidget(DebugCapability capability, IClientState _clientState, IObjectTable _objectTable) : Widget<DebugCapability>(capability)
{
    public override string HeaderName => "调试";

    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    public override void DrawBody()
    {
        using(var bar = ImRaii.TabBar("DebugTabBar"))
        {
            if(bar.Success)
            {
                using(var item = ImRaii.TabItem("集体动作"))
                {
                    if(item.Success)
                        DrawGPose();
                }

                using(var item = ImRaii.TabItem("地址"))
                {
                    if(item.Success)
                        DrawAddresses();
                }

                using(var item = ImRaii.TabItem("杂项"))
                {
                    if(item.Success)
                        DrawMisc();
                }

                using(var item = ImRaii.TabItem("物体"))
                {
                    if(item.Success)
                        DrawObjects();
                }

                using(var item = ImRaii.TabItem("世界"))
                {
                    if(item.Success)
                    {

                    }
                }
            }
        }
    }

    private unsafe void DrawObjects()
    {

    }

    private void DrawGPose()
    {
        bool fakeGPose = Capability.FakeGPose;
        if(ImGui.Checkbox("模拟集体动作", ref fakeGPose))
        {
            Capability.FakeGPose = fakeGPose;
        }

        if(ImGui.Button("进入集体动作"))
        {
            Capability.EnterGPose();
        }

        ImGui.SameLine();

        if(ImGui.Button("退出集体动作"))
        {
            Capability.ExitGPose();
        }

        ImGui.Text($"IsPosing: {Capability?.IsPosing}");
    }

    private unsafe void DrawAddresses()
    {
        DynamisService.Instance?.DrawPointer((nint)BrioEnvManager.Instance());

        foreach(var (desc, addr) in Capability.GetInterestingAddresses())
        {
            string addrStr = addr.ToString("X");

            ImGui.SetNextItemWidth(150);
            ImBrio.CenterNextElementWithPadding(10);
            ImGui.InputText(desc, ref addrStr, 16, ImGuiInputTextFlags.ReadOnly);

            DynamisService.Instance?.DrawPointer(addr);
        }
    }
    private void DrawMisc()
    {
        var io = ImGui.GetIO();

        ImGui.Text($"MapId - {_clientState.MapId}");
        ImGui.Text($"TerritoryType - {_clientState.TerritoryType}");
        ImGui.Text($"CurrentWorld - {_objectTable.LocalPlayer?.CurrentWorld.Value.Name}");
        ImGui.Text($"HomeWorld - {_objectTable.LocalPlayer?.HomeWorld.Value.Name}");

        ImGui.Text(io.Framerate.ToString("F2") + " FPS");
    }
}
