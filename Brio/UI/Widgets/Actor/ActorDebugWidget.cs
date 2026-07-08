using Brio.Capabilities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.VFX.Intertop;
using Brio.IPC;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Actor;

public class ActorDebugWidget(ActorDebugCapability capability) : Widget<ActorDebugCapability>(capability)
{
    public override string HeaderName => "调试";

    public override WidgetFlags Flags => Capability.IsDebug ? WidgetFlags.DrawBody : WidgetFlags.None;

    // TODO: Store this properly in a list or whatever so it can be cleaned up
    private unsafe static VfxData* _spawnedGoopInstance;

    string path = "vfx/common/eff/c0101_stlp_mim_gre_c0r1.avfx";

    public unsafe override void DrawBody()
    {
        using var tabBar = ImRaii.TabBar("###debug_tabs");
        if(tabBar.Success)
        {
            using(var infoTab = ImRaii.TabItem("Info"))
            {
                if(infoTab.Success)
                {
                    if(DynamisService.Instance != null)
                    {
                        ImGui.Text("游戏对象 ");
                        ImGui.SameLine();
                        DynamisService.Instance.DrawPointer(Capability.GameObject.Address);
                    }
                    else
                    {
                        string addr = Capability.GameObject.Address.ToString("X");
                        ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Address").X);
                        ImGui.InputText("Address", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                    }

                    var charaBase = Capability.Character.GetCharacterBase();
                    if(charaBase != null)
                    {
                        if(DynamisService.Instance != null)
                        {
                            ImGui.Text("角色基础对象 ");
                            ImGui.SameLine();
                            DynamisService.Instance.DrawPointer((nint)charaBase);
                        }
                        else
                        {
                            var addr = ((nint)charaBase).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("DrawObject").X - 10);
                            ImGui.InputText("DrawObject", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                        }

                        var skele = charaBase->CharacterBase.Skeleton;
                        if(DynamisService.Instance != null)
                        {
                            ImGui.Text("骨骼 ");
                            ImGui.SameLine();
                            DynamisService.Instance.DrawPointer((nint)skele);
                        }
                        else
                        {
                            var addr = ((nint)skele).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("骨骼").X - 10);
                            ImGui.InputText("骨骼", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                        }

                        var shaders = Capability.Character.GetShaderParams();
                        if(DynamisService.Instance != null)
                        {
                            ImGui.Text("角色着色器 ");
                            ImGui.SameLine();
                            DynamisService.Instance.DrawPointer((nint)shaders);
                        }
                        else
                        {
                            var addr = ((nint)shaders).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Shaders").X - 10);
                            ImGui.InputText("Shaders", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                        }
                    }
                }
            }

            using(var infoTab = ImRaii.TabItem("骨骼"))
            {
                if(infoTab.Success)
                {
                    if(ImGui.Button("刷新骨骼缓存"))
                    {
                        Capability.SkeletonService.RefreshSkeletonCache();
                    }

                    if(ImGui.CollapsingHeader("堆栈", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        var stacks = Capability.SkeletonStacks;
                        foreach(var stack in stacks)
                        {
                            ImGui.Text($"{stack.Key}: {stack.Value}");
                        }
                    }
                }
            }

            using(var vfxTab = ImRaii.TabItem("Goop Demo"))
            {
                if(vfxTab.Success)
                {

                    ImGui.InputText("Path", ref path);
                    if(ImGui.Button("创建角色特效"))
                    {
                        // TODO: Store this properly in a list or whatever so it can be cleaned up
                        _spawnedGoopInstance = Capability.VFXService.CreateActorVFX(path, Capability.GameObject);

                    }

                    if(DynamisService.Instance != null)
                    {
                        ImGui.Text("特效数据： ");
                        ImGui.SameLine();
                        DynamisService.Instance.DrawPointer((nint)_spawnedGoopInstance);
                    }

                    if(ImGui.Button("销毁角色特效"))
                    {
                        Capability.VFXService.DestroyVFX(_spawnedGoopInstance);
                        _spawnedGoopInstance = null;
                    }
                }
            }
        }
    }
}
