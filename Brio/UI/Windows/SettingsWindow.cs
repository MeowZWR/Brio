using Brio.Config;
using Brio.Input;
using Brio.IPC;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.Web;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Windows;

public class SettingsWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly WebService _webService;
    private readonly BrioIPCService _brioIPCService;
    private readonly CustomizePlusService _customizePlusService;
    private readonly MareService _mareService;

    public SettingsWindow(
        ConfigurationService configurationService,
        PenumbraService penumbraService,
        GlamourerService glamourerService,
        WebService webService,
        CustomizePlusService customizePlusService,
        BrioIPCService brioIPCService,
        MareService mareService) : base($"{Brio.Name} 设置###brio_settings_window", ImGuiWindowFlags.NoResize)
    {
        Namespace = "brio_settings_namespace";

        _configurationService = configurationService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _webService = webService;
        _brioIPCService = brioIPCService;
        _mareService = mareService;
        _customizePlusService = customizePlusService;

        Size = new Vector2(450, 450);
    }

    private bool _isModal = false;
    private float? _libraryPadding = null;
    public void OpenAsLibraryTab()
    {
        _libraryPadding = 35;

        Flags = ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
        IsOpen = true;

        BringToFront();

        _isModal = true;
    }

    public override void OnClose()
    {
        Flags = ImGuiWindowFlags.NoResize;
        _isModal = false;

        _libraryPadding = null;
    }

    public override void Draw()
    {
        using(ImRaii.PushId("brio_settings"))
        {
            if(_isModal)
            {
                DrawLibrarySection();

                if(ImBrio.Button("关闭", FontAwesomeIcon.Times, new Vector2(100, 0)))
                {
                    IsOpen = false;
                }
            }
            else
            {
                using(var tab = ImRaii.TabBar("###brio_settings_tabs"))
                {
                    if(tab.Success)
                    {
                        DrawGeneralTab();
                        DrawIPCTab();
                        DrawPosingTab();
                        DrawLibraryTab();
                        DrawSceneTab();
                        DrawKeysTab();
                        DrawAdvancedTab();
                    }
                }
            }
        }
    }

    private void DrawGeneralTab()
    {
        using(var tab = ImRaii.TabItem("常规设置"))
        {
            if(tab.Success)
            {
                if(ImGui.CollapsingHeader("窗口", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawOpenBrioSetting();
                    DrawHideSettings();
                }

                if(ImGui.CollapsingHeader("资产库", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool useLibraryWhenImporting = _configurationService.Configuration.UseLibraryWhenImporting;
                    if(ImGui.Checkbox("导入文件时使用资产库", ref useLibraryWhenImporting))
                    {
                        _configurationService.Configuration.UseLibraryWhenImporting = useLibraryWhenImporting;
                        _configurationService.ApplyChange();
                    }

                    bool returnToLastLocation = _configurationService.Configuration.Library.ReturnLibraryToLastLocation;
                    if(ImGui.Checkbox("在我最后浏览的位置打开资产库", ref returnToLastLocation))
                    {
                        _configurationService.Configuration.Library.ReturnLibraryToLastLocation = returnToLastLocation;
                        _configurationService.ApplyChange();
                    }
                }

                DrawNPCAppearanceHack();

                if(ImGui.CollapsingHeader("显示", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawDisplaySettings();
                }
            }
        }
    }

    private void DrawOpenBrioSetting()
    {
        var selectedBrioOpenBehavior = _configurationService.Configuration.Interface.OpenBrioBehavior;
        const string label = "Brio开启时机 ";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X);
        using(var combo = ImRaii.Combo(label, selectedBrioOpenBehavior.ToString()))
        {
            if(combo.Success)
            {
                foreach(var openBrioBehavior in Enum.GetValues<OpenBrioBehavior>())
                {
                    if(ImGui.Selectable($"{openBrioBehavior}", openBrioBehavior == selectedBrioOpenBehavior))
                    {
                        _configurationService.Configuration.Interface.OpenBrioBehavior = openBrioBehavior;
                        _configurationService.ApplyChange();
                    }
                }
            }
        }
    }

    private void DrawHideSettings()
    {
        bool showInGPose = _configurationService.Configuration.Interface.ShowInGPose;
        if(ImGui.Checkbox("可在集体动作中显示", ref showInGPose))
        {
            _configurationService.Configuration.Interface.ShowInGPose = showInGPose;
            _configurationService.ApplyChange();
        }

        bool showInCutscene = _configurationService.Configuration.Interface.ShowInCutscene;
        if(ImGui.Checkbox("可在过场动画中显示", ref showInCutscene))
        {
            _configurationService.Configuration.Interface.ShowInCutscene = showInCutscene;
            _configurationService.ApplyChange();
        }

        bool showWhenUIHidden = _configurationService.Configuration.Interface.ShowWhenUIHidden;
        if(ImGui.Checkbox("可在界面隐藏时显示", ref showWhenUIHidden))
        {
            _configurationService.Configuration.Interface.ShowWhenUIHidden = showWhenUIHidden;
            _configurationService.ApplyChange();
        }
    }

    private void DrawDisplaySettings()
    {
        bool censorActorNames = _configurationService.Configuration.Interface.CensorActorNames;
        if(ImGui.Checkbox("隐匿角色姓名", ref censorActorNames))
        {
            _configurationService.Configuration.Interface.CensorActorNames = censorActorNames;
            _configurationService.ApplyChange();
        }

        bool enableBrioColor = _configurationService.Configuration.Appearance.EnableBrioColor;
        if(ImGui.Checkbox("启用 Brio 颜色", ref enableBrioColor))
        {
            _configurationService.Configuration.Appearance.EnableBrioColor = enableBrioColor;
            _configurationService.ApplyChange();
        }

        bool enableBrioScale = _configurationService.Configuration.Appearance.EnableBrioScale;
        if(ImGui.Checkbox("启用 Brio 文本缩放", ref enableBrioScale))
        {
            _configurationService.Configuration.Appearance.EnableBrioScale = enableBrioScale;
            _configurationService.ApplyChange();
        }
    }

    private void DrawSceneTab()
    {
        using(var tab = ImRaii.TabItem("自动保存"))
        {
            if(tab.Success)
            {
                DrawImportScene();
            }
        }
    }

    private void DrawIPCTab()
    {
        using(var tab = ImRaii.TabItem("IPC设置"))
        {
            if(tab.Success)
            {
                DrawBrioIPC();
                DrawThirdPartyIPC();
            }
        }
    }

    private void DrawThirdPartyIPC()
    {
        if(ImGui.CollapsingHeader("第三方", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableCustomizePlus = _configurationService.Configuration.IPC.AllowCustomizePlusIntegration;
            if(ImGui.Checkbox("允许 Customize+ 集成", ref enableCustomizePlus))
            {
                _configurationService.Configuration.IPC.AllowCustomizePlusIntegration = enableCustomizePlus;
                _configurationService.ApplyChange();
                _customizePlusService.CheckStatus(true);
            }

            var customizePlusStatus = _customizePlusService.CheckStatus();
            using(ImRaii.Disabled(!enableCustomizePlus))
            {
                ImGui.Text($"Customize+ Status: {customizePlusStatus}");
                ImGui.SameLine();
                if(ImBrio.FontIconButton("refresh_Customize", FontAwesomeIcon.Sync, "刷新 Customize+ 状态"))
                {
                    _customizePlusService.CheckStatus(true);
                }
            }
        }

        if(ImGui.CollapsingHeader("第三方 [基于 Penumbra]", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var penumbraStatus = _penumbraService.CheckStatus();
            var penumbraUnavailable = penumbraStatus is IPCStatus.None or IPCStatus.NotInstalled or IPCStatus.VersionMismatch or IPCStatus.Error;

            if(penumbraUnavailable)
            {
                using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
                    ImGui.Text("请安装 Penumbra");
            }

            using(ImRaii.Disabled(penumbraUnavailable))
            {
                bool enablePenumbra = _configurationService.Configuration.IPC.AllowPenumbraIntegration;
                if(ImGui.Checkbox("允许 Penumbra 集成", ref enablePenumbra))
                {
                    _configurationService.Configuration.IPC.AllowPenumbraIntegration = enablePenumbra;
                    _configurationService.ApplyChange();
                    _penumbraService.CheckStatus(true);
                }

                ImGui.Text($"Penumbra 状态：{penumbraStatus}");
                ImGui.SameLine();
                if(ImBrio.FontIconButton("refresh_penumbra", FontAwesomeIcon.Sync, "刷新 Penumbra 状态"))
                {
                    _penumbraService.CheckStatus(true);
                }

                using(ImRaii.Disabled(!enablePenumbra))
                {
                    bool enableGlamourer = _configurationService.Configuration.IPC.AllowGlamourerIntegration;
                    if(ImGui.Checkbox("允许 Glamourer 集成", ref enableGlamourer))
                    {
                        _configurationService.Configuration.IPC.AllowGlamourerIntegration = enableGlamourer;
                        _configurationService.ApplyChange();
                        _glamourerService.CheckStatus(true);
                    }

                    var glamourerStatus = _glamourerService.CheckStatus();
                    using(ImRaii.Disabled(!enableGlamourer))
                    {
                        ImGui.Text($"Glamourer Status: {glamourerStatus}");
                        ImGui.SameLine();
                        if(ImBrio.FontIconButton("refresh_glamourer", FontAwesomeIcon.Sync, "刷新 Glamourer 状态"))
                        {
                            _glamourerService.CheckStatus(true);
                        }
                    }

                    bool enableMare = _configurationService.Configuration.IPC.AllowMareIntegration;

                    if(ImGui.Checkbox("允许 Mare Synchronos 集成", ref enableMare))
                    {
                        _configurationService.Configuration.IPC.AllowMareIntegration = enableMare;
                        _configurationService.ApplyChange();
                        _mareService.CheckStatus(true);
                    }

                    var mareStatus = _mareService.CheckStatus();
                    using(ImRaii.Disabled(!enableMare))
                    {
                        ImGui.Text($"Mare Synchronos Status: {mareStatus}");
                        ImGui.SameLine();
                        if(ImBrio.FontIconButton("refresh_mare", FontAwesomeIcon.Sync, "刷新 Mare Synchronos 状态"))
                        {
                            _mareService.CheckStatus(true);
                        }
                    }

                    _glamourerService.Disabled = !enablePenumbra;
                    _mareService.Disabled = _glamourerService.Disabled;
                }
            }
        }
    }

    private void DrawImportScene()
    {
        if(ImGui.CollapsingHeader("常规", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var enabled = _configurationService.Configuration.AutoSave.AutoSaveSystemEnabled;
            if(ImGui.Checkbox("启用自动保存", ref enabled))
            {
                _configurationService.Configuration.AutoSave.AutoSaveSystemEnabled = enabled;
                _configurationService.ApplyChange();
            }

            using(ImRaii.Disabled(!enabled))
            {
                var saveInterval = _configurationService.Configuration.AutoSave.AutoSaveInterval;
                if(ImGui.SliderInt("自动保存间隔", ref saveInterval, 15, 500, "%d 秒"))
                {
                    _configurationService.Configuration.AutoSave.AutoSaveInterval = saveInterval;
                    _configurationService.ApplyChange();
                }

                var maxSaves = _configurationService.Configuration.AutoSave.MaxAutoSaves;
                if(ImGui.SliderInt("最大自动保存次数", ref maxSaves, 3, 30))
                {
                    _configurationService.Configuration.AutoSave.MaxAutoSaves = maxSaves;
                    _configurationService.ApplyChange();
                }

                //bool applyModelTransform = _configurationService.Configuration.Import.ApplyModelTransform;
                //if(ImGui.Checkbox("Apply Model Transform on Import", ref applyModelTransform))
                //{
                //    _configurationService.Configuration.Import.ApplyModelTransform = applyModelTransform;
                //    _configurationService.ApplyChange();
                //}

                //var positionTransformType = _configurationService.Configuration.Import.PositionTransformType;
                //ImGui.SetNextItemWidth(200);
                //using(var combo = ImRaii.Combo("Position", positionTransformType.ToString()))
                //{
                //    if(combo.Success)
                //    {
                //        foreach(var poseImportTransformType in Enum.GetValues<ScenePoseTransformType>())
                //        {
                //            if(ImGui.Selectable($"{poseImportTransformType}", poseImportTransformType == positionTransformType))
                //            {
                //                _configurationService.Configuration.Import.PositionTransformType = poseImportTransformType;
                //                _configurationService.ApplyChange();
                //            }
                //        }
                //    }
                //}

                //var rotationTransformType = _configurationService.Configuration.Import.RotationTransformType;
                //ImGui.SetNextItemWidth(200);
                //using(var combo = ImRaii.Combo("Rotation", rotationTransformType.ToString()))
                //{
                //    if(combo.Success)
                //    {
                //        foreach(var poseImportTransformType in Enum.GetValues<ScenePoseTransformType>())
                //        {
                //            if(ImGui.Selectable($"{poseImportTransformType}", poseImportTransformType == rotationTransformType))
                //            {
                //                _configurationService.Configuration.Import.RotationTransformType = poseImportTransformType;
                //                _configurationService.ApplyChange();
                //            }
                //        }
                //    }
                //}

                //var scaleTransformType = _configurationService.Configuration.Import.ScaleTransformType;
                //ImGui.SetNextItemWidth(200);
                //using(var combo = ImRaii.Combo("Scale", scaleTransformType.ToString()))
                //{
                //    if(combo.Success)
                //    {
                //        foreach(var poseImportTransformType in Enum.GetValues<ScenePoseTransformType>())
                //        {
                //            if(ImGui.Selectable($"{poseImportTransformType}", poseImportTransformType == scaleTransformType))
                //            {
                //                _configurationService.Configuration.Import.ScaleTransformType = poseImportTransformType;
                //                _configurationService.ApplyChange();
                //            }
                //        }
                //    }
                //}
            }
        }
    }

    private void DrawBrioIPC()
    {
        if(ImGui.CollapsingHeader("Brio", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableBrioIpc = _configurationService.Configuration.IPC.EnableBrioIPC;
            if(ImGui.Checkbox("启用 Brio IPC", ref enableBrioIpc))
            {
                _configurationService.Configuration.IPC.EnableBrioIPC = enableBrioIpc;
                _configurationService.ApplyChange();
            }
            ImGui.Text($"Brio IPC 状态：{(_brioIPCService.IsIPCEnabled ? "已激活" : "未激活")}");

            bool enableWebApi = _configurationService.Configuration.IPC.AllowWebAPI;
            if(ImGui.Checkbox("启用 Brio API", ref enableWebApi))
            {
                _configurationService.Configuration.IPC.AllowWebAPI = enableWebApi;
                _configurationService.ApplyChange();
            }

            ImGui.Text($"Brio API 状态：{(_webService.IsRunning ? "已激活" : "未激活")}");
        }
    }

    private void DrawNPCAppearanceHack()
    {
        if(ImGui.CollapsingHeader("外观设置", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var allowNPCHackBehavior = _configurationService.Configuration.Appearance.ApplyNPCHack;
            const string label = "允许NPC外观出现在玩家身上";
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X);
            using(var combo = ImRaii.Combo(label, allowNPCHackBehavior.ToString()))
            {
                if(combo.Success)
                {
                    foreach(var npcHack in Enum.GetValues<ApplyNPCHack>())
                    {
                        if(ImGui.Selectable($"{npcHack}", npcHack == allowNPCHackBehavior))
                        {
                            _configurationService.Configuration.Appearance.ApplyNPCHack = npcHack;
                            _configurationService.ApplyChange();
                        }
                    }
                }
            }

            bool enableTinting = _configurationService.Configuration.Appearance.EnableTinting;
            if(ImGui.Checkbox("启用着色", ref enableTinting))
            {
                _configurationService.Configuration.Appearance.EnableTinting = enableTinting;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingTab()
    {
        using(var tab = ImRaii.TabItem("姿势设置"))
        {
            if(tab.Success)
            {
                DrawPosingGeneralSection();
                DrawGPoseSection();
                DrawOverlaySection();
            }
        }
    }

    private void DrawGPoseSection()
    {
        if(ImGui.CollapsingHeader("集体动作", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableMouseHook = _configurationService.Configuration.Posing.DisableGPoseMouseSelect;
            if(ImGui.Checkbox("禁止集体动作鼠标选择", ref enableMouseHook))
            {
                _configurationService.Configuration.Posing.DisableGPoseMouseSelect = enableMouseHook;
                _configurationService.ApplyChange();
            }

            bool enableBrioTargetChange = _configurationService.Configuration.Posing.BrioTargetChangesWithGPose;
            if(ImGui.Checkbox("Brio目标随集体动作目标切换", ref enableBrioTargetChange))
            {
                _configurationService.Configuration.Posing.BrioTargetChangesWithGPose = enableBrioTargetChange;
                _configurationService.ApplyChange();
            }

            bool enableGPoseTargetChange = _configurationService.Configuration.Posing.GPoseTargetChangesWithBrio;
            if(ImGui.Checkbox("集体动作目标随Brio目标切换", ref enableGPoseTargetChange))
            {
                _configurationService.Configuration.Posing.GPoseTargetChangesWithBrio = enableGPoseTargetChange;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawOverlaySection()
    {
        if(ImGui.CollapsingHeader("叠加层", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool defaultsOn = _configurationService.Configuration.Posing.OverlayDefaultsOn;
            if(ImGui.Checkbox("叠加层默认开启", ref defaultsOn))
            {
                _configurationService.Configuration.Posing.OverlayDefaultsOn = defaultsOn;
                _configurationService.ApplyChange();
            }

            bool standout = _configurationService.Configuration.Posing.ModelTransformStandout;
            if(ImGui.Checkbox("使[模型变换]骨骼突出显示", ref standout))
            {
                _configurationService.Configuration.Posing.ModelTransformStandout = standout;
                _configurationService.ApplyChange();
            }

            if(standout == false)
                ImGui.BeginDisabled();

            Vector4 modelTransformCircleStandOut = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.ModelTransformCircleStandOutColor);
            if(ImGui.ColorEdit4("[模型变换]骨骼突出显示颜色", ref modelTransformCircleStandOut, ImGuiColorEditFlags.NoInputs))
            {
                _configurationService.Configuration.Posing.ModelTransformCircleStandOutColor = ImGui.ColorConvertFloat4ToU32(modelTransformCircleStandOut);
                _configurationService.ApplyChange();
            }

            if(standout == false)
                ImGui.EndDisabled();

            bool allowGizmoAxisFlip = _configurationService.Configuration.Posing.AllowGizmoAxisFlip;
            if(ImGui.Checkbox("允许变换器坐标轴翻转", ref allowGizmoAxisFlip))
            {
                _configurationService.Configuration.Posing.AllowGizmoAxisFlip = allowGizmoAxisFlip;
                _configurationService.ApplyChange();
            }

            bool hideGizmoWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen;
            if(ImGui.Checkbox("使用高级姿势编辑时隐藏变换器", ref hideGizmoWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen = hideGizmoWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool hideToolbarWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen;
            if(ImGui.Checkbox("使用高级姿势编辑时隐藏工具栏", ref hideToolbarWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen = hideToolbarWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool showSkeletonLines = _configurationService.Configuration.Posing.ShowSkeletonLines;
            if(ImGui.Checkbox("显示骨骼线条", ref showSkeletonLines))
            {
                _configurationService.Configuration.Posing.ShowSkeletonLines = showSkeletonLines;
                _configurationService.ApplyChange();
            }

            bool hideSkeletonWhenGizmoActive = _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive;
            if(ImGui.Checkbox("变换器激活时隐藏骨骼", ref hideSkeletonWhenGizmoActive))
            {
                _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive = hideSkeletonWhenGizmoActive;
                _configurationService.ApplyChange();
            }

            float lineThickness = _configurationService.Configuration.Posing.SkeletonLineThickness;
            if(ImGui.DragFloat("骨骼线条厚度", ref lineThickness, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.SkeletonLineThickness = lineThickness;
                _configurationService.ApplyChange();
            }

            float circleSize = _configurationService.Configuration.Posing.BoneCircleSize;
            if(ImGui.DragFloat("骨骼节点圆环尺寸", ref circleSize, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.BoneCircleSize = circleSize;
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleNormalColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleNormalColor);
            if(ImGui.ColorEdit4("骨骼节点标准颜色。", ref boneCircleNormalColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleNormalColor = ImGui.ColorConvertFloat4ToU32(boneCircleNormalColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleInactiveColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleInactiveColor);
            if(ImGui.ColorEdit4("骨骼节点未激活显示的颜色", ref boneCircleInactiveColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleInactiveColor = ImGui.ColorConvertFloat4ToU32(boneCircleInactiveColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleHoveredColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleHoveredColor);
            if(ImGui.ColorEdit4("骨骼节点鼠标悬停时的颜色", ref boneCircleHoveredColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleHoveredColor = ImGui.ColorConvertFloat4ToU32(boneCircleHoveredColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleSelectedColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleSelectedColor);
            if(ImGui.ColorEdit4("骨骼节点被选中时的颜色", ref boneCircleSelectedColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleSelectedColor = ImGui.ColorConvertFloat4ToU32(boneCircleSelectedColor);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineActive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineActiveColor);
            if(ImGui.ColorEdit4("已激活骨骼的颜色", ref skeletonLineActive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineActiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineActive);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineInactive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineInactiveColor);
            if(ImGui.ColorEdit4("未激活骨骼的颜色", ref skeletonLineInactive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineInactiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineInactive);
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingGeneralSection()
    {
        if(ImGui.CollapsingHeader("常规", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
            if(ImGui.DragInt("可撤消历史记录", ref undoStackSize, 1, 0, 100))
            {
                _configurationService.Configuration.Posing.UndoStackSize = undoStackSize;
                _configurationService.ApplyChange();
            }
        }
    }

    bool resetSettings = false;
    private void DrawAdvancedTab()
    {
        using(var tab = ImRaii.TabItem("高级"))
        {
            if(tab.Success)
            {
                DrawEnvironmentSection();

                if(ImGui.CollapsingHeader("Brio", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Checkbox("启用[重置为默认设置]按钮", ref resetSettings);

                    using(ImRaii.Disabled(!resetSettings))
                    {
                        if(ImGui.Button("重置为默认设置", new(170, 0)))
                        {
                            _configurationService.Reset();
                            resetSettings = false;
                        }
                    }
                }

            }
        }
    }

    private void DrawEnvironmentSection()
    {
        if(ImGui.CollapsingHeader("环境", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var resetTimeOnGPoseExit = _configurationService.Configuration.Environment.ResetTimeOnGPoseExit;
            if(ImGui.Checkbox("退出集体动作时重置时间", ref resetTimeOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetTimeOnGPoseExit = resetTimeOnGPoseExit;
                _configurationService.ApplyChange();
            }

            var resetWeatherOnGPoseExit = _configurationService.Configuration.Environment.ResetWeatherOnGPoseExit;
            if(ImGui.Checkbox("退出集体动作时重置天气", ref resetWeatherOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetWeatherOnGPoseExit = resetWeatherOnGPoseExit;
                _configurationService.ApplyChange();
            }

            var resetWaterOnGPoseExit = _configurationService.Configuration.Environment.ResetWaterOnGPoseExit;
            if(ImGui.Checkbox("退出集体动作时重置水体", ref resetWaterOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetWaterOnGPoseExit = resetWaterOnGPoseExit;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawLibraryTab()
    {
        using(var tab = ImRaii.TabItem("资产库"))
        {
            if(tab.Success)
            {
                DrawLibrarySection();
            }
        }
    }

    private void DrawLibrarySection()
    {
        LibrarySourcesEditor.Draw(null, _configurationService, _configurationService.Configuration.Library, _libraryPadding);
    }

    private void DrawKeysTab()
    {
        using(var tab = ImRaii.TabItem("按键绑定"))
        {
            if(!tab.Success)
                return;

            bool enableKeyHandlingOnKeyMod = _configurationService.Configuration.Input.EnableKeyHandlingOnKeyMod;
            if(ImGui.Checkbox("移动自由相机时，[SPACE], [Shift], [Ctrl] & [Alt]键将专用于相机控制", ref enableKeyHandlingOnKeyMod))
            {
                _configurationService.Configuration.Input.EnableKeyHandlingOnKeyMod = enableKeyHandlingOnKeyMod;
                _configurationService.ApplyChange();
            }

            ImGui.Separator();

            bool enableKeybinds = _configurationService.Configuration.Input.EnableKeybinds;
            if(ImGui.Checkbox("启用键盘快捷键", ref enableKeybinds))
            {
                _configurationService.Configuration.Input.EnableKeybinds = enableKeybinds;
                _configurationService.ApplyChange();
            }

            if(enableKeybinds == false)
            {
                ImGui.BeginDisabled();
            }

            bool showPrompts = _configurationService.Configuration.Input.ShowPromptsInGPose;
            if(ImGui.Checkbox("在集体动作中显示提示", ref showPrompts))
            {
                _configurationService.Configuration.Input.ShowPromptsInGPose = showPrompts;
                _configurationService.ApplyChange();
            }

            if(ImGui.CollapsingHeader("界面", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawKeyBind(KeyBindEvents.Interface_ToggleBrioWindow);
                DrawKeyBind(KeyBindEvents.Interface_IncrementSmallModifier);
                DrawKeyBind(KeyBindEvents.Interface_IncrementLargeModifier);
            }

            if(ImGui.CollapsingHeader("姿势", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawKeyBind(KeyBindEvents.Posing_DisableGizmo);
                DrawKeyBind(KeyBindEvents.Posing_DisableSkeleton);
                DrawKeyBind(KeyBindEvents.Posing_HideOverlay);
                DrawKeyBind(KeyBindEvents.Posing_ToggleOverlay);
                DrawKeyBind(KeyBindEvents.Posing_Undo);
                DrawKeyBind(KeyBindEvents.Posing_Redo);
                DrawKeyBind(KeyBindEvents.Posing_Translate);
                DrawKeyBind(KeyBindEvents.Posing_Rotate);
                DrawKeyBind(KeyBindEvents.Posing_Scale);
            }

            if(enableKeybinds == false)
            {
                ImGui.EndDisabled();
            }
        }
    }

    private void DrawKeyBind(KeyBindEvents evt)
    {
        string evtText = Localize.Get($"keys.{evt}") ?? evt.ToString();

        if(KeybindEditor.KeySelector(evtText, evt, _configurationService.Configuration.Input))
        {
            _configurationService.ApplyChange();
        }
    }
}
