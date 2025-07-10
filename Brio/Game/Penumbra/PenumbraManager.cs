using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Brio.Core;
using Dalamud.Plugin;
using Penumbra.Api.IpcSubscribers;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Brio.Game.Penumbra
{
    public class PenumbraManager
    {
        public static PenumbraManager? Instance { get; private set; }

        private readonly List<PenumbraModInfo> _modInfos = new();
        private readonly IDalamudPluginInterface _pluginInterface = null!;
        
        // 事件监听
        private readonly EventSubscriber _penumbraInitialized = null!;
        private readonly EventSubscriber _penumbraDisposed = null!;
        private readonly EventSubscriber<string> _modAdded = null!;
        private readonly EventSubscriber<string> _modDeleted = null!;
        private readonly EventSubscriber<string, string> _modMoved = null!;
        private readonly EventSubscriber<ModSettingChange, Guid, string, bool> _modSettingChanged = null!;
        
        // 状态跟踪
        public bool HasModChangesSinceLastRefresh { get; private set; } = false;
        public bool HasEverRefreshed { get; private set; } = false;

        private readonly GetModList _getModList = null!;
        private readonly GetChangedItems _getChangedItems = null!;
        private readonly GetChangedItemAdapterDictionary _getChangedItemAdapterDictionary = null!;
        private readonly GetCurrentModSettings _getCurrentModSettings = null!;
        private readonly GetModDirectory _getModDirectory = null!;

        public PenumbraManager(DalamudServices services)
        {
            try
            {
                Brio.Log.Information("开始初始化PenumbraManager...");
                _pluginInterface = services.PluginInterface;
                
                Brio.Log.Information("创建Penumbra IPC订阅...");
                _getModList = new GetModList(_pluginInterface);
                _getChangedItems = new GetChangedItems(_pluginInterface);
                _getChangedItemAdapterDictionary = new GetChangedItemAdapterDictionary(_pluginInterface);
                _getCurrentModSettings = new GetCurrentModSettings(_pluginInterface);
                _getModDirectory = new GetModDirectory(_pluginInterface);

                _penumbraInitialized = Initialized.Subscriber(_pluginInterface, OnPenumbraInitialized);
                _penumbraDisposed = Disposed.Subscriber(_pluginInterface, OnPenumbraDisposed);
                _modAdded = ModAdded.Subscriber(_pluginInterface, OnModAdded);
                _modDeleted = ModDeleted.Subscriber(_pluginInterface, OnModDeleted);
                _modMoved = ModMoved.Subscriber(_pluginInterface, OnModMoved);
                _modSettingChanged = ModSettingChanged.Subscriber(_pluginInterface, OnModSettingChanged);
                
                Instance = this;
                Brio.Log.Information("PenumbraManager初始化完成。");
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"PenumbraManager初始化失败: {ex.Message}");
                Instance = this;
            }
        }
        
        public bool IsPenumbraAvailable()
        {
            try
            {
                var mods = _getModList.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"Penumbra连接失败: {ex.Message}");
                return false;
            }
        }



        public PenumbraModInfo? GetModInfoForEmote(string emoteName) 
        {
            return _modInfos.FirstOrDefault(m => m.EmoteName == emoteName);
        }
        
        public List<string> GetXcpFilesForEmote(string emoteName)
        {
            var modInfo = GetModInfoForEmote(emoteName);
            return modInfo?.XcpFiles ?? new List<string>();
        }

        public string GetModRootDirectory()
        {
            return _getModDirectory.Invoke();
        }

        private void OnPenumbraInitialized()
        {
#if DEBUG
            Brio.Log.Information("Penumbra已初始化");
#endif
            HasModChangesSinceLastRefresh = false;
        }

        private void OnPenumbraDisposed()
        {
#if DEBUG
            Brio.Log.Information("Penumbra已销毁，清空模组信息...");
#endif
            _modInfos.Clear();
            HasModChangesSinceLastRefresh = false;
        }

        private void OnModAdded(string modDirectory)
        {
#if DEBUG
            Brio.Log.Information($"模组已添加: {modDirectory}");
#endif
            HasModChangesSinceLastRefresh = true;
        }

        private void OnModDeleted(string modDirectory)
        {
#if DEBUG
            Brio.Log.Information($"模组已删除: {modDirectory}");
#endif
            HasModChangesSinceLastRefresh = true;
        }

        private void OnModMoved(string oldDirectory, string newDirectory)
        {
#if DEBUG
            Brio.Log.Information($"模组已移动: {oldDirectory} -> {newDirectory}");
#endif
            HasModChangesSinceLastRefresh = true;
        }

        private void OnModSettingChanged(ModSettingChange changeType, Guid collectionId, string modDirectory, bool inherited)
        {
#if DEBUG
            Brio.Log.Information($"模组设置已更改: {changeType}, 模组: {modDirectory}, 继承: {inherited}");
#endif
            // 只有当优先级或启用状态发生变化时才标记
            if (changeType == ModSettingChange.Priority || changeType == ModSettingChange.EnableState)
            {
                HasModChangesSinceLastRefresh = true;
            }
        }

        // 手动刷新
        public void RefreshModInfo()
        {
            Brio.Log.Information("开始刷新Penumbra模组信息...");
            _modInfos.Clear();
            try
            {
                var mods = _getModList.Invoke();
                var conflicts = _getChangedItemAdapterDictionary.Invoke();
                
                var collections = new GetCollections(_pluginInterface).Invoke();
                var currentCollectionId = collections.FirstOrDefault().Key;
                
                Brio.Log.Information($"开始刷新Penumbra模组信息，检测到 {mods.Count} 个模组");
                
                foreach (var mod in mods)
                {
                    var modInfo = new PenumbraModInfo
                    {
                        ModName = mod.Value,
                        Priority = 0,
                        ModDirectory = mod.Key
                    };

                    var changes = _getChangedItems.Invoke(mod.Key, mod.Value);
                    if (changes != null && changes.Any(c => c.Key.StartsWith("Emote:")))
                    {
                        var emoteChange = changes.First(c => c.Key.StartsWith("Emote:"));
                        var emoteText = emoteChange.Key.Replace("Emote:", "").Trim();
                        var emoteName = System.Text.RegularExpressions.Regex.Replace(emoteText, @"\s*\(\d+\)$", "").Trim();
                        modInfo.EmoteName = emoteName;
#if DEBUG
                        Brio.Log.Information($"模组 '{mod.Value}' 修改情感动作: {modInfo.EmoteName}");
#endif
                    }

                    if (currentCollectionId != Guid.Empty)
                    {
                        var (settingsEc, settings) = _getCurrentModSettings.Invoke(currentCollectionId, mod.Key, mod.Value, false);
                        if (settingsEc == PenumbraApiEc.Success && settings.HasValue)
                        {
                            modInfo.Priority = settings.Value.Item2;
                            modInfo.IsEnabled = settings.Value.Item1; // 新增：记录启用状态
#if DEBUG
                            Brio.Log.Information($"模组 '{mod.Value}' 优先级: {modInfo.Priority} 启用: {modInfo.IsEnabled}");
#endif
                        }
                    }

                    try
                    {
                        var modRootDirectory = _getModDirectory.Invoke();
                        var actualModPath = Path.Combine(modRootDirectory, mod.Key);
                        var xcpFolderPath = Path.Combine(actualModPath, "XCP");
                        
#if DEBUG
                        Brio.Log.Information($"模组 '{mod.Value}' 路径: {actualModPath}");
#endif
                        
                        if (Directory.Exists(xcpFolderPath))
                        {
                            var xcpFiles = Directory.GetFiles(xcpFolderPath, "*.xcp");
                            modInfo.XcpFiles.AddRange(xcpFiles);
#if DEBUG
                            Brio.Log.Information($"模组 '{mod.Value}' 找到 {modInfo.XcpFiles.Count} 个XCP文件");
#endif
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Brio.Log.Warning($"模组 '{mod.Value}' 获取路径失败: {ex.Message}");
#endif
                    }

                    _modInfos.Add(modInfo);
                }
                
                // 只保留启用的mod再排序
                _modInfos.RemoveAll(m => !m.IsEnabled);
                _modInfos.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                
#if DEBUG
                var topMods = _modInfos.Take(5).ToList();
                Brio.Log.Information("优先级最高的5个模组:");
                foreach (var mod in topMods)
                {
                    Brio.Log.Information($"  - '{mod.ModName}' (优先级: {mod.Priority}, 情感动作: {mod.EmoteName}, XCP文件数: {mod.XcpFiles.Count})");
                }
#endif
                
                HasModChangesSinceLastRefresh = false;
                HasEverRefreshed = true;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"刷新模组信息失败: {ex.Message}");
            }
        }
    }
} 