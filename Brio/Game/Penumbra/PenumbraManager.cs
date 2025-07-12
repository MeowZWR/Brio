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
        private readonly IDalamudPluginInterface _pluginInterface = null!;
        private readonly EventSubscriber _penumbraInitialized = null!;
        private readonly EventSubscriber _penumbraDisposed = null!;
        private readonly EventSubscriber<string> _modAdded = null!;
        private readonly EventSubscriber<string> _modDeleted = null!;
        private readonly EventSubscriber<string, string> _modMoved = null!;
        private readonly EventSubscriber<ModSettingChange, Guid, string, bool> _modSettingChanged = null!;
        private readonly GetModList _getModList = null!;
        private readonly GetChangedItems _getChangedItems = null!;
        private readonly GetChangedItemAdapterDictionary _getChangedItemAdapterDictionary = null!;
        private readonly GetCurrentModSettings _getCurrentModSettings = null!;
        private readonly GetModDirectory _getModDirectory = null!;
        private readonly GetAllModSettings _getAllSettings = null!;
        private readonly GetCurrentModSettingsWithTemp _getCurrentSettingsWithTemp = null!;
        private readonly GetChangedItemAdapterList _getChangedItemAdapterList = null!;
        private readonly TrySetModPriority _setModPriority = null!;
        private readonly TrySetMod _setModEnabled = null!;
        private readonly GetCollections _getCollections = null!;
        private readonly GetCollection _getCurrentCollection = null!;
        private IReadOnlyList<(string ModDirectory, IReadOnlyDictionary<string, object?> ChangedItems)>? _changedItems;

        public PenumbraManager(DalamudServices services)
        {
            try
            {
                _pluginInterface = services.PluginInterface;
                _getModList = new GetModList(_pluginInterface);
                _getChangedItems = new GetChangedItems(_pluginInterface);
                _getChangedItemAdapterDictionary = new GetChangedItemAdapterDictionary(_pluginInterface);
                _getCurrentModSettings = new GetCurrentModSettings(_pluginInterface);
                _getModDirectory = new GetModDirectory(_pluginInterface);
                _getAllSettings = new GetAllModSettings(_pluginInterface);
                _getCurrentSettingsWithTemp = new GetCurrentModSettingsWithTemp(_pluginInterface);
                _getChangedItemAdapterList = new GetChangedItemAdapterList(_pluginInterface);
                _setModPriority = new TrySetModPriority(_pluginInterface);
                _setModEnabled = new TrySetMod(_pluginInterface);
                _getCollections = new GetCollections(_pluginInterface);
                _getCurrentCollection = new GetCollection(_pluginInterface);
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
        public PenumbraModInfo? GetModInfoForEmote(string emoteName) => _modInfos.Where(m => m.IsEnabled && m.EmoteNames.Any(e => e == emoteName)).OrderByDescending(m => m.Priority).FirstOrDefault();
        public List<string> GetXcpFilesForEmote(string emoteName) => GetModInfoForEmote(emoteName)?.XcpFiles ?? new List<string>();
        public string GetModRootDirectory() => _getModDirectory.Invoke();
        public List<PenumbraModInfo> GetModsForEmote(string emoteName) => _modInfos.Where(m => m.EmoteNames.Contains(emoteName)).ToList();
        public PenumbraApiEc SetModToHighestPriority(PenumbraModInfo mod)
        {
            try
            {
                var currentCollection = _getCurrentCollection.Invoke(ApiCollectionType.Current);
                if (!currentCollection.HasValue) return PenumbraApiEc.CollectionMissing;
                var currentCollectionId = currentCollection.Value.Id;
                var allSettings = _getAllSettings.Invoke(currentCollectionId, false, false, 0);
                if (allSettings.Item1 != PenumbraApiEc.Success) return allSettings.Item1;
                var enabledMods = allSettings.Item2!.Where(kvp => kvp.Value.Item1).ToList();
                var maxPriority = enabledMods.Count > 0 ? enabledMods.Max(kvp => kvp.Value.Item2) : 0;
                var result = _setModPriority.Invoke(currentCollectionId, mod.ModDirectory, maxPriority + 1, mod.ModName);
                if (result == PenumbraApiEc.Success) UpdateModInfo(mod.ModDirectory);
                return result;
            }
            catch { return PenumbraApiEc.UnknownError; }
        }
        public PenumbraApiEc SetModPriority(PenumbraModInfo mod, int newPriority)
        {
            try
            {
                var currentCollection = _getCurrentCollection.Invoke(ApiCollectionType.Current);
                if (!currentCollection.HasValue) return PenumbraApiEc.CollectionMissing;
                var currentCollectionId = currentCollection.Value.Id;
                var result = _setModPriority.Invoke(currentCollectionId, mod.ModDirectory, newPriority, mod.ModName);
                if (result == PenumbraApiEc.Success) UpdateModInfo(mod.ModDirectory);
                return result;
            }
            catch { return PenumbraApiEc.UnknownError; }
        }
        public PenumbraApiEc SetModEnabled(PenumbraModInfo mod, bool enabled)
        {
            try
            {
                var currentCollection = _getCurrentCollection.Invoke(ApiCollectionType.Current);
                if (!currentCollection.HasValue) return PenumbraApiEc.CollectionMissing;
                var currentCollectionId = currentCollection.Value.Id;
                var result = _setModEnabled.Invoke(currentCollectionId, mod.ModDirectory, enabled, mod.ModName);
                if (result == PenumbraApiEc.Success) UpdateModInfo(mod.ModDirectory);
                return result;
            }
            catch { return PenumbraApiEc.UnknownError; }
        }
        private void OnPenumbraInitialized() { HasModChangesSinceLastRefresh = false; InitializeModInfo(); }
        private void OnPenumbraDisposed() { _modInfos.Clear(); _changedItems = null; HasModChangesSinceLastRefresh = false; }
        private void OnModAdded(string modDirectory) { HasModChangesSinceLastRefresh = true; }
        private void OnModDeleted(string modDirectory) { HasModChangesSinceLastRefresh = true; }
        private void OnModMoved(string oldDirectory, string newDirectory) { HasModChangesSinceLastRefresh = true; }
        private void OnModSettingChanged(ModSettingChange changeType, Guid collectionId, string modDirectory, bool inherited)
        {
            switch (changeType)
            {
                case ModSettingChange.Priority:
                case ModSettingChange.EnableState:
                    UpdateModInfo(modDirectory);
                    break;
                case ModSettingChange.Edited:
                    HasModChangesSinceLastRefresh = true;
                    break;
                default:
                    HasModChangesSinceLastRefresh = true;
                    break;
            }
        }
        private void InitializeModInfo()
        {
            _modInfos.Clear();
            try
            {
                var mods = _getModList.Invoke();
                var collections = _getCollections.Invoke();
                var currentCollectionId = collections.FirstOrDefault().Key;
                InitializeChangedItems();
                if (_getAllSettings != null && currentCollectionId != Guid.Empty)
                {
                    var allSettings = _getAllSettings.Invoke(currentCollectionId, false, false, 0);
                    if (allSettings.Item1 is PenumbraApiEc.Success)
                    {
                        foreach (var mod in mods)
                        {
                            var modInfo = new PenumbraModInfo { ModName = mod.Value, ModDirectory = mod.Key };
                            if (allSettings.Item2!.TryGetValue(mod.Key, out var settings))
                            {
                                modInfo.Priority = settings.Item2;
                                modInfo.IsEnabled = settings.Item1;
                            }
                            var changes = GetChangedItemsForMod(mod.Key, mod.Value);
                            if (changes != null && changes.Any(c => c.Key.StartsWith("Emote:")))
                            {
                                foreach (var emoteChange in changes.Where(c => c.Key.StartsWith("Emote:")))
                                {
                                    var emoteText = emoteChange.Key.Replace("Emote:", "").Trim();
                                    var emoteName = System.Text.RegularExpressions.Regex.Replace(emoteText, @"\s*\(\d+\)$", "").Trim();
                                    if (!modInfo.EmoteNames.Contains(emoteName))
                                        modInfo.EmoteNames.Add(emoteName);
                                }
                            }
                            try
                            {
                                var modRootDirectory = _getModDirectory.Invoke();
                                var actualModPath = Path.Combine(modRootDirectory, mod.Key);
                                var xcpFolderPath = Path.Combine(actualModPath, "XCP");
                                if (Directory.Exists(xcpFolderPath))
                                    modInfo.XcpFiles.AddRange(Directory.GetFiles(xcpFolderPath, "*.xcp"));
                            }
                            catch { }
                            _modInfos.Add(modInfo);
                        }
                    }
                }
                else
                {
                    foreach (var mod in mods)
                    {
                        var modInfo = new PenumbraModInfo { ModName = mod.Value, Priority = 0, ModDirectory = mod.Key };
                        var changes = _getChangedItems.Invoke(mod.Key, mod.Value);
                        if (changes != null && changes.Any(c => c.Key.StartsWith("Emote:")))
                        {
                            foreach (var emoteChange in changes.Where(c => c.Key.StartsWith("Emote:")))
                            {
                                var emoteText = emoteChange.Key.Replace("Emote:", "").Trim();
                                var emoteName = System.Text.RegularExpressions.Regex.Replace(emoteText, @"\s*\(\d+\)$", "").Trim();
                                if (!modInfo.EmoteNames.Contains(emoteName))
                                    modInfo.EmoteNames.Add(emoteName);
                            }
                        }
                        if (currentCollectionId != Guid.Empty)
                        {
                            var (settingsEc, settings) = _getCurrentModSettings.Invoke(currentCollectionId, mod.Key, mod.Value, false);
                            if (settingsEc == PenumbraApiEc.Success && settings.HasValue)
                            {
                                modInfo.Priority = settings.Value.Item2;
                                modInfo.IsEnabled = settings.Value.Item1;
                            }
                        }
                        try
                        {
                            var modRootDirectory = _getModDirectory.Invoke();
                            var actualModPath = Path.Combine(modRootDirectory, mod.Key);
                            var xcpFolderPath = Path.Combine(actualModPath, "XCP");
                            if (Directory.Exists(xcpFolderPath))
                                modInfo.XcpFiles.AddRange(Directory.GetFiles(xcpFolderPath, "*.xcp"));
                        }
                        catch { }
                        _modInfos.Add(modInfo);
                    }
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
        private void InitializeChangedItems()
        {
            try { _changedItems = _getChangedItemAdapterList.Invoke(); }
            catch { _changedItems = null; }
        }
        private IReadOnlyDictionary<string, object?>? GetChangedItemsForMod(string modDirectory, string modName)
        {
            if (_changedItems != null)
            {
                var cached = _changedItems.FirstOrDefault(x => x.ModDirectory == modDirectory);
                if (cached.ChangedItems != null)
                    return cached.ChangedItems;
            }
            return _getChangedItems.Invoke(modDirectory, modName);
        }
        private void UpdateModInfo(string modDirectory)
        {
            var mod = _modInfos.FirstOrDefault(m => m.ModDirectory == modDirectory);
            if (mod != null)
            {
                try
                {
                    var collections = _getCollections.Invoke();
                    var currentCollectionId = collections.FirstOrDefault().Key;
                    if (currentCollectionId != Guid.Empty)
                    {
                        var (settingsEc, settings) = _getCurrentModSettings.Invoke(currentCollectionId, modDirectory, mod.ModName, false);
                        if (settingsEc == PenumbraApiEc.Success && settings.HasValue)
                        {
                            mod.Priority = settings.Value.Item2;
                            mod.IsEnabled = settings.Value.Item1;
                        }
                        _modInfos.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                    }
                }
                catch { }
            }
        }
        public void RefreshModInfo() => InitializeModInfo();
        public bool HasModChangesSinceLastRefresh { get; private set; } = false;
        public bool HasEverRefreshed { get; private set; } = false;

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