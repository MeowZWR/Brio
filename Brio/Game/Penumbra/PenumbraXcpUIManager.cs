 using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Brio.Config;
using Brio.Game.Cutscene;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace Brio.Game.Penumbra
{
    public class PenumbraXcpUIManager
    {
        private readonly PenumbraXcpService _xcpService;
        private readonly ConfigurationService _configService;
        private readonly CutsceneManager _cutsceneManager;
        private readonly ModPriorityAdjustmentWindow _priorityWindow = new();
        private bool _lastXcpExisted = false;
        
        // UI验证缓存
        private uint _lastValidationFrame = 0;
        private bool _cachedValidatePenumbra = false;
        private const uint UI_CACHE_INTERVAL = 10; // UI缓存刷新间隔
        
        // IPC调用缓存
        private System.Collections.Generic.List<PenumbraModInfo>? _cachedModsForEmote;
        private string _cachedEmoteNameForMods = string.Empty;
        private uint _modsForEmoteCacheFrame = 0;
        private const uint MODS_CACHE_INTERVAL = 60; // 模组缓存刷新间隔
        
        public PenumbraXcpUIManager(PenumbraXcpService xcpService, ConfigurationService configService, CutsceneManager cutsceneManager)
        {
            _xcpService = xcpService;
            _configService = configService;
            _cutsceneManager = cutsceneManager;
        }

        public void DrawPenumbraXcpControls(string cameraPath, Action<string> setCameraPath)
        {
            if (!ValidatePenumbra()) return;

            if (string.IsNullOrEmpty(_xcpService.CurrentEmoteName))
            {
                DrawBreathingText("未选择情感动作");
                return;
            }

            DrawStatusMessages();

            ImGui.AlignTextToFramePadding();
            DrawBreathingText("自动检测镜头文件:");
            ImGui.SameLine();

            DrawXcpFileCombo(_xcpService.CachedXcpFiles, cameraPath, setCameraPath);
            DrawControlButtons();
            DrawPriorityAdjustmentPopup();
        }

        private bool ValidatePenumbra()
        {
            var currentFrame = (uint)ImGui.GetFrameCount();
            if (currentFrame - _lastValidationFrame < UI_CACHE_INTERVAL)
                return _cachedValidatePenumbra;

            _cachedValidatePenumbra = PenumbraManager.Instance?.IsPenumbraAvailable() == true;
            _lastValidationFrame = currentFrame;

            if (!_cachedValidatePenumbra)
            {
                if (PenumbraManager.Instance == null)
                    ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), "Penumbra未连接");
                else
                    ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), "Penumbra连接失败");
            }

            return _cachedValidatePenumbra;
        }

        private void DrawStatusMessages()
        {
            if (PenumbraManager.Instance?.HasEverRefreshed != true)
            {
                DrawBreathingText("本次会话尚未获取Penumbra模组状态，请点击下方刷新按钮获取。");
            }
            else if (PenumbraManager.Instance.HasModChangesSinceLastRefresh)
            {
                DrawBreathingText("检测到Penumbra模组设置变动，相机文件不正确时请手动刷新。");
            }
        }

        private void DrawXcpFileCombo(System.Collections.Generic.IReadOnlyList<string> xcpFiles, string cameraPath, Action<string> setCameraPath)
        {
            string preview = GetComboPreviewText(xcpFiles);
            
            ImGui.SetNextItemWidth(180f);
            bool comboOpen = ImGui.BeginCombo("###penumbra_xcp_combo", preview);
            
            if (comboOpen)
            {
                DrawComboItems(xcpFiles, setCameraPath);
                ImGui.EndCombo();
            }
        }

        private string GetComboPreviewText(System.Collections.Generic.IReadOnlyList<string> xcpFiles)
        {
            if (xcpFiles.Count == 0) return "未找到镜头文件";
            
            if (string.IsNullOrEmpty(_xcpService.SelectedXcpFile)) return "选择XCP文件...";
            
            return _xcpService.IsSelectedFromPenumbra 
                ? Path.GetFileName(_xcpService.SelectedXcpFile)
                : "正在使用上方相机路径";
        }

        private void DrawComboItems(System.Collections.Generic.IReadOnlyList<string> xcpFiles, Action<string> setCameraPath)
        {
            if (xcpFiles.Count == 0)
            {
                ImGui.Selectable("无可用XCP文件", true, ImGuiSelectableFlags.Disabled);
                return;
            }

            if (ImGui.Selectable("选择XCP文件...", string.IsNullOrEmpty(_xcpService.SelectedXcpFile)))
            {
                _xcpService.ClearSelectedXcpFile();
                setCameraPath(string.Empty);
            }
            
            foreach (var xcpFile in xcpFiles)
            {
                var fileName = Path.GetFileName(xcpFile);
                bool isSelected = _xcpService.SelectedXcpFile == xcpFile && _xcpService.IsSelectedFromPenumbra;
                
                if (ImGui.Selectable(fileName, isSelected) && 
                    _xcpService.SelectXcpFileFromPenumbra(xcpFile))
                {
                    setCameraPath(xcpFile);
                    _cutsceneManager.StartAllActorAnimationsOnPlay = true;
                }
            }
        }

        private void DrawControlButtons()
        {
            ImGui.SameLine();
            
            if (ImBrio.FontIconButton("refreshPenumbraModInfo", FontAwesomeIcon.Repeat, "手动刷新获取模组信息"))
                PenumbraManager.Instance?.RefreshModInfo();

            ImGui.SameLine();
            DrawHelpButton();

            ImGui.SameLine();
            DrawPriorityAdjustmentButton();

            ImGui.SameLine();
            DrawClipboardImportButton();

            ImGui.SameLine();
            DrawXcpFolderButton();
        }

        private void DrawHelpButton()
        {
            Dalamud.Interface.Components.ImGuiComponents.HelpMarker("提示");
            if (!ImGui.IsItemHovered()) return;

            var modInfo = _xcpService.CachedModInfoForEmote;
            ImGui.BeginTooltip();
            
            DrawHelpTooltipContent(modInfo);
            DrawHelpText();
            
            ImGui.EndTooltip();
        }

        private void DrawHelpTooltipContent(PenumbraModInfo? modInfo)
        {
            string modName = modInfo?.ModName ?? "未知";
            string modPath = _xcpService.GetCurrentModPath();
            
            ImGui.Text("当前动作模组："); ImGui.SameLine(); 
            ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), modName);
            ImGui.Text("文件系统路径："); ImGui.SameLine(); 
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), modPath);
            
            DrawConflictWarning(modInfo);
        }

        private void DrawConflictWarning(PenumbraModInfo? modInfo)
        {
            if (modInfo == null || string.IsNullOrEmpty(_xcpService.CurrentEmoteName)) return;

            // 性能优化：限制冲突检查的执行频率
            var currentFrame = (uint)ImGui.GetFrameCount();
            if (currentFrame % 120 != 0) return;

            var allMods = PenumbraManager.Instance?.GetAllMods() ?? new System.Collections.Generic.List<PenumbraModInfo>();
            var samePriorityMods = allMods
                .Where(m => m.IsEnabled && m.Priority == modInfo.Priority && 
                           m.ModDirectory != modInfo.ModDirectory &&
                           m.EmoteNames.Contains(_xcpService.CurrentEmoteName))
                .ToList();
            
            if (samePriorityMods.Count == 0) return;

            ImGui.Separator();
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "[!] 检测到修改同样动作的相同优先级模组：");
            
            var displayMods = samePriorityMods.Take(3).ToList();
            foreach (var sameMod in displayMods)
                ImGui.Text($"  • {sameMod.ModName}");
                
            if (samePriorityMods.Count > 3)
                ImGui.Text($"  • ... 还有 {samePriorityMods.Count - 3} 个模组");
                
            ImGui.Text("建议调整优先级以避免冲突");
        }

        private void DrawPriorityAdjustmentButton()
        {
            var emoteNameForPriority = _xcpService.CurrentEmoteName ?? string.Empty;
            
            // 缓存模组列表查询结果
            System.Collections.Generic.List<PenumbraModInfo>? modsForEmote = null;
            var currentFrame = (uint)ImGui.GetFrameCount();
            
            if (_cachedEmoteNameForMods == emoteNameForPriority && 
                currentFrame - _modsForEmoteCacheFrame < MODS_CACHE_INTERVAL)
            {
                modsForEmote = _cachedModsForEmote;
            }
            else
            {
                modsForEmote = PenumbraManager.Instance?.GetModsForEmote(emoteNameForPriority);
                _cachedModsForEmote = modsForEmote;
                _cachedEmoteNameForMods = emoteNameForPriority;
                _modsForEmoteCacheFrame = currentFrame;
            }
            
            bool hasModsForEmote = modsForEmote != null && modsForEmote.Count > 0;
            
            ImGui.BeginDisabled(!hasModsForEmote);
            if (ImBrio.FontIconButton("adjustModPriority", FontAwesomeIcon.SortNumericUp, "调整模组优先级"))
            {
                var currentHighestMod = PenumbraManager.Instance?.GetEffectiveModInfoForEmote(emoteNameForPriority);
                
                if (modsForEmote != null)
                {
                    _priorityWindow.UpdateContent(emoteNameForPriority, modsForEmote, () => {
                        var newHighestMod = PenumbraManager.Instance?.GetEffectiveModInfoForEmote(emoteNameForPriority);
                        if (currentHighestMod?.ModDirectory != newHighestMod?.ModDirectory)
                            _xcpService.ClearSelectedXcpFile();
                        
                        _cachedModsForEmote = null;
                        _cachedEmoteNameForMods = string.Empty;
                    });
                }
                ImGui.OpenPopup("mod_priority_adjustment_popup");
            }
            
            string tooltip = hasModsForEmote 
                ? $"调整 '{emoteNameForPriority}' 相关模组的优先级"
                : "当前情感动作没有相关的Penumbra模组";
            
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(tooltip);
                
            ImGui.EndDisabled();
        }

        private void DrawClipboardImportButton()
        {
            bool enabled = PenumbraManager.Instance?.HasEverRefreshed == true;
            
            ImGui.BeginDisabled(!enabled);
            if (ImBrio.FontIconButton("importXcp", FontAwesomeIcon.Clipboard, "从剪贴板导入.xcp文件到该模组"))
            {
                var modPath = _xcpService.GetCurrentModPath();
                var importer = new PenumbraClipboardImporter(
                    msg => Brio.Log.Information(msg), 
                    () => _xcpService.RefreshXcpCache());
                importer.ImportXcpFromClipboard(modPath);
            }
            
            string tooltip = enabled ? "从剪贴板导入.xcp文件" : "请先手动刷新获取模组信息";
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(tooltip);
                
            ImGui.EndDisabled();
        }

        private void DrawXcpFolderButton()
        {
            var currentFrame = (uint)ImGui.GetFrameCount();
            bool xcpExists = _xcpService.XcpFolderExists();
            bool ctrlDown = ImGui.GetIO().KeyCtrl;
            bool enabled = (xcpExists || ctrlDown) && PenumbraManager.Instance?.HasEverRefreshed == true;
            
            if (xcpExists != _lastXcpExisted && PenumbraManager.Instance?.HasEverRefreshed == true)
            {
                if (xcpExists && !_lastXcpExisted)
                {
                    Brio.Log.Debug("检测到XCP文件夹被外部创建，启动文件监控...");
                    _xcpService.RefreshXcpCache();
                }
                else if (!xcpExists && _lastXcpExisted)
                {
                    Brio.Log.Debug("检测到XCP文件夹被外部删除，停止文件监控...");
                    _xcpService.RefreshXcpCache();
                }
                _lastXcpExisted = xcpExists;
            }
            
            var icon = xcpExists ? FontAwesomeIcon.FolderOpen : FontAwesomeIcon.Plus;
            var tooltip = xcpExists ? "在文件资源管理器中打开XCP文件夹" : "按住Ctrl点击创建XCP文件夹";
            
            ImGui.BeginDisabled(!enabled);
            bool clicked = ImBrio.FontIconButton("openXcpFolder", icon, tooltip, true);
            ImGui.EndDisabled();
            
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                var finalTooltip = PenumbraManager.Instance?.HasEverRefreshed == true ? tooltip : "请先手动刷新获取模组信息";
                ImGui.SetTooltip(finalTooltip);
            }
            
            if (clicked && enabled)
            {
                if (!xcpExists && ctrlDown)
                {
                    if (_xcpService.TryCreateXcpFolder())
                    {
                        Brio.Log.Debug("XCP 文件夹已创建，刷新缓存...");
                        _xcpService.RefreshXcpCache();
                    }
                }
                else
                {
                    _xcpService.TryOpenXcpFolderInExplorer();
                }
            }
        }

        private void DrawPriorityAdjustmentPopup()
        {
            using var popup = ImRaii.Popup("mod_priority_adjustment_popup", ImGuiWindowFlags.NoCollapse);
            if (popup.Success)
                _priorityWindow.DrawContent();
        }

        private void DrawHelpText()
        {
            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), "* 若该模组包含对此动作的修改，但实际未启用相关选项，且其优先级最高，仍将被识别为当前动作的来源。");
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "* NightLife等大型模组包含大量动作修改，可能导致识别结果不准确，请尽量避免将其优先级设为最高。");
            
            if (_xcpService.CachedXcpFiles.Count == 0)
            {
                var orangeColor = new Vector4(1.0f, 0.6f, 0.2f, 1.0f);
                ImGui.Text("未检测到"); ImGui.SameLine(); 
                ImGui.TextColored(orangeColor, "XCP"); ImGui.SameLine(); 
                ImGui.Text("文件夹。请创建"); ImGui.SameLine(); 
                ImGui.TextColored(orangeColor, "XCP"); ImGui.SameLine(); 
                ImGui.Text("文件夹，放入后缀名为"); ImGui.SameLine(); 
                ImGui.TextColored(orangeColor, ".xcp"); ImGui.SameLine(); 
                ImGui.Text("的镜头文件。");
            }
        }

        private static void DrawBreathingText(string text)
        {
            float t = (float)(ImGui.GetTime() * 0.12f);
            float interp = 0.5f * (1 + MathF.Sin(t * MathF.PI * 2));
            
            float hue = 0.5f + (0.92f - 0.5f) * interp;
            float sat = 0.38f + 0.12f * interp;
            float val = 0.92f + 0.08f * interp;
            
            float r = 0, g = 0, b = 0;
            ImGui.ColorConvertHSVtoRGB(hue, sat, val, ref r, ref g, ref b);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(r, g, b, 1.0f));
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
        }
    }
} 