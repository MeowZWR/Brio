using Brio.Game.Penumbra;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Bindings.ImGui;
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
            
            if (PenumbraManager.Instance != null)
            {
                PenumbraManager.Instance.ModInfoChanged -= OnPenumbraModInfoChanged;
                PenumbraManager.Instance.ModInfoChanged += OnPenumbraModInfoChanged;
            }
            
            InitializeTemporarySettingsIpc();
            CheckExistingTemporarySettings();
            UpdateDisplayMods();
        }

        private void InitializeTemporarySettingsIpc()
        {
            if (_setTemporaryModSettings != null) return;

            try
            {
                var pluginInterface = GetPluginInterface(PenumbraManager.Instance);
                if (pluginInterface != null)
                {
                    _setTemporaryModSettings = new SetTemporaryModSettings(pluginInterface);
                    _queryTemporaryModSettings = new QueryTemporaryModSettings(pluginInterface);
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"初始化临时设置IPC订阅者失败: {ex.Message}");
            }
        }

        private void OnPenumbraModInfoChanged()
        {
            _allModsCached.Clear();
            _needsRefresh = true;
        }

        private void CheckExistingTemporarySettings()
        {
            if (_queryTemporaryModSettings == null) return;
            
            try
            {
                var currentCollection = GetCurrentCollection();
                if (!currentCollection.HasValue) return;

                var existingTemporaryMods = new List<string>();
                
                foreach (var mod in _allMods)
                {
                    var result = _queryTemporaryModSettings.Invoke(
                        currentCollection.Value.Id, mod.ModDirectory, out var settings, 
                        out var source, 0, mod.ModName);
                    
                    if (result == PenumbraApiEc.Success && settings.HasValue)
                    {
                        _temporaryModStates[mod.ModDirectory] = (true, settings.Value.Item2, settings.Value.Item3);
                        
                        if (source != "Brio")
                            existingTemporaryMods.Add($"{mod.ModName} (来源: {source})");
                    }
                }
                
                if (existingTemporaryMods.Count > 0)
                {
                    var message = $"检测到现有临时设置:\n{string.Join("\n", existingTemporaryMods)}\n这些设置可能影响模组优先级显示。";
                    Brio.Log.Information(message);
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
            
            ProcessStateChanges();
            DrawHeader();
            DrawModTable();
        }

        private void ProcessStateChanges()
        {
            if (_needsRefresh || _includeDisabledChanged || _showEmoteConflictsChanged || _useTemporarySettingsChanged)
            {
                UpdateDisplayMods();
                _needsRefresh = false;
                _includeDisabledChanged = false;
                _showEmoteConflictsChanged = false;
                _useTemporarySettingsChanged = false;
            }
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
                TryClearAllTemporarySettings();
            
            DrawTemporarySettingsStatus();
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
        
        private void DrawTemporarySettingsStatus()
        {
            var tempModCount = _temporaryModStates.Count(kvp => kvp.Value.IsTemporary);
            if (tempModCount == 0) return;

            ImGui.SameLine();
            float t = (float)(ImGui.GetTime() * 0.18f);
            float interp = 0.5f * (1 + MathF.Sin(t * MathF.PI * 2));
            float hue = 0.75f + (0.92f - 0.75f) * interp;
            float sat = 0.38f + 0.12f * interp;
            float val = 0.92f + 0.08f * interp;
            float r = 0, g = 0, b = 0;
            ImGui.ColorConvertHSVtoRGB(hue, sat, val, ref r, ref g, ref b);
            
            using var color = ImRaii.PushColor(ImGuiCol.Text, new Vector4(r, g, b, 1.0f));
            string statusText = _useTemporarySettings ? $"临时设置: {tempModCount}" : $"现有临时设置: {tempModCount}";
            ImGui.TextUnformatted(statusText);
        }

        private void DrawModTable()
        {
            var contentHeight = ImGui.GetContentRegionAvail().Y;
            using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(1, 1))
                .Push(ImGuiStyleVar.ItemSpacing, new Vector2(2, 1))
                .Push(ImGuiStyleVar.FramePadding, new Vector2(2, 1));
            using var table = ImRaii.Table("##mod_priority_table", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, 
                new Vector2(ImGui.GetContentRegionAvail().X, contentHeight));
            if (!table) return;
            
            SetupTableColumns();
            
            if (_displayMods.Count == 0)
                DrawEmptyRow();
            else
                _displayMods.ForEach(DrawModTableRow);
        }

        private void SetupTableColumns()
        {
            ImGui.TableSetupColumn("模组名称", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("镜头", ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableSetupColumn("优先级", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableHeadersRow();
        }

        private void DrawEmptyRow()
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "没有找到相关模组");
        }

        private void DrawModTableRow(PenumbraModInfo mod)
        {
            var (effectiveEnabled, effectivePriority, isTemporary) = GetEffectiveModState(mod);
            var hasOtherTemporarySettings = isTemporary && !_useTemporarySettings;
            
            ImGui.TableNextRow();
            DrawModNameColumn(mod, effectiveEnabled, effectivePriority, isTemporary, hasOtherTemporarySettings);
            DrawXcpCountColumn(mod);
            DrawPriorityColumn(mod, effectivePriority, hasOtherTemporarySettings);
            DrawPromoteButton(mod, effectiveEnabled, effectivePriority, hasOtherTemporarySettings);
            DrawStatusColumn(mod, effectiveEnabled, isTemporary, hasOtherTemporarySettings);
        }

        private (bool enabled, int priority, bool isTemporary) GetEffectiveModState(PenumbraModInfo mod)
        {
            if (_temporaryModStates.TryGetValue(mod.ModDirectory, out var tempState) && tempState.IsTemporary)
                return (tempState.Enabled, tempState.Priority, true);
            return (mod.IsEnabled, mod.Priority, false);
        }

        private void DrawModNameColumn(PenumbraModInfo mod, bool effectiveEnabled, int effectivePriority, bool isTemporary, bool hasOtherTemporarySettings)
        {
            ImGui.TableNextColumn();
            
            var color = GetModNameColor(mod, effectiveEnabled, effectivePriority);
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, color);
            
            if (ImGui.Selectable(mod.ModName, false, ImGuiSelectableFlags.None, 
                new Vector2(0, ImGui.GetTextLineHeight() * 0.8f)))
                OpenModPage(mod);

            if (ImGui.IsItemHovered())
                DrawModTooltip(mod, isTemporary, hasOtherTemporarySettings);
        }

        private Vector4 GetModNameColor(PenumbraModInfo mod, bool effectiveEnabled, int effectivePriority)
        {
            var currentActiveMod = _allMods.Where(m => GetEffectiveEnabled(m))
                .OrderByDescending(m => GetEffectivePriority(m)).FirstOrDefault();
            var isCurrentActiveMod = currentActiveMod?.ModDirectory == mod.ModDirectory;
            
            if (isCurrentActiveMod)
                return new Vector4(0.2f, 0.8f, 0.2f, 1.0f);
                
            var hasEmoteConflicts = mod.EmoteNames.Any(name => name.StartsWith("表情："));
            var isHighPriorityEmoteConflict = hasEmoteConflicts && currentActiveMod != null && 
                                            effectivePriority > GetEffectivePriority(currentActiveMod);
            
            return isHighPriorityEmoteConflict 
                ? new Vector4(1.0f, 0.6f, 0.2f, 1.0f)
                : new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        }

        private void DrawModTooltip(PenumbraModInfo mod, bool isTemporary, bool hasOtherTemporarySettings)
        {
            ImGui.BeginTooltip();
            ImGui.Text($"模组目录：{mod.ModDirectory}");
            ImGui.Text("点击以在 Penumbra 中打开模组页面。");
            
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

        private void DrawXcpCountColumn(PenumbraModInfo mod)
        {
            ImGui.TableNextColumn();
            var xcpCount = mod.XcpFiles.Count;
            CenterText(xcpCount.ToString());
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("XCP文件数量");
                ImGui.EndTooltip();
            }
        }

        private void DrawPriorityColumn(PenumbraModInfo mod, int effectivePriority, bool hasOtherTemporarySettings)
        {
            ImGui.TableNextColumn();
            var priorityInput = effectivePriority;
            ImGui.SetNextItemWidth(38f);
            
            using var disabled = ImRaii.Disabled(hasOtherTemporarySettings);
            if (ImGui.InputInt($"##priority_{mod.ModDirectory}", ref priorityInput, 1, 0, default, ImGuiInputTextFlags.EnterReturnsTrue) &&
                priorityInput != effectivePriority)
            {
                var result = SetModPriority(mod, priorityInput);
                if (result == PenumbraApiEc.Success)
                {
                    Brio.Log.Debug($"成功设置模组 '{mod.ModName}' 优先级为 {priorityInput}");
                    mod.Priority = priorityInput;
                    _needsRefresh = true;
                    _onPriorityChanged?.Invoke();
                }
                else
                    Brio.Log.Warning($"设置模组 '{mod.ModName}' 优先级失败: {result}");
            }

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(hasOtherTemporarySettings ? "此模组有临时设置，无法修改永久优先级" : "输入新的优先级数值");
        }

        private void DrawPromoteButton(PenumbraModInfo mod, bool effectiveEnabled, int effectivePriority, bool hasOtherTemporarySettings)
        {
            ImGui.TableNextColumn();
            CenterButton("▲", $"up_{mod.ModDirectory}", () => SetModToHighestPriorityForEmote(mod, _emoteName),
                CanPromoteMod(mod, effectiveEnabled, effectivePriority) && !hasOtherTemporarySettings);
        }

        private bool CanPromoteMod(PenumbraModInfo mod, bool effectiveEnabled, int effectivePriority)
        {
            if (!effectiveEnabled) return false;
            
            var enabledMods = _displayMods.Where(m => GetEffectiveEnabled(m)).ToList();
            var maxPriority = enabledMods.Count > 0 ? enabledMods.Max(m => GetEffectivePriority(m)) : 0;
            var maxPriorityMods = enabledMods.Where(m => GetEffectivePriority(m) == maxPriority).ToList();
            
            return effectivePriority < maxPriority || (effectivePriority == maxPriority && maxPriorityMods.Count > 1);
        }

        private void DrawStatusColumn(PenumbraModInfo mod, bool effectiveEnabled, bool isTemporary, bool hasOtherTemporarySettings)
        {
            ImGui.TableNextColumn();
            
            var statusText = effectiveEnabled ? "●" : "X";
            var statusColor = effectiveEnabled 
                ? new Vector4(0.2f, 0.8f, 0.2f, 1.0f) 
                : new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
            
            if (isTemporary)
                statusColor = new Vector4(0.8f, 0.4f, 1.0f, 1.0f);
            
            using var disabled = ImRaii.Disabled(hasOtherTemporarySettings);
            using var color = ImRaii.PushColor(ImGuiCol.Text, statusColor);
            
            if (CenterButton(statusText, $"toggle_{mod.ModDirectory}", () => SetModEnabled(mod, !effectiveEnabled)))
            {
                _needsRefresh = true;
                _onPriorityChanged?.Invoke();
            }

            if (ImGui.IsItemHovered())
                DrawStatusTooltip(effectiveEnabled, isTemporary, hasOtherTemporarySettings);
        }

        private void DrawStatusTooltip(bool effectiveEnabled, bool isTemporary, bool hasOtherTemporarySettings)
        {
            ImGui.BeginTooltip();
            if (hasOtherTemporarySettings)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "此模组有临时设置，无法修改永久状态");
                ImGui.Text("启用Brio临时设置以管理此模组");
            }
            else
            {
                ImGui.Text(effectiveEnabled ? "点击禁用模组" : "点击启用模组");
            }
            ImGui.EndTooltip();
        }

        private void CenterText(string text)
        {
            float colWidth = ImGui.GetColumnWidth();
            float textWidth = ImGui.CalcTextSize(text).X;
            float centerPosX = ImGui.GetCursorPosX() + (colWidth - textWidth) / 2f;
            ImGui.SetCursorPosX(centerPosX);
            ImGui.Text(text);
        }

        private bool CenterButton(string text, string id, Action onClick, bool enabled = true)
        {
            float colWidth = ImGui.GetColumnWidth();
            float buttonWidth = Math.Max(ImGui.CalcTextSize(text).X, ImGui.CalcTextSize("●").X) + ImGui.GetStyle().FramePadding.X * 2f;
            float centerPosX = ImGui.GetCursorPosX() + (colWidth - buttonWidth) / 2f;
            ImGui.SetCursorPosX(centerPosX);
            
            using var disabled = ImRaii.Disabled(!enabled);
            bool clicked = ImGui.Button($"{text}##{id}", new Vector2(buttonWidth, 0));
            
            if (clicked && enabled)
                onClick();
                
            return clicked && enabled;
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
            
            QueryTemporarySettings();
            
            _displayMods.Clear();
            var baseCollection = _showEmoteConflicts ? GetRelevantMods() : _allModsCached;
            
            _displayMods.AddRange(_includeDisabled 
                ? baseCollection 
                : baseCollection.Where(m => GetEffectiveEnabled(m)));
            
            _displayMods.Sort((a, b) => GetEffectivePriority(b).CompareTo(GetEffectivePriority(a)));
        }

        private List<PenumbraModInfo> GetRelevantMods()
        {
            var allRelevantMods = new List<PenumbraModInfo>(_allModsCached);
            
            var emoteConflictMods = PenumbraManager.Instance?.GetAllModsWithEmoteConflicts(_emoteName) ?? new List<PenumbraModInfo>();
            foreach (var conflictMod in emoteConflictMods)
            {
                if (!allRelevantMods.Any(m => m.ModDirectory == conflictMod.ModDirectory))
                    allRelevantMods.Add(conflictMod);
            }
            
            return allRelevantMods;
        }

        private void QueryTemporarySettings()
        {
            if (_queryTemporaryModSettings == null) return;
            
            try
            {
                var currentCollection = GetCurrentCollection();
                if (!currentCollection.HasValue) return;

                var oldCount = _temporaryModStates.Count;
                _temporaryModStates.Clear();
                
                foreach (var mod in _allMods)
                {
                    var result = _queryTemporaryModSettings.Invoke(currentCollection.Value.Id, mod.ModDirectory, 
                        out var settings, out var source, 0, mod.ModName);
                    
                    if (result == PenumbraApiEc.Success && settings.HasValue)
                        _temporaryModStates[mod.ModDirectory] = (true, settings.Value.Item2, settings.Value.Item3);
                }
                
                if (oldCount != _temporaryModStates.Count)
                    _needsRefresh = true;
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"查询临时设置失败: {ex.Message}");
            }
        }

        private int GetEffectivePriority(PenumbraModInfo mod) =>
            _temporaryModStates.TryGetValue(mod.ModDirectory, out var tempState) && tempState.IsTemporary
                ? tempState.Priority : mod.Priority;

        private bool GetEffectiveEnabled(PenumbraModInfo mod) =>
            _temporaryModStates.TryGetValue(mod.ModDirectory, out var tempState) && tempState.IsTemporary
                ? tempState.Enabled : mod.IsEnabled;

        private PenumbraApiEc SetModPriority(PenumbraModInfo mod, int newPriority) =>
            SetModSettings(mod, mod.IsEnabled, newPriority, 
                () => PenumbraManager.Instance?.SetModPriority(mod, newPriority) ?? PenumbraApiEc.UnknownError);

        private PenumbraApiEc SetModEnabled(PenumbraModInfo mod, bool enabled) =>
            SetModSettings(mod, enabled, mod.Priority, 
                () => PenumbraManager.Instance?.SetModEnabled(mod, enabled) ?? PenumbraApiEc.UnknownError);

        private PenumbraApiEc SetModToHighestPriorityForEmote(PenumbraModInfo mod, string emoteName)
        {
            var relevantMods = _allMods.Where(m => GetEffectiveEnabled(m) && 
                (m.EmoteNames.Contains(emoteName) || m.EmoteNames.Any(name => name.StartsWith("表情：")))).ToList();
            var maxPriority = relevantMods.Count > 0 ? relevantMods.Max(m => GetEffectivePriority(m)) : 0;
            var newPriority = maxPriority + 1;
            
            return SetModSettings(mod, GetEffectiveEnabled(mod), newPriority, 
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
                if (!currentCollection.HasValue) return PenumbraApiEc.CollectionMissing;
                if (_setTemporaryModSettings == null) return PenumbraApiEc.UnknownError;

                var result = _setTemporaryModSettings.Invoke(currentCollection.Value.Id, mod.ModDirectory, false,
                    enabled, priority, new Dictionary<string, IReadOnlyList<string>>(), "Brio", 0, mod.ModName ?? string.Empty);
                
                if (result == PenumbraApiEc.Success)
                    PenumbraManager.Instance?.ClearEffectiveModInfoCache();
                return result;
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"临时设置模组失败: {ex.Message}");
                return PenumbraApiEc.UnknownError;
            }
        }

        private void UpdateModInfo(string modDirectory)
        {
            try
            {
                var updateModInfoMethod = typeof(PenumbraManager).GetMethod("UpdateModInfo", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                updateModInfoMethod?.Invoke(PenumbraManager.Instance, new object[] { modDirectory });
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
                var pluginInterface = GetPluginInterface(PenumbraManager.Instance);
                if (pluginInterface == null)
                {
                    Brio.Log.Warning("无法获取PluginInterface，无法打开模组页面");
                    return;
                }

                var openMainWindow = new OpenMainWindow(pluginInterface);
                var result = openMainWindow.Invoke(TabType.Mods, mod.ModDirectory, mod.ModName);
                
                if (result == PenumbraApiEc.Success)
                    Brio.Log.Debug($"成功在Penumbra中打开模组: {mod.ModName}");
                else
                    Brio.Log.Warning($"在Penumbra中打开模组失败: {result}");
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
                var pluginInterface = GetPluginInterface(PenumbraManager.Instance);
                if (pluginInterface != null)
                {
                    var removeAllTemporaryModSettings = new RemoveAllTemporaryModSettings(pluginInterface);
                    var currentCollection = GetCurrentCollection();
                    
                    if (currentCollection.HasValue)
                    {
                        var result = removeAllTemporaryModSettings.Invoke(currentCollection.Value.Id, 0);
                        if (result == PenumbraApiEc.Success)
                        {
                            Brio.Log.Information("已清除所有临时设置");
                            _temporaryModStates.Clear();
                        }
                        else
                        {
                            Brio.Log.Warning($"清除临时设置失败: {result}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"清除临时设置失败: {ex.Message}");
            }
        }

        private IDalamudPluginInterface? GetPluginInterface(PenumbraManager? penumbraManager) =>
            typeof(PenumbraManager).GetField("_pluginInterface", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(penumbraManager) as IDalamudPluginInterface;

        private (Guid Id, string Name)? GetCurrentCollection()
        {
            var getCurrentCollectionField = typeof(PenumbraManager).GetField("_getCurrentCollection", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (getCurrentCollectionField?.GetValue(PenumbraManager.Instance) is GetCollection getCurrentCollection)
                return getCurrentCollection.Invoke(ApiCollectionType.Current);
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
                    var result = _queryTemporaryModSettings.Invoke(currentCollection.Value.Id, modDirectory, 
                        out var settings, out var source, 0, string.Empty);
                    
                    if (result == PenumbraApiEc.Success && settings.HasValue)
                        return source;
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"获取临时设置来源失败: {ex.Message}");
            }
            
            return string.Empty;
        }

        public void Cleanup()
        {
            if (PenumbraManager.Instance != null)
                PenumbraManager.Instance.ModInfoChanged -= OnPenumbraModInfoChanged;
        }

        private class ModInfoComparer : IEqualityComparer<PenumbraModInfo>
        {
            public bool Equals(PenumbraModInfo? x, PenumbraModInfo? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                
                return x.ModDirectory == y.ModDirectory && x.ModName == y.ModName &&
                       x.Priority == y.Priority && x.IsEnabled == y.IsEnabled &&
                       x.XcpFiles.Count == y.XcpFiles.Count && x.EmoteNames.Count == y.EmoteNames.Count;
            }

            public int GetHashCode(PenumbraModInfo obj) =>
                HashCode.Combine(obj.ModDirectory, obj.ModName, obj.Priority, obj.IsEnabled);
        }
    }
} 