using Brio.Capabilities.World;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.World;

public class EnvLifetimeWidget(EnvironmentLifetimeCapability environmentLifetimeCapability) : Widget<EnvironmentLifetimeCapability>(environmentLifetimeCapability)
{
    public override string HeaderName => "生命周期";

    public override WidgetFlags Flags => WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {

    }
}
