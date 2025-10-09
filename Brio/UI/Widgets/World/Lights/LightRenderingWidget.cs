using Brio.Capabilities.World;
using Brio.Game.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Widgets.World.Lights;

public class LightRenderingWidget(LightRenderingCapability lightRenderingCapability) : Widget<LightRenderingCapability>(lightRenderingCapability)
{
    public override string HeaderName => "灯光属性";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawPopup | WidgetFlags.CanHide;

    public override void DrawPopup()
    {
        var togglenText = Capability.GameLight.IsVisible ? $"关闭 {Capability.Entity.FriendlyName}" : $"开启 {Capability.Entity.FriendlyName}";
        if(ImGui.MenuItem($"{togglenText}###togglelight"))
        {
            Capability.GameLight.ToggleLight();
        }
    }

    public unsafe override void DrawBody()
    {        
        LightEditor.DrawLightProperties(Capability);

        ImBrio.VerticalPadding(5);

        if(ImGui.CollapsingHeader("高级阴影设置"u8, ImGuiTreeNodeFlags.None))
        {
            LightEditor.DrawAdvancedShadows(Capability);
        }

        if(ImGui.CollapsingHeader("高级设置"u8, ImGuiTreeNodeFlags.None))
        {
            LightEditor.DrawAdvancedSettings(Capability);
        }
    }
}
