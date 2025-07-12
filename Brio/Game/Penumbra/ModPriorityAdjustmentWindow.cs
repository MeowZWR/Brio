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
        private bool _needsRefresh = false;
        private Action? _onPriorityChanged;

        public void UpdateContent(string emoteName, List<PenumbraModInfo> mods, Action? onPriorityChanged = null)
        {
            _emoteName = emoteName;
            _allMods.Clear();
            _allMods.AddRange(mods);
            _needsRefresh = false;
            _onPriorityChanged = onPriorityChanged;
            UpdateDisplayMods();
        }

        public void DrawContent()
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(440, 240), new Vector2(900, 600));
            
            // 检查是否需要刷新
            if (_needsRefresh)
            {
                UpdateDisplayMods();
                _needsRefresh = false;
            }
            
            // 检查复选框状态变化
            if (_includeDisabledChanged)
            {
                _includeDisabledChanged = false;
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
            ImGui.Text($"情感动作: {_emoteName}  找到 {enabledCount}/{totalCount} 个相关模组");
            ImGui.SameLine();
            
            if (ImGui.Checkbox("包含未启用的模组", ref _includeDisabled))
            {
                _includeDisabledChanged = true;
            }
            
            ImGui.SameLine();
            if (ImGui.Button("刷新"))
            {
                PenumbraManager.Instance?.RefreshModInfo();
                UpdateDisplayMods();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("刷新模组信息\n获取最新的模组状态和优先级");
                ImGui.EndTooltip();
            }
            ImGui.Separator();
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
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Selectable(modName, false, ImGuiSelectableFlags.None, new Vector2(0, ImGui.GetTextLineHeight() * 0.8f)))
                OpenModPage(mod);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"模组目录：{mod.ModDirectory}");
                ImGui.Text("点击以在 Penumbra 中打开模组页面。");
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
            if (ImGui.InputInt($"##priority_{mod.ModDirectory}", ref priorityInput, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (priorityInput != priority)
                {
                    var result = PenumbraManager.Instance?.SetModPriority(mod, priorityInput);
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
            ImGui.TableNextColumn();
            colWidth = ImGui.GetColumnWidth();
            float buttonSizeX = ImGui.GetFrameHeight() * 0.8f;
            float buttonPosX = ImGui.GetCursorPosX() + (colWidth - buttonSizeX) / 2f;
            ImGui.SetCursorPosX(buttonPosX);
            var enabledMods = _displayMods.Where(m => m.IsEnabled).ToList();
            var maxPriority = enabledMods.Count > 0 ? enabledMods.Max(m => m.Priority) : 0;
            var isHighestPriority = isEnabled && mod.Priority >= maxPriority;
            ImGui.BeginDisabled(isHighestPriority || !isEnabled);
            if (ImGui.Button($"▲##up_{mod.ModDirectory}", new Vector2(buttonSizeX, 0)))
            {
                var result = PenumbraManager.Instance?.SetModToHighestPriority(mod);
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
                if (!isEnabled)
                    ImGui.Text($"模组 '{modName}' 未启用，无法设置优先级");
                else if (isHighestPriority)
                    ImGui.Text($"模组 '{modName}' 已经是最高优先级");
                else
                    ImGui.Text($"将模组 '{modName}' 设置为最高优先级 ({maxPriority + 1})");
                ImGui.EndTooltip();
            }
            ImGui.TableNextColumn();
            colWidth = ImGui.GetColumnWidth();
            float dotWidth = ImGui.CalcTextSize("●").X;
            float crossWidth = ImGui.CalcTextSize("×").X;
            float btnWidth = Math.Max(dotWidth, crossWidth) + ImGui.GetStyle().FramePadding.X * 2f;
            textWidth = btnWidth;
            float statusPosX = ImGui.GetCursorPosX() + (colWidth - btnWidth) / 2f;
            ImGui.SetCursorPosX(statusPosX);
            var statusText = isEnabled ? "●" : "×";
            var statusColor = isEnabled ? new Vector4(0.2f, 0.8f, 0.2f, 1.0f) : new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.Text, statusColor);
            if (ImGui.Button($"{statusText}##toggle_{mod.ModDirectory}", new Vector2(btnWidth, 0)))
            {
                var result = PenumbraManager.Instance?.SetModEnabled(mod, !isEnabled);
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
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(isEnabled ? "点击禁用模组" : "点击启用模组");
                ImGui.EndTooltip();
            }
        }

        private void UpdateDisplayMods()
        {
            // 检查模组列表是否有变化，如果有变化则重新计算缓存
            bool modsChanged = !_allModsCached.SequenceEqual(_allMods, new ModInfoComparer());
            
            if (modsChanged)
            {
                _allModsCached.Clear();
                _allModsCached.AddRange(_allMods);
                
                // 重新计算启用的模组缓存
                _enabledMods.Clear();
                _enabledMods.AddRange(_allMods.Where(m => m.IsEnabled));
                _enabledMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            
            // 根据当前状态选择显示列表
            _displayMods.Clear();
            if (_includeDisabled)
            {
                _displayMods.AddRange(_allModsCached);
                _displayMods.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            else
            {
                _displayMods.AddRange(_enabledMods);
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
                var pluginInterfaceField = typeof(PenumbraManager).GetField("_pluginInterface", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pluginInterfaceField?.GetValue(penumbraManager) is IDalamudPluginInterface pluginInterface)
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

        // 用于比较模组信息是否相同的比较器
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