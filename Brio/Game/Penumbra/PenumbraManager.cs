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
    public class PenumbraManager : IDisposable
    {
        public static PenumbraManager? Instance { get; private set; }
        
        private readonly List<PenumbraModInfo> _modInfos = new();
        private readonly IDalamudPluginInterface _pluginInterface;
        private readonly Dictionary<string, PenumbraModInfo?> _effectiveModInfoCache = new();
        private IReadOnlyList<(string ModDirectory, IReadOnlyDictionary<string, object?> ChangedItems)>? _changedItems;
        
        // IPC 订阅者
        private readonly EventSubscriber _penumbraInitialized;
        private readonly EventSubscriber _penumbraDisposed;
        private readonly EventSubscriber<string> _modAdded;
        private readonly EventSubscriber<string> _modDeleted;
        private readonly EventSubscriber<string, string> _modMoved;
        private readonly EventSubscriber<ModSettingChange, Guid, string, bool> _modSettingChanged;
        private readonly GetModList _getModList;
        private readonly GetChangedItems _getChangedItems;
        private readonly GetChangedItemAdapterList _getChangedItemAdapterList;
        private readonly GetCurrentModSettings _getCurrentModSettings;
        private readonly GetModDirectory _getModDirectory;
        private readonly GetAllModSettings _getAllSettings;
        private readonly TrySetModPriority _setModPriority;
        private readonly TrySetMod _setModEnabled;
        private readonly GetCollections _getCollections;
        private readonly GetCollection _getCurrentCollection;
        
        public event Action? ModInfoChanged;
        public bool HasModChangesSinceLastRefresh { get; private set; } = false;
        public bool HasEverRefreshed { get; private set; } = false;

        public PenumbraManager(DalamudServices services)
        {
            try
            {
                _pluginInterface = services.PluginInterface;
                
                // 初始化所有IPC订阅者
                _getModList = new GetModList(_pluginInterface);
                _getChangedItems = new GetChangedItems(_pluginInterface);
                _getChangedItemAdapterList = new GetChangedItemAdapterList(_pluginInterface);
                _getCurrentModSettings = new GetCurrentModSettings(_pluginInterface);
                _getModDirectory = new GetModDirectory(_pluginInterface);
                _getAllSettings = new GetAllModSettings(_pluginInterface);
                _setModPriority = new TrySetModPriority(_pluginInterface);
                _setModEnabled = new TrySetMod(_pluginInterface);
                _getCollections = new GetCollections(_pluginInterface);
                _getCurrentCollection = new GetCollection(_pluginInterface);
                
                // 事件订阅
                _penumbraInitialized = Initialized.Subscriber(_pluginInterface, OnPenumbraInitialized);
                _penumbraDisposed = Disposed.Subscriber(_pluginInterface, OnPenumbraDisposed);
                _modAdded = ModAdded.Subscriber(_pluginInterface, OnModAdded);
                _modDeleted = ModDeleted.Subscriber(_pluginInterface, OnModDeleted);
                _modMoved = ModMoved.Subscriber(_pluginInterface, OnModMoved);
                _modSettingChanged = ModSettingChanged.Subscriber(_pluginInterface, OnModSettingChanged);
                
                Instance = this;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"PenumbraManager初始化失败: {ex.Message}");
                Instance = this;
            }
        }

        public bool IsPenumbraAvailable()
        {
            try { _getModList.Invoke(); return true; }
            catch { return false; }
        }

        public PenumbraModInfo? GetModInfoForEmote(string emoteName) => 
            _modInfos.Where(m => m.IsEnabled && m.EmoteNames.Any(e => e == emoteName))
                     .OrderByDescending(m => m.Priority).FirstOrDefault();

        public List<string> GetXcpFilesForEmote(string emoteName) => 
            GetModInfoForEmote(emoteName)?.XcpFiles ?? new List<string>();

        public string GetModRootDirectory() => _getModDirectory.Invoke();

        public List<PenumbraModInfo> GetModsForEmote(string emoteName) => 
            _modInfos.Where(m => m.EmoteNames.Contains(emoteName)).ToList();

        public List<PenumbraModInfo> GetAllMods() => _modInfos.ToList();

        public List<PenumbraModInfo> GetAllModsWithEmoteConflicts(string currentEmoteName) =>
            _modInfos.Where(m => m.EmoteNames.Any(name => name.StartsWith("表情：")) && 
                               !m.EmoteNames.Contains(currentEmoteName)).ToList();

        public PenumbraApiEc SetModToHighestPriority(PenumbraModInfo mod) =>
            SetModToHighestPriorityInternal(mod, GetMaxPriorityFromAllMods);

        public PenumbraApiEc SetModToHighestPriorityForEmote(PenumbraModInfo mod, string emoteName) =>
            SetModToHighestPriorityInternal(mod, () => GetMaxPriorityForEmote(emoteName));

        public PenumbraApiEc SetModPriority(PenumbraModInfo mod, int newPriority) =>
            ExecuteModOperation(() => {
                var currentCollection = GetCurrentCollectionId();
                if (!currentCollection.HasValue) return PenumbraApiEc.CollectionMissing;
                
                var result = _setModPriority.Invoke(currentCollection.Value, mod.ModDirectory, newPriority, mod.ModName);
                if (result == PenumbraApiEc.Success) UpdateModInfo(mod.ModDirectory);
                return result;
            });

        public PenumbraApiEc SetModEnabled(PenumbraModInfo mod, bool enabled) =>
            ExecuteModOperation(() => {
                var currentCollection = GetCurrentCollectionId();
                if (!currentCollection.HasValue) return PenumbraApiEc.CollectionMissing;
                
                var result = _setModEnabled.Invoke(currentCollection.Value, mod.ModDirectory, enabled, mod.ModName);
                if (result == PenumbraApiEc.Success) UpdateModInfo(mod.ModDirectory);
                return result;
            });

        public PenumbraModInfo? GetEffectiveModInfoForEmote(string emoteName)
        {
            if (_effectiveModInfoCache.TryGetValue(emoteName, out var cached) && !HasModChangesSinceLastRefresh)
            {
                Brio.Log.Debug($"使用缓存的生效模组信息 '{emoteName}': {cached?.ModName ?? "null"}");
                return cached;
            }

            Brio.Log.Debug($"重新计算情感动作 '{emoteName}' 的生效模组");
            var mods = GetModsForEmote(emoteName);
            if (mods.Count == 0)
            {
                _effectiveModInfoCache[emoteName] = null;
                Brio.Log.Debug($"未找到情感动作 '{emoteName}' 的相关模组");
                return null;
            }

            Brio.Log.Debug($"找到 {mods.Count} 个相关模组，开始查询临时设置");
            var effectiveMods = GetEffectiveModStates(mods);
            var enabledMods = effectiveMods.Where(m => m.IsEnabled).ToList();
            var resultMod = enabledMods.OrderByDescending(m => m.Priority).FirstOrDefault();
            
            if (resultMod != null)
                Brio.Log.Debug($"最终选择的生效模组: '{resultMod.ModName}' (优先级: {resultMod.Priority})");
            else
                Brio.Log.Debug($"情感动作 '{emoteName}' 没有启用的模组");
            
            _effectiveModInfoCache[emoteName] = resultMod;
            HasModChangesSinceLastRefresh = false;
            return resultMod;
        }

        public void RefreshModInfo() 
        { 
            InitializeModInfo(); 
            ClearEffectiveModInfoCache(); 
        }

        public void ClearEffectiveModInfoCache()
        {
            Brio.Log.Debug("清除生效模组信息缓存，触发ModInfoChanged事件");
            _effectiveModInfoCache.Clear();
            ModInfoChanged?.Invoke();
        }

        // 事件处理
        private void OnPenumbraInitialized() 
        { 
            HasModChangesSinceLastRefresh = false; 
            InitializeModInfo(); 
            ClearEffectiveModInfoCache(); 
        }

        private void OnPenumbraDisposed() 
        { 
            _modInfos.Clear(); 
            _changedItems = null; 
            HasModChangesSinceLastRefresh = false; 
            ClearEffectiveModInfoCache(); 
        }

        private void OnModAdded(string modDirectory) => MarkModInfoChanged();
        private void OnModDeleted(string modDirectory) => MarkModInfoChanged();
        private void OnModMoved(string oldDirectory, string newDirectory) => MarkModInfoChanged();

        private void OnModSettingChanged(ModSettingChange changeType, Guid collectionId, string modDirectory, bool inherited)
        {
            switch (changeType)
            {
                case ModSettingChange.Priority:
                case ModSettingChange.EnableState:
                    UpdateModInfo(modDirectory);
                    ClearEffectiveModInfoCache();
                    break;
                case ModSettingChange.TemporaryMod:
                case ModSettingChange.TemporarySetting:
                    Brio.Log.Debug($"检测到临时设置变更: {changeType} for {modDirectory}");
                    ClearEffectiveModInfoCache();
                    break;
                default:
                    MarkModInfoChanged();
                    break;
            }
        }

        // 私有辅助方法
        private void MarkModInfoChanged()
        {
            HasModChangesSinceLastRefresh = true;
            ClearEffectiveModInfoCache();
        }

        private void InitializeModInfo()
        {
            _modInfos.Clear();
            ClearEffectiveModInfoCache();
            
            try
            {
                var mods = _getModList.Invoke();
                var currentCollectionId = GetCurrentCollectionId();
                if (!currentCollectionId.HasValue) return;

                InitializeChangedItems();
                var allSettings = GetAllModSettings(currentCollectionId.Value);

                foreach (var mod in mods)
                {
                    var modInfo = CreateModInfo(mod, allSettings, currentCollectionId.Value);
                    _modInfos.Add(modInfo);
                }

                _modInfos.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                HasModChangesSinceLastRefresh = false;
                HasEverRefreshed = true;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"初始化模组信息失败: {ex.Message}");
            }
        }

        private PenumbraModInfo CreateModInfo(KeyValuePair<string, string> mod, 
            Dictionary<string, (bool, int)>? allSettings, Guid currentCollectionId)
        {
            var modInfo = new PenumbraModInfo { ModName = mod.Value, ModDirectory = mod.Key };

            // 设置优先级和启用状态
            if (allSettings?.TryGetValue(mod.Key, out var settings) == true)
            {
                modInfo.Priority = settings.Item2;
                modInfo.IsEnabled = settings.Item1;
            }
            else
            {
                var (ec, currentSettings) = _getCurrentModSettings.Invoke(currentCollectionId, mod.Key, mod.Value, false);
                if (ec == PenumbraApiEc.Success && currentSettings.HasValue)
                {
                    modInfo.Priority = currentSettings.Value.Item2;
                    modInfo.IsEnabled = currentSettings.Value.Item1;
                }
            }

            // 处理情感动作名称
            var changes = GetChangedItemsForMod(mod.Key, mod.Value);
            if (changes?.Any(c => c.Key.StartsWith("Emote:")) == true)
            {
                foreach (var emoteChange in changes.Where(c => c.Key.StartsWith("Emote:")))
                {
                    var emoteName = System.Text.RegularExpressions.Regex
                        .Replace(emoteChange.Key.Replace("Emote:", "").Trim(), @"\s*\(\d+\)$", "").Trim();
                    if (!modInfo.EmoteNames.Contains(emoteName))
                        modInfo.EmoteNames.Add(emoteName);
                }
            }

            // 添加XCP文件
            AddXcpFiles(modInfo, mod.Key);
            
            return modInfo;
        }

        private void AddXcpFiles(PenumbraModInfo modInfo, string modDirectory)
        {
            try
            {
                var modRootDirectory = _getModDirectory.Invoke();
                var xcpFolderPath = Path.Combine(modRootDirectory, modDirectory, "XCP");
                if (Directory.Exists(xcpFolderPath))
                    modInfo.XcpFiles.AddRange(Directory.GetFiles(xcpFolderPath, "*.xcp"));
            }
            catch { /* 忽略文件系统错误 */ }
        }

        private Dictionary<string, (bool, int)>? GetAllModSettings(Guid currentCollectionId)
        {
            var allSettings = _getAllSettings.Invoke(currentCollectionId, false, false, 0);
            return allSettings.Item1 == PenumbraApiEc.Success 
                ? allSettings.Item2?.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value.Item1, kvp.Value.Item2))
                : null;
        }

        private List<PenumbraModInfo> GetEffectiveModStates(List<PenumbraModInfo> mods)
        {
            var queryTemp = new QueryTemporaryModSettings(_pluginInterface);
            var currentCollection = GetCurrentCollectionId();
            if (!currentCollection.HasValue) 
            {
                Brio.Log.Warning("无法获取当前集合ID，使用原始模组状态");
                return mods;
            }

            Brio.Log.Debug($"查询临时设置，集合ID: {currentCollection.Value}");
            var effectiveMods = new List<PenumbraModInfo>();
            foreach (var mod in mods)
            {
                var result = queryTemp.Invoke(currentCollection.Value, mod.ModDirectory, 
                    out var settings, out var source, 0, mod.ModName);
                
                if (result == PenumbraApiEc.Success && settings.HasValue)
                {
                    Brio.Log.Debug($"模组 '{mod.ModName}' 发现临时设置: 启用={settings.Value.Item2}, 优先级={settings.Value.Item3}, 来源={source}");
                    effectiveMods.Add(new PenumbraModInfo
                    {
                        ModName = mod.ModName,
                        ModDirectory = mod.ModDirectory,
                        EmoteNames = mod.EmoteNames,
                        XcpFiles = mod.XcpFiles,
                        IsEnabled = settings.Value.Item2,
                        Priority = settings.Value.Item3
                    });
                }
                else
                {
                    if (result != PenumbraApiEc.Success)
                        Brio.Log.Debug($"模组 '{mod.ModName}' 临时设置查询失败: {result}");
                    effectiveMods.Add(mod);
                }
            }
            return effectiveMods;
        }

        private PenumbraApiEc SetModToHighestPriorityInternal(PenumbraModInfo mod, Func<int> getMaxPriority) =>
            ExecuteModOperation(() => {
                var currentCollection = GetCurrentCollectionId();
                if (!currentCollection.HasValue) return PenumbraApiEc.CollectionMissing;
                
                var maxPriority = getMaxPriority();
                var result = _setModPriority.Invoke(currentCollection.Value, mod.ModDirectory, maxPriority + 1, mod.ModName);
                if (result == PenumbraApiEc.Success) UpdateModInfo(mod.ModDirectory);
                return result;
            });

        private int GetMaxPriorityFromAllMods()
        {
            var currentCollectionId = GetCurrentCollectionId();
            if (!currentCollectionId.HasValue) return 0;
            
            var allSettings = _getAllSettings.Invoke(currentCollectionId.Value, false, false, 0);
            if (allSettings.Item1 != PenumbraApiEc.Success) return 0;
            
            return allSettings.Item2?.Where(kvp => kvp.Value.Item1).Max(kvp => kvp.Value.Item2) ?? 0;
        }

        private int GetMaxPriorityForEmote(string emoteName)
        {
            var relevantMods = _modInfos.Where(m => m.IsEnabled && 
                (m.EmoteNames.Contains(emoteName) || m.EmoteNames.Any(name => name.StartsWith("表情：")))).ToList();
            return relevantMods.Count > 0 ? relevantMods.Max(m => m.Priority) : 0;
        }

        private PenumbraApiEc ExecuteModOperation(Func<PenumbraApiEc> operation)
        {
            try { return operation(); }
            catch { return PenumbraApiEc.UnknownError; }
        }

        private Guid? GetCurrentCollectionId()
        {
            var currentCollection = _getCurrentCollection.Invoke(ApiCollectionType.Current);
            return currentCollection?.Id;
        }

        private void InitializeChangedItems()
        {
            try { _changedItems = _getChangedItemAdapterList.Invoke(); }
            catch { _changedItems = null; }
        }

        private IReadOnlyDictionary<string, object?>? GetChangedItemsForMod(string modDirectory, string modName) =>
            _changedItems?.FirstOrDefault(x => x.ModDirectory == modDirectory).ChangedItems 
            ?? _getChangedItems.Invoke(modDirectory, modName);

        private void UpdateModInfo(string modDirectory)
        {
            var mod = _modInfos.FirstOrDefault(m => m.ModDirectory == modDirectory);
            if (mod == null) return;

            var currentCollectionId = GetCurrentCollectionId();
            if (!currentCollectionId.HasValue) return;

            var (ec, settings) = _getCurrentModSettings.Invoke(currentCollectionId.Value, modDirectory, mod.ModName, false);
            if (ec == PenumbraApiEc.Success && settings.HasValue)
            {
                mod.Priority = settings.Value.Item2;
                mod.IsEnabled = settings.Value.Item1;
                _modInfos.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        public void Dispose()
        {
            _penumbraInitialized?.Dispose();
            _penumbraDisposed?.Dispose();
            _modAdded?.Dispose();
            _modDeleted?.Dispose();
            _modMoved?.Dispose();
            _modSettingChanged?.Dispose();
        }
    }
} 