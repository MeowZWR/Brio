using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Entities;
using Brio.Files;
using Brio.Game.Actor.Extensions;
using Brio.Game.Cutscene;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Game.Penumbra;
using Brio.Resources;
using Brio.UI;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.IO;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;
using System.Collections.Generic;
using System.Linq;

namespace Brio.UI.Controls.Editors;

public class ActionTimelineEditor
{
    private static float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXX").X;
    private static float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + (ImGui.GetStyle().FramePadding.X * 2f);
    private static readonly ActionTimelineSelector _globalTimelineSelector = new("global_timeline_selector");
    private static bool _startAnimationOnSelect = true;
    private static bool _isBaseMode = false;

    private string _cameraPath = string.Empty;
    private ActionTimelineCapability _capability = null!;
    private bool _delimitSpeed = false;
    
    // Penumbra XCP 相关服务
    private readonly PenumbraXcpService _xcpService;
    private readonly PenumbraXcpUIManager _xcpUIManager;
    
    // 缓存上次的动作ID，避免重复更新
    private int _lastBaseAnimationId = -1;
    
    private readonly CutsceneManager _cutsceneManager;
    private readonly GPoseService _gPoseService;
    private readonly PhysicsService _physicsService;
    private readonly ConfigurationService _configService;
    private readonly EntityManager _entityManager;
    
    public ActionTimelineEditor(CutsceneManager cutsceneManager, GPoseService gPoseService, EntityManager entityManager, PhysicsService physicsService, ConfigurationService configService)
    {
        _cutsceneManager = cutsceneManager;
        _gPoseService = gPoseService;
        _physicsService = physicsService;
        _configService = configService;
        _entityManager = entityManager;
        
        // 初始化 Penumbra XCP 服务
        _xcpService = new PenumbraXcpService(cutsceneManager);
        _xcpUIManager = new PenumbraXcpUIManager(_xcpService, configService, cutsceneManager);
        
        // 订阅 XCP 文件变化事件来同步相机路径
        _xcpService.SelectedXcpFileChanged += OnSelectedXcpFileChanged;
    }

    private void OnSelectedXcpFileChanged(string selectedFile)
    {
        _cameraPath = selectedFile;
    }

