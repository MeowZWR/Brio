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
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;
using System.Collections.Generic;
using System.Linq;

namespace Brio.UI.Controls.Editors;

public class ActionTimelineEditor(CutsceneManager cutsceneManager, GPoseService gPoseService, EntityManager entityManager, PhysicsService physicsService, ConfigurationService configService)
{
    private readonly CutsceneManager _cutsceneManager = cutsceneManager;
    private readonly GPoseService _gPoseService = gPoseService;
    private readonly PhysicsService _physicsService = physicsService;
    private readonly ConfigurationService _configService = configService;
    private readonly EntityManager _entityManager = entityManager;

    private static float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXX").X;
    private static float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + (ImGui.GetStyle().FramePadding.X * 2f);

    private static readonly ActionTimelineSelector _globalTimelineSelector = new("global_timeline_selector");

    private static bool _startAnimationOnSelect = true;

    private string _cameraPath = string.Empty;
    private ActionTimelineCapability _capability = null!;
    private bool _delimitSpeed = false;
    private string _selectedXcpFile = string.Empty;
    private string? _lastEmoteName = null;
    private readonly ModPriorityAdjustmentWindow _priorityWindow = new();

    public void Draw(bool drawAdvanced, ActionTimelineCapability capability)
    {
        _capability = capability;

        DrawHeder();

        ImGui.Separator();

        DrawBaseOverride();
        DrawBlend();
        DrawOverallSpeed(drawAdvanced);

        if(drawAdvanced == false)
        {
            ImGui.Separator();

            DrawFirstScrub();
        }

        if(drawAdvanced)
        {
            DrawLips();

            if(ImGui.CollapsingHeader("进度"))
            {
                DrawScrub();
            }

            if(ImGui.CollapsingHeader("栏位"))
            {
                DrawSlots();
            }

            if(ImGui.CollapsingHeader("场景控制（XAT整合）"))
            {
                DrawCutscene();
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

        ImBrio.RightAlign(97, 1);

        if(ImGui.Button("角色     ", new Vector2(70, 25)))
        {
            ImGui.OpenPopup("animation_control");
        }

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Button, 0))
        {
            var curPos = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(curPos.X - 30, curPos.Y + 2));

            ImGui.ArrowButton("###animation_control_drop", ImGuiDir.Down);
        }

        ImGui.SameLine();

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
            if(ImGui.Button("冻结所有角色", Vector2.Zero))
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

            if(ImGui.Button("取消冻结所有角色", Vector2.Zero))
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

            if(ImGui.Button("播放所有动画", Vector2.Zero))
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

            if(ImGui.Button("停止所有动画", Vector2.Zero))
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

