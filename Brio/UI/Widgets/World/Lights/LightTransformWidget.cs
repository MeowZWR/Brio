using Brio.Capabilities.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.World.Lights;

public class LightTransformWidget(LightTransformCapability lightGizmoCapability) : Widget<LightTransformCapability>(lightGizmoCapability)
{
    public override string HeaderName => "灯光变换";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.CanHide | WidgetFlags.HasAdvanced;

    private readonly ITransformableEditor _transformableEditor = new();

    public override void ToggleAdvancedWindow()
    {
        Capability.LightWindowOpen = !Capability.LightWindowOpen;
    }

    public override void DrawBody()
    {
        LightEditor.DrawLightTransformHeader(Capability);

        ImBrio.SeparatorText("灯光变换");

        _transformableEditor.Draw($"light_transform_{Capability.Entity.Id}", Capability.Light, 0.1f);

        var lightRenderingCapability = Capability.Light.GetCapability<LightRenderingCapability>();

        ImBrio.SeparatorText("灯光属性");

        LightEditor.DrawLightProperties(lightRenderingCapability);

        if(ImGui.CollapsingHeader("高级设置"u8, ImGuiTreeNodeFlags.None))
        {
            LightEditor.DrawAdvancedShadows(lightRenderingCapability);
        }
    }
}
