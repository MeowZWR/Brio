using Brio.Capabilities.Posing;
using Brio.Game.Posing;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class BoneIKEditor
{
    public static void Draw(BonePoseInfo poseInfo, PosingCapability posing)
    {
        bool didChange = false;

        var ik = poseInfo.DefaultIK;

        // Set IK Button


        using(ImRaii.Disabled(posing?.SkeletonPosing.PoseInfo.HasIKStacks is false))
        using(ImRaii.PushFont(UiBuilder.IconFont))
            if(ImGui.Button($"{FontAwesomeIcon.BreadSlice.ToIconString()}###clear_ik", new Vector2(-1, 26)))
                posing?.SkeletonPosing.ResetIK();
        ImBrio.AttachToolTip($"固化 IK 更改{(!posing?.SkeletonPosing.PoseInfo.HasIKStacks ?? false ? "。\n\n启用 IK 并用 IK 做出更改后，使用此按钮将…\n所有 IK 更改固化（锁定）到姿态中。" : "")}");

        var center = ImGui.GetItemRectMin() + (ImGui.GetItemRectSize() / 2);
        var radius = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.9f);
        var thickness = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.1f);

        if(posing?.SkeletonPosing.PoseInfo.HasIKStacks is false)
        {
            thickness += 0.2f;
            var offset = (radius - thickness) / MathF.Sqrt(2.0f);
            var lineStart = center + new Vector2(-offset, -offset);
            var lineEnd = center + new Vector2(offset, offset);
            ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, 0x400000FF, thickness);
        }

        if(ImGui.Checkbox("启用", ref ik.Enabled))
        {
            didChange |= true;
        }

        using(ImRaii.Disabled(!ik.Enabled))
        {
            if(ImGui.Checkbox("强制约束", ref ik.EnforceConstraints))
            {
                didChange |= true;
            }

            string solverType = ik.SolverOptions.Match(_ => "CCD", _ => "双关节");
            using(var combo = ImRaii.Combo("解算器", solverType))
            {
                if(combo.Success)
                {
                    if(ImGui.Selectable("CCD"))
                    {
                        ik.SolverOptions = BoneIKInfo.CalculateDefault(poseInfo.Name, false).SolverOptions;
                        didChange |= true;
                    }

                    if(BoneIKInfo.CanUseJoint(poseInfo.Name))
                    {
                        if(ImGui.Selectable("双关节"))
                        {
                            ik.SolverOptions = BoneIKInfo.CalculateDefault(poseInfo.Name, true).SolverOptions;
                            didChange |= true;
                        }
                    }
                }
            }

            ik.SolverOptions.Switch(
                ccd =>
                {
                    if(ImGui.SliderInt("深度", ref ccd.Depth, 1, 20))
                    {
                        ik.SolverOptions = ccd;
                        didChange |= true;
                    }

                    if(ImGui.SliderInt("迭代", ref ccd.Iterations, 1, 20))
                    {
                        ik.SolverOptions = ccd;
                        didChange |= true;
                    }
                },
                twoJoint =>
                {
                }
             );
        }

        if(didChange)
        {
            poseInfo.DefaultIK = ik;
        }
    }
}