        if(ImBrio.FontIconButtonRight("base_play", FontAwesomeIcon.PlayCircle, 3, "播放", _capability.SlotedBaseAnimation != 0))
            ApplyBaseOverride(_capability);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_reset", FontAwesomeIcon.StopCircle, 2, "停止", _capability.HasBaseOverride))
        {
            _capability.ResetBaseOverride();
            _capability.ResetOverallSpeedOverride();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_search", FontAwesomeIcon.Search, 1, "搜索"))
        {
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
        


                if(_globalTimelineSelector.SoftSelectionChanged && _globalTimelineSelector.SoftSelected != null)
                {
                    _capability.SlotedBaseAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
                }

                if(_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
                {
                    _capability.SlotedBaseAnimation = _globalTimelineSelector.Selected.TimelineId;

                    if(_startAnimationOnSelect)
                        ApplyBaseOverride(_capability, true);

                    ImGui.CloseCurrentPopup();
                }
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

        if(ImBrio.FontIconButtonRight("blend_play", FontAwesomeIcon.PlayCircle, 2, "播放", _capability.SlotedBlendAnimation != 0))
            ApplyBlend(_capability);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("blend_search", FontAwesomeIcon.Search, 1, "搜索"))
        {
            _globalTimelineSelector.Select(null, false);
            _globalTimelineSelector.AllowBlending = true;

            ImGui.OpenPopup("blend_search_popup");

        }

        using(var popup = ImRaii.Popup("blend_search_popup"))
        {
            if(popup.Success)
            {
                _globalTimelineSelector.Draw();

                if(_globalTimelineSelector.SoftSelectionChanged && _globalTimelineSelector.SoftSelected != null)
                {
                    _capability.SlotedBlendAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
                }

                if(_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
                {
                    _capability.SlotedBlendAnimation = _globalTimelineSelector.Selected.TimelineId;
                    ApplyBlend(_capability);
                    ImGui.CloseCurrentPopup();
                }
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

            float existingSpeed = _capability.GetSlotSpeed(slot);
            float newSpeed = existingSpeed;
            const string speedLabel = "栏位速度";
            ImGui.SetNextItemWidth(ImGui.CalcTextSize($"XXXXXXXXXXXXXXXXXi").X);
            if(ImGui.SliderFloat($"{speedLabel}", ref newSpeed, 0f, 5f))
                _capability.SetSlotSpeedOverride(slot, newSpeed);


            ImGui.SameLine();

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
                        _selectedXcpFile = string.Empty; // 清空Penumbra选择

                        string? folderPath = Path.GetDirectoryName(_cameraPath);
                        if(folderPath is not null)
                        {
                            _configService.Configuration.LastXATPath = folderPath;
                            _configService.Save();

                            _cutsceneManager.CameraPath = new XATCameraFile(new BinaryReader(File.OpenRead(_cameraPath)));
                        }
                    }
                }, 1, _configService.Configuration.LastXATPath, false);
        }

        // Penumbra XCP文件下拉菜单
        DrawPenumbraXcpDropdown();

        ImGui.Separator();

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

            ImGui.Separator();

            ImGui.InputFloat3("相机缩放", ref _cutsceneManager.CameraSettings.Scale);
            ImGui.InputFloat3("相机偏移", ref _cutsceneManager.CameraSettings.Offset);

            ImGui.Separator();

            ImGui.Checkbox("循环", ref _cutsceneManager.CameraSettings.Loop);

            ImGui.Checkbox("播放时隐藏Brio（按下组合键[Shift+B]来停止播放场景）", ref _cutsceneManager.CloseWindowsOnPlay);

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

            ImGui.Separator();
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

            ImGui.Separator();

            ImGui.Text("延迟功能的时间刻度单位为毫秒！");
            ImGui.Text("1000毫秒 = 1秒");

            ImGui.Separator();

            var isrunning = _cutsceneManager.IsRunning;
            using(ImRaii.Disabled(isrunning))
            {
                if(ImGui.Button("播放"))
                {
                    _cutsceneManager.StartPlayback();
                }
            }

            ImGui.SameLine();

            using(ImRaii.Disabled(!isrunning))
            {
                if(ImGui.Button("停止"))
                {
                    _cutsceneManager.StopPlayback();
                }
            }
        }
    }

    private void DrawPenumbraXcpDropdown()
    {
        if (PenumbraManager.Instance == null)
        {
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), "Penumbra未连接");
            return;
        }

        if (!PenumbraManager.Instance.IsPenumbraAvailable())
        {
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), "Penumbra连接失败");
            return;
        }

        // 获取当前选择的情感动作名称
        string currentEmoteName = GetCurrentEmoteName();
        if (_lastEmoteName != currentEmoteName)
        {
            _selectedXcpFile = string.Empty;
            _lastEmoteName = currentEmoteName;
        }
        if (string.IsNullOrEmpty(currentEmoteName))
        {
            DrawBreathingText("未选择情感动作");
            return;
        }

        var modInfo = PenumbraManager.Instance.GetEffectiveModInfoForEmote(currentEmoteName);
        string modName = modInfo?.ModName ?? "未知";
        string modDirectory = modInfo?.ModDirectory ?? string.Empty;
        string modPath = "未知";
        List<string> xcpFiles = new();
        if (modInfo != null && !string.IsNullOrEmpty(modDirectory))
        {
            try
            {
                var modRootDirectory = PenumbraManager.Instance.GetModRootDirectory();
                modPath = System.IO.Path.Combine(modRootDirectory, modDirectory);
                var xcpFolderPath = System.IO.Path.Combine(modPath, "XCP");
                if (System.IO.Directory.Exists(xcpFolderPath))
                {
                    xcpFiles.AddRange(System.IO.Directory.GetFiles(xcpFolderPath, "*.xcp"));
                }
            }
            catch { }
        }

        bool isSelectedXcpValid = false;
        if (!string.IsNullOrEmpty(_selectedXcpFile) && !string.IsNullOrEmpty(modDirectory))
        {
            try
            {
                var modRootDirectory = PenumbraManager.Instance.GetModRootDirectory();
                var currentModPath = System.IO.Path.Combine(modRootDirectory, modDirectory);
                var currentXcpFolder = System.IO.Path.Combine(currentModPath, "XCP");
                isSelectedXcpValid = _selectedXcpFile.StartsWith(currentXcpFolder);
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(_selectedXcpFile) && !isSelectedXcpValid)
        {
            _selectedXcpFile = string.Empty;
            if (!string.IsNullOrEmpty(_cameraPath) && _cameraPath == _selectedXcpFile)
            {
                _cameraPath = string.Empty;
                _cutsceneManager.CameraPath = null;
            }
        }

        if (!PenumbraManager.Instance.HasEverRefreshed)
        {
            DrawBreathingText("本次会话尚未获取Penumbra模组状态，请点击下方刷新按钮获取。");
        }
        else if (PenumbraManager.Instance.HasModChangesSinceLastRefresh)
        {
            DrawBreathingText("检测到Penumbra模组设置变动，相机文件不正确时请手动刷新。");
        }
        ImGui.AlignTextToFramePadding();
        DrawBreathingText("自动检测镜头文件:");
        ImGui.SameLine();
        // 下拉菜单
        var preview = xcpFiles.Count == 0 ? "未找到镜头文件" : (string.IsNullOrEmpty(_selectedXcpFile) ? "选择XCP文件..." : Path.GetFileName(_selectedXcpFile));
        float comboWidth = 180f;
        ImGui.SetNextItemWidth(comboWidth);
        bool comboOpen = ImGui.BeginCombo("###penumbra_xcp_combo", preview);
        if (comboOpen)
        {
            if (xcpFiles.Count == 0)
            {
                ImGui.Selectable("无可用XCP文件", true, ImGuiSelectableFlags.Disabled);
            }
            else
            {
                if (ImGui.Selectable("选择XCP文件...", string.IsNullOrEmpty(_selectedXcpFile)))
                {
                    _selectedXcpFile = string.Empty;
                    if (!string.IsNullOrEmpty(_cameraPath) && _cameraPath == _selectedXcpFile)
                    {
                        _cameraPath = string.Empty;
                        _cutsceneManager.CameraPath = null;
                    }
                }
                foreach (var xcpFile in xcpFiles)
                {
                    var fileName = Path.GetFileName(xcpFile);
                    bool isSelected = _selectedXcpFile == xcpFile;
                    if (ImGui.Selectable(fileName, isSelected))
                    {
                        _selectedXcpFile = xcpFile;
                        _cameraPath = xcpFile;
                        try
                        {
                            _cutsceneManager.CameraPath = new XATCameraFile(new BinaryReader(File.OpenRead(xcpFile)));
                            Brio.Log.Information($"已加载Penumbra XCP文件: {fileName}");
                            // 自动勾选全员动画
                            _cutsceneManager.StartAllActorAnimationsOnPlay = true;
                        }
                        catch (Exception ex)
                        {
                            Brio.Log.Error($"加载XCP文件失败: {ex.Message}");
                            _selectedXcpFile = string.Empty;
                            _cameraPath = string.Empty;
                            _cutsceneManager.CameraPath = null;
                        }
                    }
                }
            }
            ImGui.EndCombo();
        }

        // 如果当前选择的XCP文件无效，显示警告提示
        if (!string.IsNullOrEmpty(_selectedXcpFile) && !isSelectedXcpValid)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "⚠");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("当前选择的XCP文件不属于当前生效模组");
                ImGui.Text("请重新选择或调整模组优先级");
                ImGui.EndTooltip();
            }
        }

        // 按钮同一行右侧
        ImGui.SameLine();
        // 刷新按钮
        if (ImBrio.FontIconButton("refreshPenumbraModInfo", FontAwesomeIcon.Repeat, "手动刷新获取模组信息"))
        {
            PenumbraManager.Instance?.RefreshModInfo();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("手动刷新获取模组信息");

        // 提示按钮
        ImGui.SameLine();
        Dalamud.Interface.Components.ImGuiComponents.HelpMarker("提示");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text("当前动作模组："); ImGui.SameLine(); ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), modName);
            ImGui.Text("文件系统路径："); ImGui.SameLine(); ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), modPath);
            
            // 检查是否有相同优先级的模组
            if (modInfo != null)
            {
                var allMods = PenumbraManager.Instance?.GetAllMods() ?? new List<PenumbraModInfo>();
                var samePriorityMods = allMods
                    .Where(m => m.IsEnabled && 
                               m.Priority == modInfo.Priority && 
                               m.ModDirectory != modInfo.ModDirectory &&
                               m.EmoteNames.Contains(currentEmoteName))
                    .ToList();
                
                if (samePriorityMods.Count > 0)
                {
                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "⚠ 检测到修改同样动作的相同优先级模组：");
                    var displayMods = samePriorityMods.Take(3).ToList();
                    foreach (var sameMod in displayMods)
                    {
                        ImGui.Text($"  • {sameMod.ModName}");
                    }
                    if (samePriorityMods.Count > 3)
                    {
                        ImGui.Text($"  • ... 还有 {samePriorityMods.Count - 3} 个模组");
                    }
                    ImGui.Text("建议调整优先级以避免冲突");
                }
            }
            
            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), "* 若该模组包含对此动作的修改，但实际未启用相关选项，且其优先级最高，仍将被识别为当前动作的来源。");
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "* NightLife等大型模组包含大量动作修改，可能导致识别结果不准确，请尽量避免将其优先级设为最高。");
            if (xcpFiles.Count == 0)
            {
                ImGui.Text("未检测到"); ImGui.SameLine(); ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "XCP"); ImGui.SameLine(); ImGui.Text("文件夹。请创建"); ImGui.SameLine(); ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "XCP"); ImGui.SameLine(); ImGui.Text("文件夹，放入后缀名为"); ImGui.SameLine(); ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), ".xcp"); ImGui.SameLine(); ImGui.Text("的镜头文件。");
            }
            ImGui.EndTooltip();
        }

        // 优先级调整按钮
        ImGui.SameLine();
        var emoteNameForPriority = GetCurrentEmoteName();
        var modsForEmote = PenumbraManager.Instance?.GetModsForEmote(emoteNameForPriority);
        bool hasModsForEmote = modsForEmote?.Count > 0;
        
        ImGui.BeginDisabled(!hasModsForEmote);
        if (ImBrio.FontIconButton("adjustModPriority", FontAwesomeIcon.SortNumericUp, "调整模组优先级"))
        {
            if (hasModsForEmote)
            {
                var currentHighestMod = PenumbraManager.Instance?.GetModInfoForEmote(emoteNameForPriority);
                
                _priorityWindow.UpdateContent(emoteNameForPriority, modsForEmote, () => {
                    var newHighestMod = PenumbraManager.Instance?.GetModInfoForEmote(emoteNameForPriority);
                    bool highestModChanged = currentHighestMod?.ModDirectory != newHighestMod?.ModDirectory;
                    
                    if (highestModChanged)
                    {
                        _selectedXcpFile = string.Empty;
                        if (!string.IsNullOrEmpty(_cameraPath))
                        {
                            _cameraPath = string.Empty;
                            _cutsceneManager.CameraPath = null;
                        }
                    }
                });
                ImGui.OpenPopup("mod_priority_adjustment_popup");
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (!hasModsForEmote)
                ImGui.SetTooltip("当前情感动作没有相关的Penumbra模组");
            else
                ImGui.SetTooltip($"调整修改 '{emoteNameForPriority}' 的模组优先级");
        }
        ImGui.EndDisabled();

        using (var popup = ImRaii.Popup("mod_priority_adjustment_popup", ImGuiWindowFlags.NoCollapse))
        {
            if (popup.Success)
                _priorityWindow.DrawContent();
        }

        // 从剪贴板导入按钮
        ImGui.SameLine();
        bool penumbraRefreshed = PenumbraManager.Instance.HasEverRefreshed;
        ImGui.BeginDisabled(!penumbraRefreshed);
        if (ImBrio.FontIconButton("importXcp", FontAwesomeIcon.Clipboard, "从剪贴板导入.xcp文件"))
        {
            var importer = new PenumbraClipboardImporter(msg => Brio.Log.Information(msg));
            importer.ImportXcpFromClipboard(modPath);
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (!penumbraRefreshed)
                ImGui.SetTooltip("请先手动刷新获取模组信息");
            else
                ImGui.SetTooltip("从剪贴板导入.xcp文件");
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        var xcpFolder = System.IO.Path.Combine(modPath, "XCP");
        bool xcpExists = System.IO.Directory.Exists(xcpFolder);
        bool ctrlDown = ImGui.GetIO().KeyCtrl;
        var icon = xcpExists ? FontAwesomeIcon.FolderOpen : FontAwesomeIcon.Plus;
        var tooltip = xcpExists ? "在文件资源管理器中打开XCP文件夹" : "按住Ctrl点击创建XCP文件夹";
        bool enabled = (xcpExists || ctrlDown) && penumbraRefreshed;
        ImGui.BeginDisabled(!enabled);
        bool clicked = ImBrio.FontIconButton("openXcpFolder", icon, tooltip, true);
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (!penumbraRefreshed)
                ImGui.SetTooltip("请先手动刷新获取模组信息");
            else
                ImGui.SetTooltip(tooltip);
        }
        if (clicked && enabled)
        {
            try
            {
                if (!xcpExists && ctrlDown)
                    System.IO.Directory.CreateDirectory(xcpFolder);
                if (System.IO.Directory.Exists(xcpFolder))
                    System.Diagnostics.Process.Start("explorer.exe", xcpFolder);
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"打开文件夹失败: {ex.Message}");
            }
        }
    }

    private string GetCurrentEmoteName()
    {
        // 从当前选择的基础动画中获取情感动作名称
        if (_capability.SlotedBaseAnimation == 0)
            return string.Empty;

        try
        {
            // 通过动画ID查找对应的情感动作名称
            var timelineId = (uint)_capability.SlotedBaseAnimation;
            if (GameDataProvider.Instance.ActionTimelines.TryGetValue(timelineId, out var timeline))
            {
                // 查找对应的情感动作
                foreach (var emote in GameDataProvider.Instance.Emotes.Values)
                {
                    if (emote.ActionTimeline[0].RowId == _capability.SlotedBaseAnimation ||
                        emote.ActionTimeline[1].RowId == _capability.SlotedBaseAnimation ||
                        emote.ActionTimeline[2].RowId == _capability.SlotedBaseAnimation ||
                        emote.ActionTimeline[3].RowId == _capability.SlotedBaseAnimation ||
                        emote.ActionTimeline[4].RowId == _capability.SlotedBaseAnimation)
                    {
                        return emote.Name.ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Brio.Log.Error($"获取情感动作名称失败: {ex.Message}");
        }

        return string.Empty;
    }

    // 呼吸彩字
    private static void DrawBreathingText(string text)
    {
        float t = (float)(ImGui.GetTime() * 0.18f);
        float hueStart = 0.5f, hueEnd = 0.92f;
        float interp = 0.5f * (1 + MathF.Sin(t * MathF.PI * 2));
        float hue = hueStart + (hueEnd - hueStart) * interp;
        float sat = 0.38f + 0.12f * interp;
        float val = 0.92f + 0.08f * interp;
        float r, g, b;
        ImGui.ColorConvertHSVtoRGB(hue, sat, val, out r, out g, out b);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(r, g, b, 1.0f));
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    //

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
