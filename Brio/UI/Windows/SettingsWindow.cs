using Brio.Config;
using Brio.Core;
using Brio.Game.Posing;
using Brio.Input;
using Brio.IPC;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Brio.Web;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using static Brio.Game.Posing.BoneCategories;

namespace Brio.UI.Windows;

public class SettingsWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly WebService _webService;
    private readonly CustomizePlusService _customizePlusService;
    private readonly PosingService _posingService;

    public SettingsWindow(
        ConfigurationService configurationService,
        PenumbraService penumbraService,
        GlamourerService glamourerService,
        WebService webService,
        CustomizePlusService customizePlusService,
        PosingService posingService) : base($"{Brio.Name} 设置###brio_settings_window", ImGuiWindowFlags.NoResize)
    {
        Namespace = "brio_settings_namespace";

        _configurationService = configurationService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _webService = webService;
        _customizePlusService = customizePlusService;
        _posingService = posingService;

        this.AllowBackgroundBlur = false;

        Size = new Vector2(500, 550);
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

    public override void PreDraw()
    {
        ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X - Size!.Value.X) / 2, (ImGui.GetIO().DisplaySize.Y - Size!.Value.Y) / 2), ImGuiCond.Appearing);

        base.PreDraw();
    }

    public override void OnClose()
    {
        Flags = ImGuiWindowFlags.NoResize;
        _isModal = false;

        _libraryPadding = null;
    }

    int selected;
    public override void Draw()
    {
        ImBrio.BlurWindow();

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
                ImBrio.ButtonSelectorStrip("settings_filters_selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["通常", "摆姿", "资源库", "自动保存", "输入", "高级"]);

                using(var child = ImRaii.Child("###settingsPane"))
                {
                    if(child.Success)
                    {
                        switch(selected)
                        {
                            case 0:
                                DrawGeneralTab();
                                break;
                            case 1:
                                DrawPosingTab();
                                break;
                            case 2:
                                DrawLibraryTab();
                                break;
                            case 3:
                                DrawSceneTab();
                                break;
                            case 4:
                                DrawKeysTab();
                                break;
                            case 5:
                                DrawAdvancedTab();
                                break;
                        }
                    }
                }
            }
        }
    }

    private void DrawGeneralTab()
    {
        ImBrio.SeparatorText("选项");

        DrawGeneralSettings();

        ImBrio.SeparatorText("IPC");

        DrawBrioIPC();
        DrawThirdPartyIPC();

        ImBrio.SeparatorText("其他");

        if(ImGui.CollapsingHeader("资源库", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool useLibraryWhenImporting = _configurationService.Configuration.UseLibraryWhenImporting;
            const string label1 = "导入文件时使用资产库";
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label1).X - 15);
            if(ImGui.Checkbox(label1, ref useLibraryWhenImporting))
            {
                _configurationService.Configuration.UseLibraryWhenImporting = useLibraryWhenImporting;
                _configurationService.ApplyChange();
            }

            bool returnToLastLocation = _configurationService.Configuration.Library.ReturnLibraryToLastLocation;
            const string label2 = "在我最后浏览的位置打开资产库";
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label2).X - 15);
            if(ImGui.Checkbox(label2, ref returnToLastLocation))
            {
                _configurationService.Configuration.Library.ReturnLibraryToLastLocation = returnToLastLocation;
                _configurationService.ApplyChange();
            }

            bool useFilenameAsActorName = _configurationService.Configuration.Library.UseFilenameAsActorName;
            const string label3 = "使用角色文件名作为参与者名称";
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label3).X - 15);
            if(ImGui.Checkbox(label3, ref useFilenameAsActorName))
            {
                _configurationService.Configuration.Library.UseFilenameAsActorName = useFilenameAsActorName;
                _configurationService.ApplyChange();
            }
        }

        if(ImGui.CollapsingHeader("XAT 场景"))
        {
            DrawOffsetSection();
        }
    }

    private void DrawOpenBrioSetting()
    {
        var selectedBrioOpenBehavior = _configurationService.Configuration.Interface.OpenBrioBehavior;
        const string label = "打开 Brio";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X - 20);
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

    private void DrawGeneralSettings()
    {
        bool censorActorNames = _configurationService.Configuration.Interface.CensorActorNames;
        if(ImGui.Checkbox("在 Brio 中隐藏角色名称", ref censorActorNames))
        {
            _configurationService.Configuration.Interface.CensorActorNames = censorActorNames;
            _configurationService.ApplyChange();
        }

        bool hideNames = _configurationService.Configuration.Posing.HideNameOnGPoseSettingsWindow;
        if(ImGui.Checkbox("在「集体动作设置」窗口中隐藏名称", ref hideNames))
        {
            _configurationService.Configuration.Posing.HideNameOnGPoseSettingsWindow = hideNames;
            _configurationService.ApplyChange();
        }

        ImBrio.SeparatorText("Brio 主题");

        var currentThemeName = _configurationService.Configuration.Appearance.Theme;
        const string themeLabel = "主题";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(themeLabel).X - 15);
        using(var combo = ImRaii.Combo(themeLabel, currentThemeName))
        {
            if(combo.Success)
            {
                foreach(var theme in ThemeManager.Themes)
                {
                    if(ImGui.Selectable(theme.Name, theme.Name == currentThemeName))
                    {
                        _configurationService.Configuration.Appearance.Theme = theme.Name;
                        ThemeManager.CurrentTheme = theme;
                        _configurationService.ApplyChange();
                    }
                }
            }
        }

        //float windowBgOpacity = _configurationService.Configuration.Appearance.WindowOpacity;
        //const string opacityLabel = "Window Opacity";
        //ImGui.SetNextItemWidth(-ImGui.CalcTextSize(opacityLabel).X - 15);
        //if(ImGui.SliderFloat(opacityLabel, ref windowBgOpacity, 0.0f, 1.0f))
        //{
        //    _configurationService.Configuration.Appearance.WindowOpacity = windowBgOpacity;
        //    _configurationService.ApplyChange();
        //}

        bool blur = _configurationService.Configuration.Appearance.EnableBlur;
        if(ImGui.Checkbox("启用背景模糊", ref blur))
        {
            _configurationService.Configuration.Appearance.EnableBlur = blur;
            _configurationService.ApplyChange();
        }

    }

    private void DrawSceneTab()
    {
        ImBrio.SeparatorText("选项");

        DrawAutoSaveSettings();
    }

    private void DrawThirdPartyIPC()
    {
        if(ImGui.CollapsingHeader("第三方 IPC"))
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
            }

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
                ImGui.Text($"Glamourer 状态：{glamourerStatus}");
                ImGui.SameLine();
                if(ImBrio.FontIconButton("refresh_glamourer", FontAwesomeIcon.Sync, "刷新 Glamourer 状态"))
                {
                    _glamourerService.CheckStatus(true);
                }
            }

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
                ImGui.Text($"Penumbra 状态：{penumbraStatus}");
                ImGui.SameLine();
                if(ImBrio.FontIconButton("refresh_Customize", FontAwesomeIcon.Sync, "刷新 Customize+ 状态"))
                {
                    _customizePlusService.CheckStatus(true);
                }
            }
        }
    }

    private void DrawAutoSaveSettings()
    {
        var enabled = _configurationService.Configuration.AutoSave.AutoSaveSystemEnabled;
        if(ImGui.Checkbox("启用自动保存", ref enabled))
        {
            _configurationService.Configuration.AutoSave.AutoSaveSystemEnabled = enabled;
            _configurationService.ApplyChange();
        }

        using(ImRaii.Disabled(!enabled))
        {
            var individual = _configurationService.Configuration.AutoSave.AutoSaveIndividualPoses;
            if(ImGui.Checkbox("保存单独姿势", ref individual))
            {
                _configurationService.Configuration.AutoSave.AutoSaveIndividualPoses = individual;
                _configurationService.ApplyChange();
            }

            var autoGPoseClear = _configurationService.Configuration.AutoSave.CleanAutoSaveOnLeavingGpose;
            if(ImGui.Checkbox("离开集体动作时清理自动保存", ref autoGPoseClear))
            {
                _configurationService.Configuration.AutoSave.CleanAutoSaveOnLeavingGpose = autoGPoseClear;
                _configurationService.ApplyChange();
            }

            var saveInterval = _configurationService.Configuration.AutoSave.AutoSaveInterval;
            if(ImGui.SliderInt("自动保存间隔", ref saveInterval, 15, 500, "%d 秒"))
            {
                _configurationService.Configuration.AutoSave.AutoSaveInterval = saveInterval;
                _configurationService.ApplyChange();
            }

            var maxSaves = _configurationService.Configuration.AutoSave.MaxAutoSaves;
            if(ImGui.SliderInt("最大自动保存次数", ref maxSaves, 3, 80))
            {
                _configurationService.Configuration.AutoSave.MaxAutoSaves = maxSaves;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawBrioIPC()
    {
        if(ImGui.CollapsingHeader("Brio API 与 IPC"))
        {
            bool enableBrioIpc = _configurationService.Configuration.IPC.EnableBrioIPC;
            if(ImGui.Checkbox("启用 Brio IPC", ref enableBrioIpc))
            {
                _configurationService.Configuration.IPC.EnableBrioIPC = enableBrioIpc;
                _configurationService.ApplyChange();
            }
            ImGui.Text($"Brio IPC 状态：{(enableBrioIpc ? "已启用" : "未启用")}");

            bool enableWebApi = _configurationService.Configuration.IPC.AllowWebAPI;
            if(ImGui.Checkbox("启用 Brio API", ref enableWebApi))
            {
                _configurationService.Configuration.IPC.AllowWebAPI = enableWebApi;
                _configurationService.ApplyChange();
            }

            ImGui.Text($"Brio API 状态：{(_webService.IsRunning ? "已启用" : "未启用")}");
        }
    }

    private void DrawNPCAppearanceHack()
    {
        if(ImGui.CollapsingHeader("外观", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableBrioColor = _configurationService.Configuration.Appearance.EnableBrioColor;
            if(ImGui.Checkbox("启用 Brio 颜色（主题的Header颜色不透明会导致看不见高级按钮）", ref enableBrioColor))
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

            var allowNPCHackBehavior = _configurationService.Configuration.Appearance.ApplyNPCHack;
            const string label = "Brio开启时机";
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X - 15);
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
        ImBrio.SeparatorText("选项");

        DrawPosingGeneralSection();
        DrawGPoseSection();
        DrawOverlaySection();
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

            bool autoSelectLight = _configurationService.Configuration.Posing.AutoSelectLightWhenClickingOnALight;
            if(ImGui.Checkbox("点击灯光实体时在灯光窗口中选中", ref autoSelectLight))
            {
                _configurationService.Configuration.Posing.AutoSelectLightWhenClickingOnALight = autoSelectLight;
                _configurationService.ApplyChange();
            }

            bool ifLightWindowOpenDontUseSceneManager = _configurationService.Configuration.Posing.IfLightWindowisOpenDontUseSceneManager;
            if(ImGui.Checkbox("灯光窗口打开时，不在场景管理器中显示灯光组件", ref ifLightWindowOpenDontUseSceneManager))
            {
                _configurationService.Configuration.Posing.IfLightWindowisOpenDontUseSceneManager = ifLightWindowOpenDontUseSceneManager;
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

            ImBrio.SeparatorText("变换器");

            bool enableAdvanedGizmo = _configurationService.Configuration.Posing.IsAdvancedGizmoEnabled;
            if(ImGui.Checkbox("默认启用高级变换器", ref enableAdvanedGizmo))
            {
                _configurationService.Configuration.Posing.IsAdvancedGizmoEnabled = enableAdvanedGizmo;
                _configurationService.ApplyChange();
            }

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

            ImBrio.SeparatorText("骨骼");

            bool showSkeletonLines = _configurationService.Configuration.Posing.ShowSkeletonLines;
            if(ImGui.Checkbox("显示骨骼线条", ref showSkeletonLines))
            {
                _configurationService.Configuration.Posing.ShowSkeletonLines = showSkeletonLines;
                _configurationService.ApplyChange();
            }

            using(ImRaii.Disabled(!showSkeletonLines))
            {
                bool skeletonLineToCircle = _configurationService.Configuration.Posing.SkeletonLineToCircle;
                if(ImGui.Checkbox("骨骼线条连接到圆环边缘", ref skeletonLineToCircle))
                {
                    _configurationService.Configuration.Posing.SkeletonLineToCircle = skeletonLineToCircle;
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
            }

            float circleSize = _configurationService.Configuration.Posing.BoneCircleSize;
            if(ImGui.DragFloat("骨骼节点圆环尺寸", ref circleSize, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.BoneCircleSize = circleSize;
                _configurationService.ApplyChange();
            }

            ImBrio.SeparatorText("叠加层颜色");

            ImGui.TextDisabled("骨骼圆点");

            using(ImRaii.PushIndent())
            {
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
            }

            ImGui.Spacing();
            ImGui.TextDisabled("实体颜色");

            bool standout = _configurationService.Configuration.Posing.ModelTransformStandout;
            if(ImGui.Checkbox("使[模型变换]骨骼突出显示", ref standout))
            {
                _configurationService.Configuration.Posing.ModelTransformStandout = standout;
                _configurationService.ApplyChange();
            }
            using(ImRaii.Disabled(!standout))
            using(ImRaii.PushIndent())
            {
                Vector4 modelTransformCircleStandOut = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.ModelTransformCircleStandOutColor);
                if(ImGui.ColorEdit4("[模型变换]骨骼突出显示颜色", ref modelTransformCircleStandOut, ImGuiColorEditFlags.NoInputs))
                {
                    _configurationService.Configuration.Posing.ModelTransformCircleStandOutColor = ImGui.ColorConvertFloat4ToU32(modelTransformCircleStandOut);
                    _configurationService.ApplyChange();
                }

                Vector4 lightCircleNormalColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.LightCircleNormalColor);
                if(ImGui.ColorEdit4("灯光节点标准颜色", ref lightCircleNormalColor, ImGuiColorEditFlags.NoInputs))
                {
                    _configurationService.Configuration.Posing.LightCircleNormalColor = ImGui.ColorConvertFloat4ToU32(lightCircleNormalColor);
                    _configurationService.ApplyChange();
                }

                Vector4 worldObjectTypeOverlayColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.WorldObjectOverlayColor);
                if(ImGui.ColorEdit4("世界物体突出颜色##entitytype", ref worldObjectTypeOverlayColor, ImGuiColorEditFlags.NoInputs))
                {
                    _configurationService.Configuration.Posing.WorldObjectOverlayColor = ImGui.ColorConvertFloat4ToU32(worldObjectTypeOverlayColor);
                    _configurationService.ApplyChange();
                }
            }

            ImGui.Spacing();
            ImGui.TextDisabled("骨骼连线");

            using(ImRaii.PushIndent())
            {
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

            ImGui.Spacing();
            ImGui.TextDisabled("自定义骨骼颜色");

            bool usePerCategoryLineColors = _configurationService.Configuration.Posing.UsePerCategoryLineColors;
            if(ImGui.Checkbox("按分类着色骨骼", ref usePerCategoryLineColors))
            {
                _configurationService.Configuration.Posing.UsePerCategoryLineColors = usePerCategoryLineColors;
                _configurationService.ApplyChange();
            }

            if(usePerCategoryLineColors)
            {
                using(ImRaii.PushIndent())
                {
                    var categoryColors = _configurationService.Configuration.Posing.BoneCategoryLineColors;
                    foreach(var category in _posingService.BoneCategories.Categories)
                    {
                        if(category.Type != BoneCategoryTypes.Filter)
                            continue;

                        if(!categoryColors.TryGetValue(category.Id, out var existingColor))
                            existingColor = _configurationService.Configuration.Posing.SkeletonLineActiveColor;

                        Vector4 catColor = ImGui.ColorConvertU32ToFloat4(existingColor);
                        if(ImGui.ColorEdit4($"{category.Name}##cat_{category.Id}", ref catColor, ImGuiColorEditFlags.NoInputs))
                        {
                            categoryColors[category.Id] = ImGui.ColorConvertFloat4ToU32(catColor);
                            _configurationService.ApplyChange();
                        }
                    }
                }
            }

            ImBrio.SeparatorText("圆点偏移");
            DrawBoneOverlayOffsets();
        }
    }

    // Bone Overlay Offsets

    private string _newBoneOffsetName = string.Empty;
    private string _boneOffsetSearch = string.Empty;

    private void DrawBoneOverlayOffsets()
    {
        bool useOverlayOffset = _configurationService.Configuration.Posing.UseOverlayOffset;
        if(ImGui.Checkbox("启用叠加层偏移", ref useOverlayOffset))
        {
            _configurationService.Configuration.Posing.UseOverlayOffset = useOverlayOffset;
            _configurationService.ApplyChange();
        }

        ImGui.TextDisabled("偏移叠加层中骨骼的圆点位置");

        ImBrio.VerticalPadding(5);

        using(ImRaii.Disabled(!useOverlayOffset))
        {
            var boneOffsets = _configurationService.Configuration.Posing.BoneOverlayOffsets;

            DrawOffsetTable(boneOffsets);

            ImGui.Spacing();

            DrawOffsetAddRow(boneOffsets);
        }

        ImBrio.VerticalPadding(10);
    }

    private void DrawOffsetTable(IDictionary<string, Vector3> boneOffsets)
    {
        if(boneOffsets.Count == 0)
        {
            ImGui.TextDisabled("无偏移。请在下方添加。");
            return;
        }

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingFixedFit;
        using var table = ImRaii.Table("##dotoffsets", 3, flags);
        if(!table.Success)
            return;

        ImGui.TableSetupColumn("###bone", ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("###offset", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("###button", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);

        string? toRemove = null;
        foreach(var bone in boneOffsets)
        {
            using var id = ImRaii.PushId(bone.Key);

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            bool known = IsKnownBone(bone.Key);
            var friendlyName = Localize.Get($"bones.{bone.Key}", bone.Key);
            if(known)
            {
                ImGui.Text(friendlyName);
            }
            else
            {
                ImGui.TextDisabled(friendlyName);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("自定义骨骼（不在目录中）");
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var offset = bone.Value;
            if(ImGui.DragFloat3("##offset", ref offset, 0.0001f, -10f, 10f, "%.4f"))
            {
                boneOffsets[bone.Key] = offset;
                _configurationService.ApplyChange();
            }

            ImGui.TableNextColumn();
            if(ImBrio.FontIconButton("###delButton", FontAwesomeIcon.Trash, $"Remove offset for '{bone.Key}'"))
                toRemove = bone.Key;
        }

        if(toRemove is not null)
        {
            boneOffsets.Remove(toRemove);
            _configurationService.ApplyChange();
        }
    }
    private void DrawOffsetAddRow(IDictionary<string, Vector3> boneOffsets)
    {
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        DrawBonePickerCombo(boneOffsets);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("###new_dotoffset", "或输入骨骼名称", ref _newBoneOffsetName, 128);

        ImGui.SameLine();
        var canAdd = !string.IsNullOrWhiteSpace(_newBoneOffsetName) && !boneOffsets.ContainsKey(_newBoneOffsetName.Trim());
        if(ImBrio.FontIconButton("###add_dotoffset", FontAwesomeIcon.Plus, "添加骨骼圆点偏移", canAdd))
        {
            boneOffsets[_newBoneOffsetName.Trim()] = Vector3.Zero;
            _configurationService.ApplyChange();
            _newBoneOffsetName = string.Empty;
            _boneOffsetSearch = string.Empty;
        }
    }
    private void DrawBonePickerCombo(IDictionary<string, Vector3> boneOffsets)
    {
        var preview = string.IsNullOrEmpty(_newBoneOffsetName) ? "选择骨骼" : _newBoneOffsetName;
        using var combo = ImRaii.Combo("##bonepicker", preview, ImGuiComboFlags.HeightLargest);
        if(!combo.Success)
            return;

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("###bonesearch", "搜索", ref _boneOffsetSearch, 64);

        ImGui.Separator();

        var allCategories = _posingService.BoneCategories.Categories;
        string? lastCategory = null;
        foreach(var category in allCategories)
        {
            if(category.Type != BoneCategoryTypes.Filter)
                continue;

            var matches = category.Bones.Where(b =>
                _boneOffsetSearch.Length == 0 ||
                b.Contains(_boneOffsetSearch, StringComparison.OrdinalIgnoreCase) ||
                Localize.Get($"bones.{b}", b).Contains(_boneOffsetSearch, StringComparison.OrdinalIgnoreCase));

            bool headerDrawn = false;
            foreach(var b in matches)
            {
                if(!headerDrawn)
                {
                    var parentCategory = allCategories.FirstOrDefault(c =>
                        c.Type == BoneCategoryTypes.Category && c.Bones.Contains(category.Id));

                    if(parentCategory != null && parentCategory.Name != lastCategory)
                    {
                        ImBrio.SeparatorText(parentCategory.Name);
                        lastCategory = parentCategory.Name;
                    }
                    ImGui.TextDisabled($"  {category.Name}");
                    headerDrawn = true;
                }

                using(ImRaii.PushIndent())
                {
                    bool used = boneOffsets.ContainsKey(b);
                    var friendlyName = Localize.Get($"bones.{b}", b);
                    using(ImRaii.Disabled(used))
                    {
                        if(ImGui.Selectable($"{friendlyName}{(used ? "  (added)" : "")}"))
                            _newBoneOffsetName = b;
                    }
                }
            }
        }
    }

    private bool IsKnownBone(string bone)
    {
        foreach(var category in _posingService.BoneCategories.Categories)
        {
            if(category.Type != BoneCategoryTypes.Filter)
                continue;

            foreach(var prefix in category.Bones)
                if(bone.StartsWith(prefix))
                    return true;
        }
        return false;
    }

    //
    private void DrawPosingGeneralSection()
    {
        var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
        if(ImGui.DragInt("可撤消历史记录", ref undoStackSize, 1, 0, 100))
        {
            _configurationService.Configuration.Posing.UndoStackSize = undoStackSize;
            _configurationService.ApplyChange();
        }

        bool swapRotationXandY = _configurationService.Configuration.Posing.SwapRotationXandY;
        if(ImGui.Checkbox("Swap Rotation X and Y in Transform Editors", ref swapRotationXandY))
        {
            _configurationService.Configuration.Posing.SwapRotationXandY = swapRotationXandY;
            _configurationService.ApplyChange();
        }
    }

    private void DrawOffsetSection()
    {
        var defaultTransformMovementSpeed = _configurationService.Configuration.Interface.DefaultTransformMovementSpeed;
        const string label1 = "变换移动速度";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label1).X - 15);
        if(ImGui.DragFloat(label1, ref defaultTransformMovementSpeed, 0.001f, 0.001f, 10f))
        {
            _configurationService.Configuration.Interface.DefaultTransformMovementSpeed = defaultTransformMovementSpeed;
            _configurationService.ApplyChange();
        }

        var defaultBoneTransformMovementSpeed = _configurationService.Configuration.Interface.DefaultBoneTransformMovementSpeed;
        const string label2 = "骨骼变换移动速度";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label2).X - 15);
        if(ImGui.DragFloat(label2, ref defaultBoneTransformMovementSpeed, 0.001f, 0.001f, 10f))
        {
            _configurationService.Configuration.Interface.DefaultBoneTransformMovementSpeed = defaultBoneTransformMovementSpeed;
            _configurationService.ApplyChange();
        }

        var defaultFreeCamMovementSpeed = _configurationService.Configuration.Interface.DefaultFreeCameraMovementSpeed;
        const string label3 = "自由相机移动速度";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label3).X - 15);
        if(ImGui.DragFloat(label3, ref defaultFreeCamMovementSpeed, 0.001f, 0.005f, 0.3f))
        {
            _configurationService.Configuration.Interface.DefaultFreeCameraMovementSpeed = defaultFreeCamMovementSpeed;
            _configurationService.ApplyChange();
        }

        var defaultFreeCamMouseSensitivity = _configurationService.Configuration.Interface.DefaultFreeCameraMouseSensitivity;
        const string label4 = "自由相机鼠标灵敏度";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label4).X - 15);
        if(ImGui.DragFloat(label4, ref defaultFreeCamMouseSensitivity, 0.001f, 0.001f, 0.2f))
        {
            _configurationService.Configuration.Interface.DefaultFreeCameraMouseSensitivity = defaultFreeCamMouseSensitivity;
            _configurationService.ApplyChange();
        }
    }

    bool resetSettings = false;
    private void DrawAdvancedTab()
    {
        ImBrio.SeparatorText("支持");

        if(ImGui.Button("复制支持信息到剪贴板"))
        {
            ImGui.SetClipboardText(Brio.GetDebugInfo());
        }

        ImGui.SameLine();

        if(ImGui.Button("复制日志到剪贴板"))
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Brio.GetDebugInfo())));
            Brio.Log.Warning("BRIOSUPPORT:" + base64);

            var logPath = ClientRegionHelper.GetDalamudLogPath();

            using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            var log = sr.ReadToEnd();
            ImGui.SetClipboardText(log);
        }

        ImBrio.SeparatorText("高级");

        if(ImGui.CollapsingHeader("场景管理器"))
        {
            DrawOpenBrioSetting();
            DrawHideSettings();
        }

        DrawNPCAppearanceHack();

        DrawEnvironmentSection();

        if(ImGui.CollapsingHeader("设置", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Checkbox("启用 [重置为默认设置] 按钮", ref resetSettings);

            using(ImRaii.Disabled(!resetSettings))
            {
                if(ImGui.Button("重置设置为默认", new(170 * ImGuiHelpers.GlobalScale, 0)))
                {
                    _configurationService.Reset();
                    resetSettings = false;
                }
            }
        }
    }

    private void DrawEnvironmentSection()
    {
        if(ImGui.CollapsingHeader("环境"))
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

            var resetAdvancedOnGPoseExit = _configurationService.Configuration.Environment.ResetAdvancedOnGPoseExit;
            if(ImGui.Checkbox("退出 GPose 时重置高级环境参数", ref resetAdvancedOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetAdvancedOnGPoseExit = resetAdvancedOnGPoseExit;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawLibraryTab()
    {
        DrawLibrarySection();
    }

    private void DrawLibrarySection()
    {
        LibrarySourcesEditor.Draw(null, _configurationService, _configurationService.Configuration.Library, _libraryPadding);
    }

    private void DrawKeysTab()
    {
        ImBrio.SeparatorText("选项");

        bool enableKeybinds = _configurationService.Configuration.InputManager.Enable;
        if(ImGui.Checkbox("启用键盘快捷键", ref enableKeybinds))
        {
            _configurationService.Configuration.InputManager.Enable = enableKeybinds;
            _configurationService.ApplyChange();
        }

        bool enableKeyHandlingOnKeyMod = _configurationService.Configuration.InputManager.EnableKeyHandlingOnKeyMod;
        if(ImGui.Checkbox("移动自由相机时，[SPACE], [Shift], [Ctrl] & [Alt]键将专用于相机控制", ref enableKeyHandlingOnKeyMod))
        {
            _configurationService.Configuration.InputManager.EnableKeyHandlingOnKeyMod = enableKeyHandlingOnKeyMod;
            _configurationService.ApplyChange();
        }

        bool handlingAllOnKeys = _configurationService.Configuration.InputManager.EnableConsumeAllInput;
        if(ImGui.Checkbox("在集体动作接管所有游戏输入", ref handlingAllOnKeys))
        {
            _configurationService.Configuration.InputManager.EnableConsumeAllInput = handlingAllOnKeys;
            _configurationService.ApplyChange();
        }

        bool showPrompts = _configurationService.Configuration.InputManager.ShowPromptsInGPose;
        if(ImGui.Checkbox("在集体动作中显示提示", ref showPrompts))
        {
            _configurationService.Configuration.InputManager.ShowPromptsInGPose = showPrompts;
            _configurationService.ApplyChange();
        }

        ImBrio.SeparatorText("按键绑定");

        if(ImGui.CollapsingHeader("自由相机"))
        {
            DrawKeyBind(InputAction.FreeCamera_Forward);
            DrawKeyBind(InputAction.FreeCamera_Backward);
            DrawKeyBind(InputAction.FreeCamera_Left);
            DrawKeyBind(InputAction.FreeCamera_Right);
            DrawKeyBind(InputAction.FreeCamera_Up);
            DrawKeyBind(InputAction.FreeCamera_UpAlt);
            DrawKeyBind(InputAction.FreeCamera_Down);
            DrawKeyBind(InputAction.FreeCamera_DownAlt);
            DrawKeyBind(InputAction.FreeCamera_IncreaseCamMovement);
            DrawKeyBind(InputAction.FreeCamera_DecreaseCamMovement);
        }

        using(ImRaii.Disabled(!enableKeybinds))
        {
            if(ImGui.CollapsingHeader("界面"))
            {
                DrawKeyBind(InputAction.Interface_ToggleBrioWindow);
                DrawKeyBind(InputAction.Posing_Undo);
                DrawKeyBind(InputAction.Posing_Redo);
                DrawKeyBind(InputAction.Interface_IncrementSmallModifier);
            }

            if(ImGui.CollapsingHeader("XAT 过场"))
            {
                DrawKeyBind(InputAction.Interface_StopCutscene);
                DrawKeyBind(InputAction.Interface_StartAllActorsAnimations);
                DrawKeyBind(InputAction.Interface_StopAllActorsAnimations);
            }

            if(ImGui.CollapsingHeader("摆姿"))
            {
                DrawKeyBind(InputAction.Posing_ToggleOverlay);
                DrawKeyBind(InputAction.Posing_HideOverlay);
                DrawKeyBind(InputAction.Posing_Freeze);
                DrawKeyBind(InputAction.Posing_DisableGizmo);
                DrawKeyBind(InputAction.Posing_DisableSkeleton);
                DrawKeyBind(InputAction.Posing_ToggleLink);
                DrawKeyBind(InputAction.Posing_Translate);
                DrawKeyBind(InputAction.Posing_Rotate);
                DrawKeyBind(InputAction.Posing_Scale);
                DrawKeyBind(InputAction.Posing_Universal);
                DrawKeyBind(InputAction.Posing_ToggleWorld);
            }
        }

        ImBrio.SeparatorText("高级");

        if(ImGui.CollapsingHeader("高级"))
        {
            bool flipKeybindsPastNinety = _configurationService.Configuration.InputManager.FlipKeyBindsPastNinety;
            if(ImGui.Checkbox("翻转自由相机按键绑定，超过-90/90度", ref flipKeybindsPastNinety))
            {
                _configurationService.Configuration.InputManager.FlipKeyBindsPastNinety = flipKeybindsPastNinety;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawKeyBind(InputAction keyAction)
    {
        string evtText = Localize.Get($"keys.{keyAction}") ?? keyAction.ToString();

        if(KeybindEditor.KeySelector(evtText, keyAction, _configurationService.Configuration.InputManager))
        {
            _configurationService.ApplyChange();
            _configurationService.Save();
        }
    }
}