    private void HandleSelectorChanges()
    {
        if(_globalTimelineSelector.SoftSelectionChanged && _globalTimelineSelector.SoftSelected != null)
        {
            if(_isBaseMode)
            {
                _capability.SlotedBaseAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
            }
            else
            {
                _capability.SlotedBlendAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
            }
        }

        if(_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
        {
            if(_isBaseMode)
            {
                _capability.SlotedBaseAnimation = _globalTimelineSelector.Selected.TimelineId;
                if(_startAnimationOnSelect)
                    ApplyBaseOverride(_capability, true);
            }
            else
            {
                _capability.SlotedBlendAnimation = _globalTimelineSelector.Selected.TimelineId;
                ApplyBlend(_capability);
            }

            // Close popup if not pinned
            if(!_globalTimelineSelector.IsPinned)
                ImGui.CloseCurrentPopup();
        }
    }

    public void Draw(bool drawAdvanced, ActionTimelineCapability capability)
    {
        _capability = capability;
        
        // 只在动作ID发生变化时才更新情感动作
        if (_lastBaseAnimationId != capability.SlotedBaseAnimation)
        {
            _xcpService.UpdateCurrentEmoteFromCapability(capability);
            _lastBaseAnimationId = capability.SlotedBaseAnimation;
        }

        _globalTimelineSelector.DrawAsWindow();

        HandleSelectorChanges();

        DrawHeder();

        ImGui.Separator();
        ImBrio.VerticalPadding(2);

        DrawBaseOverride();
        ImBrio.VerticalPadding(2);

        DrawBlend();
        ImBrio.VerticalPadding(2);

        DrawOverallSpeed(drawAdvanced);

        if(drawAdvanced == false)
        {
            ImGui.Separator();
            ImBrio.VerticalPadding(2);

            DrawFirstScrub();
        }

        if(drawAdvanced)
        {
            ImBrio.VerticalPadding(2);
            DrawLips();

            ImBrio.VerticalPadding(4);

            if(ImGui.CollapsingHeader("进度"))
            {
                ImBrio.VerticalPadding(2);
                DrawScrub();
                ImBrio.VerticalPadding(2);
            }

            if(ImGui.CollapsingHeader("栏位"))
            {
                ImBrio.VerticalPadding(2);
                DrawSlots();
                ImBrio.VerticalPadding(2);
            }

            if(ImGui.CollapsingHeader("场景控制（XAT整合）"))
            {
                ImBrio.VerticalPadding(2);
                DrawCutscene();
                ImBrio.VerticalPadding(2);
            }
        }
    }

    private void DrawHeder()
    {
        if(ImBrio.ToggelButton("冻结物理", new Vector2(95, 25), _physicsService.IsFreezeEnabled, hoverText: _physicsService.IsFreezeEnabled ? "解冻物理" : "冻结物理"))
        {
            _physicsService.FreezeToggle();
        }

        ImGui.SameLine();
        ImBrio.HorizontalPadding(2);

        ImBrio.RightAlign(100 * ImGuiHelpers.GlobalScale, 1);

        if(ImGui.Button("角色  ▼", new Vector2(70, 25) * ImGuiHelpers.GlobalScale))
        {
            ImGui.OpenPopup("animation_control");
        }

        ImGui.SameLine();
        ImBrio.HorizontalPadding(2);

        if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "重置动画", _capability.HasOverride))
        {
            _capability.Reset();
            _cutsceneManager.StopPlayback();
            _cutsceneManager.CameraPath = null;
            _cameraPath = string.Empty;
        }

