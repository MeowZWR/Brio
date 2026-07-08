using Brio.Capabilities.World;
using Brio.Game.World.Interop;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Linq;

namespace Brio.UI.Widgets.World.Lights;

public class LightContainerWidget(LightContainerCapability capability) : Widget<LightContainerCapability>(capability)
{
    public override string HeaderName => "灯光";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImGui.BeginMenu("从世界添加...###containerwidgetpopup_add"))
            {
                if(ImGui.BeginMenu("世界灯光...###containerwidgetpopup_addWorldLight"))
                {
                    var worldLights = Capability.GetWorldLights().OrderBy(x => x.distance).ToList();
                    if(worldLights.Count == 0)
                    {
                        ImGui.TextDisabled("未找到世界中的灯光");
                    }
                    else
                    {
                        if(ImGui.MenuItem($"全部添加 ({worldLights.Count})###containerwidgetpopup_addAllWorldLights"))
                        {
                            Capability.AddAllWorldLights();
                        }
                        ImGui.Separator();
                        foreach(var (light, distance) in worldLights)
                        {
                            if(ImGui.MenuItem($"灯光：{distance:F1}y##worldlight_{light}"))
                            {
                                Capability.AddWorldLight(light);
                            }
                        }
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }

            if(ImGui.MenuItem("打开灯光窗口###containerwidgetpopup_openWindow"))
            {
                Capability.OpenLightWindow();
            }

            if(ImGui.BeginMenu("新建...###containerwidgetpopup_new"))
            {
                ImGui.Separator();

                if(ImGui.MenuItem("聚光灯###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.SpotLight);
                }
                if(ImGui.MenuItem("区域光###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.PointLight);
                }
                if(ImGui.MenuItem("平面光###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.FlatLight);
                }
                ImGui.EndMenu();
            }

            if(ImGui.BeginMenu("全部销毁...###containerwidgetpopup_destroy"))
            {
                if(ImGui.BeginMenu("灯光###containerwidgetpopup_destroyLights"))
                {
                    if(ImGui.MenuItem("确认销毁##containerwidgetpopup_destroyallLights"))
                    {
                        Capability.LightingService.DestroyAll();
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }
        }
    }
}
