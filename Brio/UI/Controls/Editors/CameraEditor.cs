using Brio.Capabilities.Camera;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal static class CameraEditor
{
    struct CameraPresetProperties(Vector3 offset, float rotation, float zoom, float fov, Vector2 pan, Vector2 angle, bool disableCollision, bool delimitCamera)
    {
        public bool isSet = true;
        public Vector3 offset = offset;
        public float rotation = rotation;
        public float zoom = zoom;
        public float fov = fov;
        public Vector2 pan = pan;
        public Vector2 angle = angle;
        public bool disableCollision = disableCollision;
        public bool delimitCamera = delimitCamera;
    }
    private static readonly CameraPresetProperties[] presets = new CameraPresetProperties[3];

    public unsafe static void Draw(string id, CameraCapability capability)
    {
        var camera = capability.Camera;

        using(ImRaii.PushId(id))
        {
            using(ImRaii.Disabled(!capability.IsAllowed))
            {
                if(camera != null)
                {
                    var width = -ImGui.CalcTextSize("XXXXXXXXXx").X;

                    const string offsetText = "偏移";
                    Vector3 pos = capability.PositionOffset;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat3(offsetText, ref pos, 0.001f))
                        capability.PositionOffset = pos;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("reset", Dalamud.Interface.FontAwesomeIcon.Undo, 1f, "重置", capability.IsOveridden))
                        capability.Reset();

                    const string rotationText = "旋转";
                    float rotation = camera->Rotation;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderAngle(rotationText, ref rotation, -180, 180, "%.2f"))
                        camera->Rotation = rotation;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetRotation", Dalamud.Interface.FontAwesomeIcon.Undo, 1f, "重置", rotation != 0))
                        camera->Rotation = 0;

                    const string zoomText = "变焦";
                    float zoom = camera->Camera.Distance;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderFloat(zoomText, ref zoom, camera->Camera.MaxDistance, camera->Camera.MinDistance, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera->Camera.Distance = zoom;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetZoom", Dalamud.Interface.FontAwesomeIcon.Undo, 1f, "重置", zoom != 2.5))
                        camera->Camera.Distance = 2.5f;

                    const string fovText = "视场";
                    float fov = camera->FoV;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderAngle(fovText, ref fov, -44, 120, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera->FoV = fov;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetFoV", Dalamud.Interface.FontAwesomeIcon.Undo, 1f, "重置", fov != 0))
                        camera->FoV = 0f;

                    const string panText = "摇摄";
                    Vector2 pan = camera->Pan;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat2(panText, ref pan, 0.001f))
                        camera->Pan = pan;
            
                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetPan", Dalamud.Interface.FontAwesomeIcon.Undo, 1f, "重置", pan != Vector2.Zero))
                        camera->Pan = new Vector2(0, 0);

                    const string angleText = "角度";
                    Vector2 angle = camera->Angle;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat2(angleText, ref angle, 0.001f))
                        camera->Angle = angle;
        
                    var disable = capability.DisableCollision;
                    if(ImGui.Checkbox("禁用碰撞", ref disable))
                        capability.DisableCollision = disable;

                    ImGui.SameLine();

                    var delimit = capability.DelimitCamera;
                    if(ImGui.Checkbox("解限相机", ref delimit))
                        capability.DelimitCamera = delimit;

                    ImGui.Separator();

                    if(ImGui.CollapsingHeader("相机预设"))
                    {
                        for(int i = 0; i < 3; i++)
                        {
                            ImGui.Text($"Preset {i + 1} :");

                            ImGui.SameLine();

                            if(ImGui.Button($"Save##{i}"))
                                presets[i] = new CameraPresetProperties(capability.PositionOffset, camera->Rotation,
                                    camera->Camera.Distance, camera->FoV, camera->Pan, camera->Angle,
                                    capability.DisableCollision, capability.DelimitCamera);

                            if(presets[i].isSet)
                            {
                                ImGui.SameLine();
                                if(ImGui.Button($"Load##{i}"))
                                {
                                    capability.PositionOffset = presets[i].offset;
                                    camera->Rotation = presets[i].rotation;
                                    camera->Camera.Distance = presets[i].zoom;
                                    camera->FoV = presets[i].fov;
                                    camera->Pan = presets[i].pan;
                                    camera->Angle = presets[i].angle;
                                    capability.DisableCollision = presets[i].disableCollision;
                                    capability.DelimitCamera = presets[i].delimitCamera;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
