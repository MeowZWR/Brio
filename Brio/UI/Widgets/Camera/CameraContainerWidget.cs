using Brio.Capabilities.Camera;
using Brio.Entities.Camera;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Widgets.Camera;

public class CameraContainerWidget(CameraContainerCapability capability) : Widget<CameraContainerCapability>(capability)
{
    public override string HeaderName => "相机";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    private CameraEntity? _selectedEntity;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            bool hasSelection = _selectedEntity != null;

            if(ImBrio.FontIconButton("CameraContainerWidget_New_Camera", FontAwesomeIcon.Plus, "新建相机"))
            {
                ImGui.OpenPopup("DrawSpawnMenuPopup");
            }
            CameraEditor.DrawSpawnMenu(Capability.VirtualCameraManager);

            ImGui.SameLine();

            using(ImRaii.Disabled(hasSelection == false))
            {
                using(ImRaii.Disabled(true))
                    if(ImBrio.FontIconButton("CameraLifetime_clone", FontAwesomeIcon.Clone, "克隆相机"))
                    {

                    }

                ImGui.SameLine();

                using(ImRaii.Disabled(_selectedEntity?.VirtualCamera.CameraID == 0))
                {
                    if(ImBrio.FontIconButton("CameraLifetime_destroy", FontAwesomeIcon.Trash, "销毁相机"))
                    {
                        Capability.VirtualCameraManager.DestroyCamera(_selectedEntity!.VirtualCamera.CameraID);
                    }
                }

                ImGui.SameLine();

                if(ImBrio.FontIconButton("CameraLifetime_target", FontAwesomeIcon.LocationCrosshairs, "选中相机"))
                {
                    Capability.VirtualCameraManager.SelectCamera(_selectedEntity!.VirtualCamera);
                }

                ImGui.SameLine();

                if(ImBrio.FontIconButton("containerwidget_selectinhierarchy", FontAwesomeIcon.FolderTree, "在层级中选择", hasSelection))
                {
                    Capability.VirtualCameraManager.SelectInHierarchy(_selectedEntity!);
                }
            }

            using(ImRaii.Disabled(Capability.VirtualCameraManager.CamerasCount == 0))
            {
                ImGui.SameLine();

                if(ImBrio.FontIconButton("containerwidget_destroyall", FontAwesomeIcon.Bomb, "销毁全部"))
                {
                    Capability.VirtualCameraManager.DestroyAll();
                }
            }
        }
    }

    public unsafe override void DrawBody()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImGui.BeginListBox($"###CameraContainerWidget_{Capability.Entity.Id}_list", new Vector2(-1, 150)))
            {
                foreach(var child in Capability.Entity.Children)
                {
                    if(child is CameraEntity cameraEntity)
                    {
                        bool isSelected = cameraEntity.Equals(_selectedEntity);
                        if(ImGui.Selectable($"{child.FriendlyName}###CameraContainerWidget_{Capability.Entity.Id}_item_{cameraEntity.Id}", isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                        {
                            _selectedEntity = cameraEntity;
                        }
                    }
                }

                ImGui.EndListBox();
            }
        }
    }
}

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

        }
        else
        {
            CameraEditor.DrawBrioCam("camera_widget_editor", Capability);
        }
    }

    public override void ToggleAdvancedWindow() => Capability.ShowCameraWindow();
}