        using var popup = ImRaii.Popup("animation_control");
        if(popup.Success)
        {
            ImBrio.VerticalPadding(1);

            if(ImBrio.Button("冻结所有角色", FontAwesomeIcon.Snowflake, new Vector2(180, 0)))
            {
                foreach(var actor in _entityManager.TryGetAllActors())
                {
                    if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
                    {
                        if(atCap is null)
                            return;

                        if(atCap.SpeedMultiplier > 0f)
                        {
                            atCap.SetOverallSpeedOverride(0f);
                        }
                    }
                }
            }

            ImBrio.VerticalPadding(1);

            if(ImBrio.Button("解冻所有角色", FontAwesomeIcon.Fire, new Vector2(180, 0)))
            {
                foreach(var actor in _entityManager.TryGetAllActors())
                {
                    if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
                    {
                        if(atCap is null)
                            return;

                        if(atCap.HasSpeedMultiplierOverride)
                        {
                            atCap.ResetOverallSpeedOverride();
                        }
                    }
                }
            }

            ImBrio.VerticalPadding(1);

            if(ImBrio.Button("播放所有动画", FontAwesomeIcon.PlayCircle, new Vector2(180, 0)))
            {
                foreach(var actor in _entityManager.TryGetAllActors())
                {
                    if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
                    {
                        if(atCap is null)
                            return;

                        ApplyBaseOverride(atCap, true);
                    }
                }
            }

            ImBrio.VerticalPadding(1);

            if(ImBrio.Button("停止所有动画", FontAwesomeIcon.StopCircle, new Vector2(180, 0)))
            {
                foreach(var actor in _entityManager.TryGetAllActors())
                {
                    if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
                    {
                        if(atCap is null)
                            return;

                        atCap.Stop();
                    }
                }
            }
        }
    }

    private void DrawBaseOverride()
    {
        const string baseLabel = "基础";
        ImGui.SetNextItemWidth(MaxItemWidth - ImGui.CalcTextSize("XXXX").X);
        ImGui.InputInt($"###base_animation", ref _capability.SlotedBaseAnimation, 0, 0);
        if(ImBrio.IsItemConfirmed())
        {
            ApplyBaseOverride(_capability, true);
        }

        ImGui.SameLine();
        ImGui.Checkbox("###base_interrupt", ref _capability.DoBaseInterrupt);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("中断");

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(baseLabel);

        ImGui.SameLine();
        ImBrio.HorizontalPadding(4);

        if(ImBrio.FontIconButtonRight("base_play", FontAwesomeIcon.PlayCircle, 3, "播放", _capability.SlotedBaseAnimation != 0))
        {
            if(_cutsceneManager.IsRunning)
            {
                _cutsceneManager.StopPlayback();
            }
            ApplyBaseOverride(_capability);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_reset", FontAwesomeIcon.StopCircle, 2, "停止", _capability.HasBaseOverride))
        {
            _capability.ResetBaseOverride();
            _capability.ResetOverallSpeedOverride();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_search", FontAwesomeIcon.Search, 1, "搜索"))
        {
            _isBaseMode = true;
            _globalTimelineSelector.Select(null, false);
            _globalTimelineSelector.AllowBlending = false;
            ImGui.OpenPopup("base_search_popup");
        }

        using(var popup = ImRaii.Popup("base_search_popup"))
        {
            if(popup.Success)
            {
                ImGui.Checkbox("选择后开始动画", ref _startAnimationOnSelect);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("选择时启动动画");

                        _globalTimelineSelector.Draw();
        

                _globalTimelineSelector.Draw();
            }
        }
    }

    private void DrawBlend()
    {
        const string blendLabel = "混合";

        ImGui.SetNextItemWidth(MaxItemWidth);
        ImGui.InputInt($"###blend_animation", ref _capability.SlotedBlendAnimation, 0, 0);
        if(ImBrio.IsItemConfirmed())
        {
            ApplyBlend(_capability);
        }

        ImGui.SameLine();

        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(blendLabel);

        ImGui.SameLine();
        ImBrio.HorizontalPadding(4);

        if(ImBrio.FontIconButtonRight("blend_play", FontAwesomeIcon.PlayCircle, 2, "播放", _capability.SlotedBlendAnimation != 0))
            ApplyBlend(_capability);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("blend_search", FontAwesomeIcon.Search, 1, "搜索"))
        {
            _isBaseMode = false;
            _globalTimelineSelector.Select(null, false);
            _globalTimelineSelector.AllowBlending = true;
            ImGui.OpenPopup("blend_search_popup");
        }

        using(var popup = ImRaii.Popup("blend_search_popup"))
        {
            if(popup.Success)
            {
                _globalTimelineSelector.Draw();
            }
        }
    }

    private void DrawLips()
    {
        var lipsOverride = _capability.LipsOverride;

        string preview = "无";
        if(lipsOverride != 0)
            preview = GameDataProvider.Instance.ActionTimelines[lipsOverride].Key.ToString();

        ImGui.SetNextItemWidth(MaxItemWidth);
        using(var combo = ImRaii.Combo("###lips", preview))
        {
            if(combo.Success)
            {
                if(ImGui.Selectable($"无", lipsOverride == 0))
                {
                    _capability.LipsOverride = 0;
                }

                for(uint i = 0x272; i <= 0x272 + 8; ++i)
                {
                    var entry = GameDataProvider.Instance.ActionTimelines[i];
                    bool selected = lipsOverride == i;
                    if(ImGui.Selectable($"{entry.Key} ({i})", selected))
                    {
                        _capability.LipsOverride = (ushort)i;
                    }
                }
            }
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("嘴唇（口型）");
    }

    private unsafe void DrawScrub()
    {
        float width = -ImGui.CalcTextSize("XXXX").X;

        var drawObj = _capability.Character.Native()->GameObject.DrawObject;
        if(drawObj == null)
            return;

        if(drawObj->Object.GetObjectType() != ObjectType.CharacterBase)
            return;

        var charaBase = (CharacterBase*)drawObj;

        if(charaBase->Skeleton == null)
            return;

        var skeleton = charaBase->Skeleton;

        for(int p = 0; p < skeleton->PartialSkeletonCount; ++p)
        {
            var partial = &skeleton->PartialSkeletons[p];
            var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
            if(animatedSkele == null)
                continue;

            for(int c = 0; c < animatedSkele->AnimationControls.Length; ++c)
            {
                var control = animatedSkele->AnimationControls[c].Value;
                if(control == null)
                    continue;

                var binding = control->hkaAnimationControl.Binding;
                if(binding.ptr == null)
                    continue;

                var anim = binding.ptr->Animation.ptr;
                if(anim == null)
                    continue;

                var duration = anim->Duration;
                var time = control->hkaAnimationControl.LocalTime;
                ImGui.SetNextItemWidth(width);
                if(ImGui.SliderFloat($"###scrub_{p}_{c}", ref time, 0f, duration, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                {
                    control->hkaAnimationControl.LocalTime = time;
                }
                if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    _capability.SetOverallSpeedOverride(0f);
                }
                ImGui.SameLine();
                ImGui.Text($"{p}.{c}");
            }
        }
    }

    private unsafe void DrawFirstScrub()
    {
        var drawObj = _capability.Character.Native()->GameObject.DrawObject;
        if(drawObj == null)
            return;

        if(drawObj->Object.GetObjectType() != ObjectType.CharacterBase)
            return;

        var charaBase = (CharacterBase*)drawObj;

        if(charaBase->Skeleton == null)
            return;

        var skeleton = charaBase->Skeleton;

        if(!(skeleton->PartialSkeletonCount > 0))
            return;

        var partial = &skeleton->PartialSkeletons[0];
        var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
        if(animatedSkele == null)
            return;

        if(!(animatedSkele->AnimationControls.Length > 0))
            return;

        var control = animatedSkele->AnimationControls[0].Value;
        if(control == null)
            return;

        var binding = control->hkaAnimationControl.Binding;
        if(binding.ptr == null)
            return;

        var anim = binding.ptr->Animation.ptr;
        if(anim == null)
            return;

        var duration = anim->Duration;
        var time = control->hkaAnimationControl.LocalTime;

        ImGui.SetNextItemWidth(-ImGui.CalcTextSize("ScrubX").X);
        if(ImGui.SliderFloat($"###scrub_001", ref time, 0f, duration, "%.2f", ImGuiSliderFlags.AlwaysClamp))
        {
            control->hkaAnimationControl.LocalTime = time;
        }
        if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            _capability.SetOverallSpeedOverride(0f);
        }
        ImGui.SameLine();
        ImGui.Text("进度条");
    }

private void DrawSlots()
    {
        var slots = Enum.GetValues<ActionTimelineSlots>();

        foreach(var slot in slots)
        {
            using(ImRaii.PushId((int)slot))
            {
                DrawSlot(slot);
                ImBrio.VerticalPadding(2);
                ImGui.Separator();
            }
        }
    }

    private void DrawSlot(ActionTimelineSlots slot)
    {
        var actionInfo = _capability.GetSlotAction(slot).Match(
                   action => $"{action.RowId} ({action.Key})",
                   none => "无"
               );

        var slotDescription = $"{slot} ({(int)slot}): {actionInfo}";

        using(ImRaii.PushId($"slot_{slot}"))
        {
            ImGui.Text(slotDescription);

            ImBrio.VerticalPadding(2);

            float existingSpeed = _capability.GetSlotSpeed(slot);
            float newSpeed = existingSpeed;
            const string speedLabel = "栏位速度";
            ImGui.SetNextItemWidth(ImGui.CalcTextSize($"XXXXXXXXXXXXXXXXXi").X);
            if(ImGui.SliderFloat($"{speedLabel}", ref newSpeed, 0f, 5f))
                _capability.SetSlotSpeedOverride(slot, newSpeed);

            ImGui.SameLine();
            ImBrio.HorizontalPadding(4);

            if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "重置速度", _capability.HasSlotSpeedOverride(slot)))
                _capability.ResetSlotSpeedOverride(slot);

            ImGui.SameLine();

            var speed = _capability.GetSlotSpeed(slot);
            if(ImBrio.FontIconButtonRight("speed_pause", FontAwesomeIcon.PauseCircle, 2, "暂停", speed > 0f))
                _capability.SetSlotSpeedOverride(slot, 0.0f);
        }
    }

    private void DrawOverallSpeed(bool drawAdvanced)
    {
        float existingSpeed = _capability.SpeedMultiplier;
        float newSpeed = existingSpeed;

        const string speedLabel = "速度";
        ImGui.SetNextItemWidth(drawAdvanced ? MaxItemWidth - ImGui.CalcTextSize("XXXX").X : MaxItemWidth);
        if(ImGui.SliderFloat($"###speed_slider", ref newSpeed, _delimitSpeed ? -5f : 0f, _delimitSpeed ? 10f : 5f))
            _capability.SetOverallSpeedOverride(newSpeed);

        if(drawAdvanced)
        {
            ImGui.SameLine();
            if(ImGui.Checkbox("###delimit_speed", ref _delimitSpeed))
                if(_delimitSpeed == false)
                {
                    _capability.ResetOverallSpeedOverride();
                }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("速度限界");
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(speedLabel);

        ImGui.SameLine();
        ImBrio.HorizontalPadding(4);

        if(ImBrio.FontIconButtonRight("speed_reset", FontAwesomeIcon.Undo, 1, "重置速度", _capability.HasSpeedMultiplierOverride))
            _capability.ResetOverallSpeedOverride();

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("speed_pause", FontAwesomeIcon.PauseCircle, 2, "暂停", _capability.SpeedMultiplier != 0f))
        {
            _capability.SetOverallSpeedOverride(0f);
        }
    }

    private void DrawCutscene()
    {
        ImGui.Text("相机路径 ");

        ImGui.SameLine();

        ImGui.InputText(string.Empty, ref _cameraPath, 260, ImGuiInputTextFlags.ReadOnly);

        ImGui.SameLine();

        if(ImGui.Button("浏览"))
        {
            UIManager.Instance.FileDialogManager.OpenFileDialog("浏览XAT相机文件", "XAT Camera File {.xcp}",
                (success, path) =>
                {
                    if(success)
                    {
                        _cameraPath = path[0];

                        string? folderPath = Path.GetDirectoryName(_cameraPath);
                        if(folderPath is not null)
                        {
                            _configService.Configuration.LastXATPath = folderPath;
                            _configService.Save();

                            // 通过XcpService来处理浏览选择的文件
                            _xcpService.SelectXcpFileFromBrowse(_cameraPath);
                        }
                    }
                }, 1, _configService.Configuration.LastXATPath, false);
        }

        // Penumbra XCP文件下拉菜单
        _xcpUIManager.DrawPenumbraXcpControls(_cameraPath, (newPath) => _cameraPath = newPath);

        ImGui.Separator();
        ImBrio.VerticalPadding(2);

        using(ImRaii.Disabled(string.IsNullOrEmpty(_cameraPath)))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Checkbox("启用相机视场（FOV）", ref _cutsceneManager.CameraSettings.EnableFOV);
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("启用后可通过FOV参数调整相机视角");
            ImGui.SameLine();
            Dalamud.Interface.Components.ImGuiComponents.HelpMarker(
                "禁用FOV会使相机的精度降低。\n但可以提供更简单的方式来支持更多的角色尺寸。\n这样就不需要修改相机的缩放值了！"
            );

            ImBrio.VerticalPadding(4);
            ImGui.Separator();
            ImBrio.VerticalPadding(2);

            ImGui.InputFloat3("相机缩放", ref _cutsceneManager.CameraSettings.Scale);
            ImGui.InputFloat3("相机偏移", ref _cutsceneManager.CameraSettings.Offset);

            ImBrio.VerticalPadding(4);
            ImGui.Separator();
            ImBrio.VerticalPadding(2);

            ImGui.Checkbox("循环", ref _cutsceneManager.CameraSettings.Loop);

            ImGui.Checkbox("播放时隐藏Brio（按下组合键[Shift+B]来停止播放场景）", ref _cutsceneManager.CloseWindowsOnPlay);

            ImBrio.VerticalPadding(4);
            ImGui.Separator();
            ImBrio.VerticalPadding(2);

            ImGui.Checkbox("###delay_Start", ref _cutsceneManager.DelayStart);
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("启动延迟（毫秒）");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxItemWidth);

            using(ImRaii.Disabled(_cutsceneManager.DelayStart == false))
            {
                ImGui.InputInt($"###delay_Start_Chek", ref _cutsceneManager.DelayTime, 0, 0);
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(LabelStart);
            ImGui.Text("启动延迟（毫秒）");

            ImBrio.VerticalPadding(4);
            ImGui.Separator();
            ImBrio.VerticalPadding(2);

            ImGui.Checkbox("在播放时启动所有角色的动画。", ref _cutsceneManager.StartAllActorAnimationsOnPlay);

            using(ImRaii.Disabled(_cutsceneManager.StartAllActorAnimationsOnPlay == false))
            {
                ImGui.Checkbox("###animation_delay_Start", ref _cutsceneManager.DelayAnimationStart);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("动画启动延迟");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(MaxItemWidth);

                using(ImRaii.Disabled(_cutsceneManager.DelayAnimationStart == false))
                {
                    ImGui.InputInt($"###animation_delay_Start_Chek", ref _cutsceneManager.DelayAnimationTime, 0, 0);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(LabelStart);
                ImGui.Text("动画延迟（毫秒）");
            }

            ImBrio.VerticalPadding(4);
            ImGui.Separator();
            ImBrio.VerticalPadding(2);

            ImGui.TextWrapped("延迟功能的时间刻度单位为毫秒！");
            ImGui.TextWrapped("1000毫秒 = 1秒");

            ImBrio.VerticalPadding(4);
            ImGui.Separator();
            ImBrio.VerticalPadding(2);

            var isrunning = _cutsceneManager.IsRunning;
            using(ImRaii.Disabled(isrunning))
            {
                if(ImBrio.Button("播放", FontAwesomeIcon.Play, new Vector2(-1, 30)))
                {
                    _cutsceneManager.StartPlayback();
                }
            }

            ImBrio.VerticalPadding(2);

            using(ImRaii.Disabled(!isrunning))
            {
                if(ImBrio.Button("停止", FontAwesomeIcon.Stop, new Vector2(-1, 30)))
                {
                    _cutsceneManager.StopPlayback();
                }
            }
        }
    }



    public static void ApplyBaseOverride(ActionTimelineCapability cap, bool resetSpeed = false)
    {
        if(cap.SlotedBaseAnimation == 0 || cap.IsPaused)
            return;

        if(resetSpeed || cap.SpeedMultiplier == 0)
            cap.ResetOverallSpeedOverride();

        cap.ApplyBaseOverride((ushort)cap.SlotedBaseAnimation, cap.DoBaseInterrupt);
    }
    public static void ApplyBlend(ActionTimelineCapability cap)
    {
        if(cap.SlotedBlendAnimation == 0 || cap.IsPaused)
            return;

        cap.BlendTimeline((ushort)cap.SlotedBlendAnimation);
    }
}
