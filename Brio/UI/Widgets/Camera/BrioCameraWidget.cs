using Brio.Capabilities.Camera;
using Brio.Entities.Camera;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.Camera;

public class BrioCameraWidget(BrioCameraCapability capability) : Widget<BrioCameraCapability>(capability)
{
    public override string HeaderName => "相机编辑器";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.HasAdvanced;

    public unsafe override void DrawBody()
    {
        if(Capability.CameraEntity.CameraType == CameraType.Free)
        {
            CameraEditor.DrawFreeCam("camera_widget_editor", Capability);
        }
        else if(Capability.CameraEntity.CameraType == CameraType.Cutscene)
        {
            if(ImGui.Button("打开相机窗口"))
            {
                Capability.ShowCameraWindow();
            }
            ImBrio.TextCentered("打开相机窗口以编辑或播放场景", ImGui.GetWindowContentRegionMax().X);

        }
        else
        {
            CameraEditor.DrawBrioCam("camera_widget_editor", Capability);
        }
    }

    public override void ToggleAdvancedWindow() => Capability.ShowCameraWindow();
}
