using Brio.Game.Penumbra;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using ImGuiNET;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Brio.Game.Penumbra
{
    public class ModPriorityAdjustmentWindow
    {
        private string _emoteName = string.Empty;
        private List<PenumbraModInfo> _allMods = new();
        private List<PenumbraModInfo> _displayMods = new();
        private List<PenumbraModInfo> _enabledMods = new();
        private List<PenumbraModInfo> _allModsCached = new();
        private bool _includeDisabled = false;
        private bool _includeDisabledChanged = false;
        private bool _showEmoteConflicts = false;
        private bool _showEmoteConflictsChanged = false;
        private bool _useTemporarySettings = false;
        private bool _useTemporarySettingsChanged = false;
        private bool _needsRefresh = false;
        private Action? _onPriorityChanged;
        
        private SetTemporaryModSettings? _setTemporaryModSettings;
        private QueryTemporaryModSettings? _queryTemporaryModSettings;
        private Dictionary<string, (bool IsTemporary, bool Enabled, int Priority)> _temporaryModStates = new();

        public void UpdateContent(string emoteName, List<PenumbraModInfo> mods, Action? onPriorityChanged = null)
        {
            _emoteName = emoteName;
            _allMods.Clear();
            _allMods.AddRange(mods);
            _needsRefresh = false;
            _onPriorityChanged = onPriorityChanged;
            
            if (_setTemporaryModSettings == null)
            {
                try
                {
                    var penumbraManager = PenumbraManager.Instance;
                    if (penumbraManager != null)
                    {
                        var pluginInterfaceField = typeof(PenumbraManager).GetField("_pluginInterface", 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (pluginInterfaceField?.GetValue(penumbraManager) is IDalamudPluginInterface pluginInterface)
                        {
                            _setTemporaryModSettings = new SetTemporaryModSettings(pluginInterface);
                            _queryTemporaryModSettings = new QueryTemporaryModSettings(pluginInterface);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Brio.Log.Warning($"初始化临时设置IPC订阅者失败: {ex.Message}");
                }
            }
            
            // 检测现有临时设置
            CheckExistingTemporarySettings();
            
            UpdateDisplayMods();
        }

        private void CheckExistingTemporarySettings()
        {
            if (_queryTemporaryModSettings == null) return;
            
            try
            {
                var currentCollection = GetCurrentCollection();
                if (currentCollection.HasValue)
                {
                    var existingTemporaryMods = new List<string>();
                    
                    foreach (var mod in _allMods)
                    {
                        var result = _queryTemporaryModSettings.Invoke(
                            currentCollection.Value.Id, 
                            mod.ModDirectory, 
                            out var settings, 
                            out var source, 
                            0, 
                            mod.ModName);
                        
                        if (result == PenumbraApiEc.Success && settings.HasValue)
                        {
                            // 记录所有临时设置，无论来源
                            _temporaryModStates[mod.ModDirectory] = (true, settings.Value.Item2, settings.Value.Item3);
                            
                            if (source != "Brio")
                            {
                                // 其他插件设置的临时设置
                                existingTemporaryMods.Add($"{mod.ModName} (来源: {source})");
                            }
                        }
                    }
                    
                    if (existingTemporaryMods.Count > 0)
                    {
                        var message = $"检测到现有临时设置:\n{string.Join("\n", existingTemporaryMods)}\n这些设置可能影响模组优先级显示。";
                        Brio.Log.Information(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"检测现有临时设置失败: {ex.Message}");
            }
        }

        public void DrawContent()
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(440, 240), new Vector2(900, 600));
            
            if (_needsRefresh)
            {
                UpdateDisplayMods();
                _needsRefresh = false;
            }
            
            if (_includeDisabledChanged)
            {
                _includeDisabledChanged = false;
                UpdateDisplayMods();
            }
            
            if (_showEmoteConflictsChanged)
            {
                _showEmoteConflictsChanged = false;
                UpdateDisplayMods();
            }
            
            if (_useTemporarySettingsChanged)
            {
                _useTemporarySettingsChanged = false;
                UpdateDisplayMods();
            }
            
            DrawHeader();
            DrawModTable();
        }

        private void DrawHeader()
        {
            int enabledCount = _allMods.Count(m => m.IsEnabled);
            int totalCount = _allMods.Count;
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"情感动作：{_emoteName} {enabledCount}/{totalCount}（启用/总数）");
            ImGui.SameLine();
            
            DrawHeaderButton("include_disabled", FontAwesomeIcon.EyeSlash, "显示未启用模组", 
                ref _includeDisabled, ref _includeDisabledChanged, ImGui.GetColorU32(ImGuiCol.CheckMark),
                "显示或隐藏已禁用的模组\n启用后可以看到所有模组，包括当前未启用的", "当前显示所有模组");
            
            ImGui.SameLine();
            
            DrawHeaderButton("show_conflicts", FontAwesomeIcon.ExclamationTriangle, "显示表情冲突模组", 
                ref _showEmoteConflicts, ref _showEmoteConflictsChanged, ImGui.GetColorU32(ImGuiCol.CheckMark),
                "显示或隐藏包含表情修改的模组\n这些模组可能与当前动作产生冲突", "当前显示冲突模组");
            
            ImGui.SameLine();
            
            bool wasTemporary = _useTemporarySettings;
            DrawHeaderButton("temporary_settings", FontAwesomeIcon.Clock, "临时设置", 
                ref _useTemporarySettings, ref _useTemporarySettingsChanged, ImGui.GetColorU32(ImGuiCol.CheckMark),
                "启用后，优先级和启用状态的修改将使用临时设置\n临时设置不会永久修改Penumbra模组配置，可以随时清除", "当前使用临时设置模式");
            
            if (wasTemporary && !_useTemporarySettings)
            {
                TryClearAllTemporarySettings();
            }
            
            // 显示临时设置状态
            var tempModCount = _temporaryModStates.Count(kvp => kvp.Value.IsTemporary);
            if (tempModCount > 0)
            {
                ImGui.SameLine();
                float t = (float)(ImGui.GetTime() * 0.18f);
                float hueStart = 0.75f, hueEnd = 0.92f;
                float interp = 0.5f * (1 + MathF.Sin(t * MathF.PI * 2));
                float hue = hueStart + (hueEnd - hueStart) * interp;
                float sat = 0.38f + 0.12f * interp;
                float val = 0.92f + 0.08f * interp;
                float r, g, b;
                ImGui.ColorConvertHSVtoRGB(hue, sat, val, out r, out g, out b);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(r, g, b, 1.0f));
                string statusText = _useTemporarySettings ? $"临时设置: {tempModCount}" : $"现有临时设置: {tempModCount}";
                ImGui.TextUnformatted(statusText);
                ImGui.PopStyleColor();
            }
            
            ImGui.Separator();
        }

        private void DrawHeaderButton(string id, FontAwesomeIcon icon, string tooltip, 
            ref bool state, ref bool changed, uint activeColor, string baseTip, string activeTip)
        {
            uint iconColor = state ? activeColor : 0xFFFFFFFF;
            if (ImBrio.FontIconButton(id, icon, tooltip, true, true, iconColor))
            {
                state = !state;
                changed = true;
            }
            
            if (ImGui.IsItemHovered())
            {
                string tip = baseTip;
                if (state)
                {
                    tip += $"\n{activeTip}";
                }
                ImGui.SetTooltip(tip);
            }
        }

        private void DrawModTable()
        {
            var contentHeight = ImGui.GetContentRegionAvail().Y;
            using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(1, 1))
                .Push(ImGuiStyleVar.ItemSpacing, new Vector2(2, 1))
                .Push(ImGuiStyleVar.FramePadding, new Vector2(2, 1));
            using var table = ImRaii.Table("##mod_priority_table", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, 
                new Vector2(ImGui.GetContentRegionAvail().X, contentHeight));
            if (!table)
                return;
            ImGui.TableSetupColumn("模组名称", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("镜头", ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableSetupColumn("优先级", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableHeadersRow();
            if (_displayMods.Count == 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "没有找到相关模组");
            }
            else
            {
                foreach (var mod in _displayMods)
                    DrawModTableRow(mod);
            }
        }

        private void DrawModTableRow(PenumbraModInfo mod)
        {
            var isEnabled = mod.IsEnabled;
            var priority = mod.Priority;
            var modName = mod.ModName;
            var xcpCount = mod.XcpFiles.Count;
            var hasEmoteConflicts = HasEmoteConflicts(mod);
            
            var hasTemporarySettings = _temporaryModStates.TryGetValue(mod.ModDirectory, out var tempState);
            var isTemporary = hasTemporarySettings && tempState.IsTemporary;
            
            if (isTemporary)
            {
                isEnabled = tempState.Enabled;
                priority = tempState.Priority;
            }
            
            var currentActiveMod = _allMods.Where(m => m.IsEnabled).OrderByDescending(m => m.Priority).FirstOrDefault();
            var isCurrentActiveMod = currentActiveMod?.ModDirectory == mod.ModDirectory;
            var isHighPriorityEmoteConflict = hasEmoteConflicts && 
                                            currentActiveMod != null && 
                                            mod.Priority > currentActiveMod.Priority;
            
            // 检查是否有其他插件的临时设置但Brio未启用临时设置
            var hasOtherTemporarySettings = isTemporary && !_useTemporarySettings;
            
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            
            Vector4 nameColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            if (isCurrentActiveMod)
            {
                nameColor = new Vector4(0.2f, 0.8f, 0.2f, 1.0f);
            }
            else if (isHighPriorityEmoteConflict)
            {
                nameColor = new Vector4(1.0f, 0.6f, 0.2f, 1.0f);
            }
            
            using (ImRaii.PushColor(ImGuiCol.Text, nameColor))
            {
                if (ImGui.Selectable(modName, false, ImGuiSelectableFlags.None, new Vector2(0, ImGui.GetTextLineHeight() * 0.8f)))
                    OpenModPage(mod);
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"模组目录：{mod.ModDirectory}");
                ImGui.Text("点击以在 Penumbra 中打开模组页面。");
                if (isCurrentActiveMod)
                {
                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1.0f), "当前正在使用的模组");
                }
                else if (hasEmoteConflicts)
                {
                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "此模组包含表情修改");
                    ImGui.Text("可能与当前动作产生冲突");
                }
                if (isTemporary)
                {
                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(0.8f, 0.4f, 1.0f, 1.0f), "使用临时设置");
                    var source = GetTemporarySettingsSource(mod.ModDirectory);
                    if (!string.IsNullOrEmpty(source))
                    {
                        ImGui.TextColored(new Vector4(0.8f, 0.4f, 1.0f, 1.0f), $"当前模组临时设置由 {source} 管理");
                        if (hasOtherTemporarySettings)
                        {
                            ImGui.Separator();
                            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "启用Brio临时设置以修改此模组");
                        }
                    }
                }
                ImGui.EndTooltip();
            }
            ImGui.TableNextColumn();
            float colWidth = ImGui.GetColumnWidth();
            float textWidth = ImGui.CalcTextSize(xcpCount.ToString()).X;
            float centerPosX = ImGui.GetCursorPosX() + (colWidth - textWidth) / 2f;
            ImGui.SetCursorPosX(centerPosX);
            ImGui.Text(xcpCount.ToString());
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("XCP文件数量");
                ImGui.EndTooltip();
            }
            ImGui.TableNextColumn();
            var priorityInput = priority;
            ImGui.SetNextItemWidth(38f);
            ImGui.BeginDisabled(hasOtherTemporarySettings);
            if (ImGui.InputInt($"##priority_{mod.ModDirectory}", ref priorityInput, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (priorityInput != priority)
                {
                    var result = SetModPriority(mod, priorityInput);
                    if (result == PenumbraApiEc.Success)
                    {
                        Brio.Log.Information($"成功设置模组 '{modName}' 优先级为 {priorityInput}");
                        mod.Priority = priorityInput;
                        _needsRefresh = true;
                        _onPriorityChanged?.Invoke();
                    }
                    else
                        Brio.Log.Warning($"设置模组 '{modName}' 优先级失败: {result}");
                }
            }
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();
                if (hasOtherTemporarySettings)
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "此模组有临时设置，无法修改永久优先级");
                    ImGui.Text("启用Brio临时设置以管理此模组");
                }
                else
                {
                    ImGui.Text("输入新的优先级数值");
                }
                ImGui.EndTooltip();
            }
            ImGui.TableNextColumn();
            colWidth = ImGui.GetColumnWidth();
            float buttonSizeX = ImGui.GetFrameHeight() * 0.8f;
            float buttonPosX = ImGui.GetCursorPosX() + (colWidth - buttonSizeX) / 2f;
            ImGui.SetCursorPosX(buttonPosX);
            var enabledMods = _displayMods.Where(m => m.IsEnabled).ToList();
            var maxPriority = enabledMods.Count > 0 ? enabledMods.Max(m => m.Priority) : 0;
            var maxPriorityMods = enabledMods.Where(m => m.Priority == maxPriority).ToList();
            var canPromote = isEnabled && (mod.Priority < maxPriority || (mod.Priority == maxPriority && maxPriorityMods.Count > 1));
            ImGui.BeginDisabled(!canPromote || hasOtherTemporarySettings);
            if (ImGui.Button($"▲##up_{mod.ModDirectory}", new Vector2(buttonSizeX, 0)))
            {
                var result = SetModToHighestPriorityForEmote(mod, _emoteName);
                if (result == PenumbraApiEc.Success)
                {
                    _needsRefresh = true;
                    _onPriorityChanged?.Invoke();
                }
                else
                    Brio.Log.Warning($"设置模组 '{modName}' 优先级失败: {result}");
            }
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();
                if (hasOtherTemporarySettings)
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "此模组有临时设置，无法修改永久优先级");
                    ImGui.Text("启用Brio临时设置以管理此模组");
                }
                else if (!isEnabled)
                    ImGui.Text($"模组 '{modName}' 未启用，无法设置优先级");
                else if (!canPromote && mod.Priority >= maxPriority)
                    ImGui.Text($"模组 '{modName}' 已经是最高优先级");
                else if (canPromote && mod.Priority == maxPriority)
                    ImGui.Text($"模组 '{modName}' 与其他模组并列最高优先级，可以提升");
                else
                    ImGui.Text($"将模组 '{modName}' 设置为最高优先级 ({maxPriority + 1})");
                ImGui.EndTooltip();
            }
            ImGui.TableNextColumn();
            colWidth = ImGui.GetColumnWidth();
            float dotWidth = ImGui.CalcTextSize("●").X;
            float crossWidth = ImGui.CalcTextSize("X").X;
            float btnWidth = Math.Max(dotWidth, crossWidth) + ImGui.GetStyle().FramePadding.X * 2f;
            float statusPosX = ImGui.GetCursorPosX() + (colWidth - btnWidth) / 2f;
            ImGui.SetCursorPosX(statusPosX);
            var statusText = isEnabled ? "●" : "X";
            var statusColor = isEnabled ? new Vector4(0.2f, 0.8f, 0.2f, 1.0f) : new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
            
            if (isTemporary)
            {
                statusColor = new Vector4(0.8f, 0.4f, 1.0f, 1.0f);
            }
            
            bool buttonClicked = false;
            ImGui.BeginDisabled(hasOtherTemporarySettings);
            using (ImRaii.PushColor(ImGuiCol.Text, statusColor))
            {
                buttonClicked = ImGui.Button($"{statusText}##toggle_{mod.ModDirectory}", new Vector2(btnWidth, 0));
            }
            ImGui.EndDisabled();
            
            if (buttonClicked)
            {
                var result = SetModEnabled(mod, !isEnabled);
                if (result == PenumbraApiEc.Success)
                {
                    _needsRefresh = true;
                    _onPriorityChanged?.Invoke();
                }
                else
                {
                    Brio.Log.Warning($"切换模组 '{modName}' 启用状态失败: {result}");
                }
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (hasOtherTemporarySettings)
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "此模组有临时设置，无法修改永久状态");
                    ImGui.Text("启用Brio临时设置以管理此模组");
                }
                else
                {
                    ImGui.Text(isEnabled ? "点击禁用模组" : "点击启用模组");
                }
                if (isTemporary)
                {
                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(0.8f, 0.4f, 1.0f, 1.0f), "临时设置");
                    var source = GetTemporarySettingsSource(mod.ModDirectory);
                    if (!string.IsNullOrEmpty(source))
                    {
                        ImGui.TextColored(new Vector4(0.8f, 0.4f, 1.0f, 1.0f), $"当前模组临时设置由 {source} 管理");
                    }
                }
                ImGui.EndTooltip();
            }
        }

        private bool HasEmoteConflicts(PenumbraModInfo mod)
        {
            return mod.EmoteNames.Any(emoteName => emoteName.StartsWith("表情："));
        }

        private void UpdateDisplayMods()
        {
            bool modsChanged = !_allModsCached.SequenceEqual(_allMods, new ModInfoComparer());
            
            if (modsChanged)
            {
                _allModsCached.Clear();
                _allModsCached.AddRange(_allMods);
                
                _enabledMods.Clear();
                _enabledMods.AddRange(_allMods.Where(m => m.IsEnabled));
                _enabledMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            
            // 无论是否启用临时设置，都查询现有临时设置
            QueryTemporarySettings();
            
            _displayMods.Clear();
            
            if (_showEmoteConflicts)
            {
                var allRelevantMods = new List<PenumbraModInfo>();
                allRelevantMods.AddRange(_allModsCached);
                
                var emoteConflictMods = PenumbraManager.Instance?.GetAllModsWithEmoteConflicts(_emoteName) ?? new List<PenumbraModInfo>();
                foreach (var conflictMod in emoteConflictMods)
                {
                    if (!allRelevantMods.Any(m => m.ModDirectory == conflictMod.ModDirectory))
                    {
                        allRelevantMods.Add(conflictMod);
                    }
                }
                
                if (_includeDisabled)
                {
                    _displayMods.AddRange(allRelevantMods);
                }
                else
                {
                    _displayMods.AddRange(allRelevantMods.Where(m => m.IsEnabled));
                }
            }
            else
            {
                if (_includeDisabled)
                {
                    _displayMods.AddRange(_allModsCached);
                }
                else
                {
                    _displayMods.AddRange(_enabledMods);
                }
            }
            
            _displayMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        private void RefreshModStates()
        {
            try
            {
                var penumbraManager = PenumbraManager.Instance;
                if (penumbraManager != null)
                {
                    // 重新查询每个模组的实际状态
                    foreach (var mod in _allMods)
                    {
                        var updateModInfoMethod = typeof(PenumbraManager).GetMethod("UpdateModInfo", 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        updateModInfoMethod?.Invoke(penumbraManager, new object[] { mod.ModDirectory });
                    }
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"刷新模组状态失败: {ex.Message}");
            }
        }

        private PenumbraApiEc SetModPriority(PenumbraModInfo mod, int newPriority)
        {
            return SetModSettings(mod, mod.IsEnabled, newPriority, 
                () => PenumbraManager.Instance?.SetModPriority(mod, newPriority) ?? PenumbraApiEc.UnknownError);
        }

        private PenumbraApiEc SetModEnabled(PenumbraModInfo mod, bool enabled)
        {
            return SetModSettings(mod, enabled, mod.Priority, 
                () => PenumbraManager.Instance?.SetModEnabled(mod, enabled) ?? PenumbraApiEc.UnknownError);
        }

        private PenumbraApiEc SetModToHighestPriorityForEmote(PenumbraModInfo mod, string emoteName)
        {
            var relevantMods = _allMods.Where(m => m.IsEnabled && 
                (m.EmoteNames.Contains(emoteName) || 
                 m.EmoteNames.Any(emoteName => emoteName.StartsWith("表情：")))).ToList();
            var maxPriority = relevantMods.Count > 0 ? relevantMods.Max(m => m.Priority) : 0;
            var newPriority = maxPriority + 1;
            
            return SetModSettings(mod, mod.IsEnabled, newPriority, 
                () => PenumbraManager.Instance?.SetModToHighestPriorityForEmote(mod, emoteName) ?? PenumbraApiEc.UnknownError);
        }

        private PenumbraApiEc SetModSettings(PenumbraModInfo mod, bool enabled, int priority, Func<PenumbraApiEc> fallbackAction)
        {
            if (_useTemporarySettings && _setTemporaryModSettings != null)
            {
                var result = SetTemporaryModSettings(mod, enabled, priority);
                if (result == PenumbraApiEc.Success)
                {
                    _temporaryModStates[mod.ModDirectory] = (true, enabled, priority);
                    mod.IsEnabled = enabled;
                    mod.Priority = priority;
                    UpdateModInfo(mod.ModDirectory);
                    return result;
                }
            }
            
            return fallbackAction();
        }

        private PenumbraApiEc SetTemporaryModSettings(PenumbraModInfo mod, bool enabled, int priority)
        {
            try
            {
                var currentCollection = GetCurrentCollection();
                if (currentCollection.HasValue)
                {
                    return _setTemporaryModSettings.Invoke(
                        currentCollection.Value.Id, 
                        mod.ModDirectory, 
                        false,
                        enabled, 
                        priority,
                        new Dictionary<string, IReadOnlyList<string>>(),
                        "Brio", 
                        0,
                        mod.ModName);
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"临时设置模组失败: {ex.Message}");
            }
            
            return PenumbraApiEc.UnknownError;
        }

        private void QueryTemporarySettings()
        {
            if (_queryTemporaryModSettings == null) return;
            
            try
            {
                var currentCollection = GetCurrentCollection();
                if (currentCollection.HasValue)
                {
                    _temporaryModStates.Clear();
                    
                    foreach (var mod in _allMods)
                    {
                        var result = _queryTemporaryModSettings.Invoke(
                            currentCollection.Value.Id, 
                            mod.ModDirectory, 
                            out var settings, 
                            out var source, 
                            0, 
                            mod.ModName);
                        
                        if (result == PenumbraApiEc.Success && settings.HasValue)
                        {
                            // 记录所有临时设置，无论来源
                            _temporaryModStates[mod.ModDirectory] = (true, settings.Value.Item2, settings.Value.Item3);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"查询临时设置失败: {ex.Message}");
            }
        }

        private void UpdateModInfo(string modDirectory)
        {
            try
            {
                var penumbraManager = PenumbraManager.Instance;
                if (penumbraManager != null)
                {
                    var updateModInfoMethod = typeof(PenumbraManager).GetMethod("UpdateModInfo", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    updateModInfoMethod?.Invoke(penumbraManager, new object[] { modDirectory });
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"更新模组信息失败: {ex.Message}");
            }
        }

        private void OpenModPage(PenumbraModInfo mod)
        {
            try
            {
                var penumbraManager = PenumbraManager.Instance;
                if (penumbraManager == null)
                {
                    Brio.Log.Warning("PenumbraManager未初始化，无法打开模组页面");
                    return;
                }
                var pluginInterface = GetPluginInterface(penumbraManager);
                if (pluginInterface != null)
                {
                    var openMainWindow = new OpenMainWindow(pluginInterface);
                    var result = openMainWindow.Invoke(TabType.Mods, mod.ModDirectory, mod.ModName);
                    if (result == PenumbraApiEc.Success)
                        Brio.Log.Information($"成功在Penumbra中打开模组: {mod.ModName}");
                    else
                        Brio.Log.Warning($"在Penumbra中打开模组失败: {result}");
                }
                else
                    Brio.Log.Warning("无法获取PluginInterface，无法打开模组页面");
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"打开模组页面时发生错误: {ex.Message}");
            }
        }

        private void TryClearAllTemporarySettings()
        {
            try
            {
                var penumbraManager = PenumbraManager.Instance;
                if (penumbraManager != null)
                {
                    var pluginInterface = GetPluginInterface(penumbraManager);
                    if (pluginInterface != null)
                    {
                        var removeAllTemporaryModSettings = new RemoveAllTemporaryModSettings(pluginInterface);
                        var getCurrentCollection = new GetCollection(pluginInterface);
                        var currentCollection = getCurrentCollection.Invoke(ApiCollectionType.Current);
                        if (currentCollection.HasValue)
                        {
                            var result = removeAllTemporaryModSettings.Invoke(currentCollection.Value.Id, 0);
                            if (result == PenumbraApiEc.Success)
                            {
                                Brio.Log.Information("已清除所有临时设置");
                            }
                            else
                            {
                                Brio.Log.Warning($"清除临时设置失败: {result}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"清除临时设置失败: {ex.Message}");
            }
        }

        private IDalamudPluginInterface? GetPluginInterface(PenumbraManager penumbraManager)
        {
            var pluginInterfaceField = typeof(PenumbraManager).GetField("_pluginInterface", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            return pluginInterfaceField?.GetValue(penumbraManager) as IDalamudPluginInterface;
        }

        private (Guid Id, string Name)? GetCurrentCollection()
        {
            var penumbraManager = PenumbraManager.Instance;
            if (penumbraManager != null)
            {
                var getCurrentCollectionField = typeof(PenumbraManager).GetField("_getCurrentCollection", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (getCurrentCollectionField?.GetValue(penumbraManager) is GetCollection getCurrentCollection)
                {
                    return getCurrentCollection.Invoke(ApiCollectionType.Current);
                }
            }
            return null;
        }

        private string GetTemporarySettingsSource(string modDirectory)
        {
            if (_queryTemporaryModSettings == null) return string.Empty;
            
            try
            {
                var currentCollection = GetCurrentCollection();
                if (currentCollection.HasValue)
                {
                    var result = _queryTemporaryModSettings.Invoke(
                        currentCollection.Value.Id, 
                        modDirectory, 
                        out var settings, 
                        out var source, 
                        0, 
                        string.Empty);
                    
                    if (result == PenumbraApiEc.Success && settings.HasValue)
                    {
                        return source;
                    }
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"获取临时设置来源失败: {ex.Message}");
            }
            
            return string.Empty;
        }

        private class ModInfoComparer : IEqualityComparer<PenumbraModInfo>
        {
            public bool Equals(PenumbraModInfo? x, PenumbraModInfo? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                
                return x.ModDirectory == y.ModDirectory &&
                       x.ModName == y.ModName &&
                       x.Priority == y.Priority &&
                       x.IsEnabled == y.IsEnabled &&
                       x.XcpFiles.Count == y.XcpFiles.Count &&
                       x.EmoteNames.Count == y.EmoteNames.Count;
            }

            public int GetHashCode(PenumbraModInfo obj)
            {
                return HashCode.Combine(obj.ModDirectory, obj.ModName, obj.Priority, obj.IsEnabled);
            }
        }
    }
} 