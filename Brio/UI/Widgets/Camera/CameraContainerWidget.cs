using Brio.Capabilities.Camera;
using Brio.Entities.Camera;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Camera;

public class CameraContainerWidget(CameraContainerCapability capability) : Widget<CameraContainerCapability>(capability)
{
    public override string HeaderName => "Cameras";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImGui.MenuItem("Open Camera Editor###containerwidgetpopup_OpenAdvance"))
            {
                Capability.OpenCameraWindow();
            }

            if(ImGui.BeginMenu("New...###containerwidgetpopup_new"))
            {
                ImGui.Separator();

                if(ImGui.MenuItem("Camera###containerwidgetpopup_newcamera"))
                {
                    Capability.VirtualCameraManager.CreateCamera(CameraType.Game);
                }
                if(ImGui.MenuItem("Free-Cam###containerwidgetpopup_newfreecamera"))
                {
                    Capability.VirtualCameraManager.CreateCamera(CameraType.Free);
                }

                ImGui.EndMenu();
            }

            if(ImGui.BeginMenu("Destroy All...###containerwidgetpopup_destroy"))
            {
                if(ImGui.BeginMenu("新建...###containerwidgetpopup_new"))
                {
                    if(ImGui.MenuItem("打开相机编辑器###containerwidgetpopup_OpenAdvance"))
                    {
                        Capability.VirtualCameraManager.DestroyAll();
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }
        }
    }
}
