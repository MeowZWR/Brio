using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Brio.Capabilities.Actor;
using Brio.Files;
using Brio.Game.Cutscene;
using Brio.Resources;
using Dalamud.Bindings.ImGui;

namespace Brio.Game.Penumbra
{
    public class PenumbraXcpService : IDisposable
    {
        private string? _currentEmoteName;
        private PenumbraModInfo? _cachedModInfoForEmote;
        private readonly List<string> _cachedXcpFiles = new();
        private string _cachedXcpModDirectory = string.Empty;
        private string _selectedXcpFile = string.Empty;
        private bool _isSelectedFromPenumbra = false;
        
        private readonly CutsceneManager _cutsceneManager;
        private FileSystemWatcher? _xcpFileWatcher;
        
        // 文件系统操作缓存
        private string _cachedModPath = string.Empty;
        private string _cachedXcpFolderPath = string.Empty;
        private bool? _cachedXcpFolderExists;
        private uint _cacheFrame = 0;
        private const uint CACHE_REFRESH_INTERVAL = 30; // 每30帧刷新一次缓存
        
        public event Action<string>? SelectedXcpFileChanged;
        public event Action<string?>? CurrentEmoteNameChanged;

        public PenumbraXcpService(CutsceneManager cutsceneManager)
        {
            _cutsceneManager = cutsceneManager;
            
            if (PenumbraManager.Instance != null)
                PenumbraManager.Instance.ModInfoChanged += OnPenumbraModInfoChanged;
        }

        public string SelectedXcpFile
        {
            get => _selectedXcpFile;
            private set
            {
                if (_selectedXcpFile != value)
                {
                    _selectedXcpFile = value;
                    SelectedXcpFileChanged?.Invoke(value);
                }
            }
        }

        public string? CurrentEmoteName
        {
            get => _currentEmoteName;
            private set
            {
                if (_currentEmoteName != value)
                {
                    _currentEmoteName = value;
                    CurrentEmoteNameChanged?.Invoke(value);
                }
            }
        }

        public PenumbraModInfo? CachedModInfoForEmote => _cachedModInfoForEmote;
        public IReadOnlyList<string> CachedXcpFiles => _cachedXcpFiles.AsReadOnly();
        public bool IsSelectedFromPenumbra => _isSelectedFromPenumbra;

        public void UpdateCurrentEmoteFromCapability(ActionTimelineCapability capability) =>
            SetCurrentEmoteName(GetEmoteNameFromCapability(capability));

        public void SetCurrentEmoteName(string? emoteName)
        {
            if (CurrentEmoteName == emoteName) return;

            CurrentEmoteName = emoteName;
            SelectedXcpFile = string.Empty;
            
            _cachedModInfoForEmote = !string.IsNullOrEmpty(emoteName)
                ? PenumbraManager.Instance?.GetEffectiveModInfoForEmote(emoteName)
                : null;
                
            InvalidateCache();
            RefreshXcpCache();
        }
        
        private void InvalidateCache()
        {
            _cachedModPath = string.Empty;
            _cachedXcpFolderPath = string.Empty;
            _cachedXcpFolderExists = null;
            _cacheFrame = 0;
        }

        public bool SelectXcpFile(string xcpFilePath, bool isFromPenumbra)
        {
            try
            {
                _cutsceneManager.CameraPath = new XATCameraFile(new BinaryReader(File.OpenRead(xcpFilePath)));
                SelectedXcpFile = xcpFilePath;
                _isSelectedFromPenumbra = isFromPenumbra;
                
                var source = isFromPenumbra ? "Penumbra" : "浏览";
                Brio.Log.Information($"已从{source}加载 XCP 文件: {Path.GetFileName(xcpFilePath)}");
                return true;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"加载 XCP 文件失败: {ex.Message}");
                ClearSelectedXcpFile();
                return false;
            }
        }

        public bool SelectXcpFileFromPenumbra(string xcpFilePath) => SelectXcpFile(xcpFilePath, true);
        public bool SelectXcpFileFromBrowse(string xcpFilePath) => SelectXcpFile(xcpFilePath, false);

        public void ClearSelectedXcpFile()
        {
            SelectedXcpFile = string.Empty;
            _cutsceneManager.CameraPath = null;
            _isSelectedFromPenumbra = false;
        }

        public void ResetSelectionFlag() => _isSelectedFromPenumbra = false;

        public string GetCurrentModPath()
        {
            var currentFrame = (uint)ImGui.GetFrameCount();
            if (currentFrame - _cacheFrame < CACHE_REFRESH_INTERVAL && !string.IsNullOrEmpty(_cachedModPath))
                return _cachedModPath;

            if (_cachedModInfoForEmote == null || PenumbraManager.Instance == null)
            {
                _cachedModPath = "未知";
            }
            else
            {
                try
                {
                    var modRootDirectory = PenumbraManager.Instance.GetModRootDirectory();
                    _cachedModPath = Path.Combine(modRootDirectory, _cachedModInfoForEmote.ModDirectory);
                }
                catch
                {
                    _cachedModPath = "未知";
                }
            }
            
            _cacheFrame = currentFrame;
            return _cachedModPath;
        }

        public string GetCurrentXcpFolderPath()
        {
            var modPath = GetCurrentModPath();
            if (modPath == "未知") 
            {
                _cachedXcpFolderPath = string.Empty;
                return _cachedXcpFolderPath;
            }
            
            _cachedXcpFolderPath = Path.Combine(modPath, "XCP");
            return _cachedXcpFolderPath;
        }

        public bool XcpFolderExists() 
        {
            var currentFrame = (uint)ImGui.GetFrameCount();
            if (currentFrame - _cacheFrame < CACHE_REFRESH_INTERVAL && _cachedXcpFolderExists.HasValue)
                return _cachedXcpFolderExists.Value;
                
            var folderPath = GetCurrentXcpFolderPath();
            _cachedXcpFolderExists = !string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath);
            return _cachedXcpFolderExists.Value;
        }

        public bool TryCreateXcpFolder()
        {
            try
            {
                var xcpFolderPath = GetCurrentXcpFolderPath();
                if (!string.IsNullOrEmpty(xcpFolderPath))
                {
                    Directory.CreateDirectory(xcpFolderPath);
                    
                    // 创建文件夹后立即清空缓存，确保下次检查时能获取到最新状态
                    InvalidateCache();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"创建 XCP 文件夹失败: {ex.Message}");
                return false;
            }
        }

        public bool TryOpenXcpFolderInExplorer()
        {
            try
            {
                var xcpFolderPath = GetCurrentXcpFolderPath();
                if (Directory.Exists(xcpFolderPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", xcpFolderPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"打开文件夹失败: {ex.Message}");
                return false;
            }
        }

        public void RefreshXcpCache()
        {
            _cachedXcpFiles.Clear();
            _cachedXcpModDirectory = string.Empty;
            
            StopFileWatcher();
            
            InvalidateCache();

            if (_cachedModInfoForEmote == null) return;

            try
            {
                var modRootDirectory = PenumbraManager.Instance?.GetModRootDirectory();
                if (!string.IsNullOrEmpty(modRootDirectory))
                {
                    var modPath = Path.Combine(modRootDirectory, _cachedModInfoForEmote.ModDirectory);
                    var xcpFolderPath = Path.Combine(modPath, "XCP");

                    if (Directory.Exists(xcpFolderPath))
                    {
                        _cachedXcpFiles.AddRange(Directory.GetFiles(xcpFolderPath, "*.xcp"));
                        
                        StartFileWatcher(xcpFolderPath);
                    }

                    _cachedXcpModDirectory = _cachedModInfoForEmote.ModDirectory;
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"刷新 XCP 缓存失败: {ex.Message}");
            }
        }

        private string? GetEmoteNameFromCapability(ActionTimelineCapability capability)
        {
            if (capability.SlotedBaseAnimation == 0) return null;

            try
            {
                var timelineId = (uint)capability.SlotedBaseAnimation;
                if (!GameDataProvider.Instance.ActionTimelines.TryGetValue(timelineId, out var timeline)) 
                    return null;

                var emote = GameDataProvider.Instance.Emotes.Values
                    .Where(e => e.ActionTimeline.Any(at => at.RowId == capability.SlotedBaseAnimation))
                    .FirstOrDefault();
                return !emote.Equals(default) ? emote.Name.ToString() : null;
            }
            catch (Exception ex)
            {
                Brio.Log.Error($"获取情感动作名称失败: {ex.Message}");
                return null;
            }
        }

        private void OnPenumbraModInfoChanged()
        {
            Brio.Log.Debug($"PenumbraXcpService 收到模组信息变化事件，当前情感动作: {_currentEmoteName}");
            var oldModInfo = _cachedModInfoForEmote;
            
            if (!string.IsNullOrEmpty(_currentEmoteName))
                _cachedModInfoForEmote = PenumbraManager.Instance?.GetEffectiveModInfoForEmote(_currentEmoteName);
            
            InvalidateCache();
            RefreshXcpCache();
            
            bool modChanged = (oldModInfo?.ModDirectory != _cachedModInfoForEmote?.ModDirectory) ||
                             (oldModInfo?.ModName != _cachedModInfoForEmote?.ModName);
                             
            if (modChanged && !string.IsNullOrEmpty(_selectedXcpFile))
            {
                Brio.Log.Debug($"模组信息变化: '{oldModInfo?.ModName}' -> '{_cachedModInfoForEmote?.ModName}'");
                ResetSelectionFlag();
            }
            else if (modChanged)
            {
                Brio.Log.Debug($"模组信息变化但无选择的XCP文件: '{oldModInfo?.ModName}' -> '{_cachedModInfoForEmote?.ModName}'");
            }
            else
            {
                Brio.Log.Debug("模组信息未发生变化");
            }
        }

        private void StartFileWatcher(string xcpFolderPath)
        {
            try
            {
                _xcpFileWatcher = new FileSystemWatcher(xcpFolderPath)
                {
                    Filter = "*.xcp",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                _xcpFileWatcher.Created += OnXcpFileChanged;
                _xcpFileWatcher.Deleted += OnXcpFileChanged;
                _xcpFileWatcher.Renamed += OnXcpFileChanged;
                
                Brio.Log.Debug($"开始监控XCP文件夹: {xcpFolderPath}");
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"启动文件监控失败: {ex.Message}");
            }
        }

        private void StopFileWatcher()
        {
            if (_xcpFileWatcher != null)
            {
                _xcpFileWatcher.EnableRaisingEvents = false;
                _xcpFileWatcher.Created -= OnXcpFileChanged;
                _xcpFileWatcher.Deleted -= OnXcpFileChanged;
                _xcpFileWatcher.Renamed -= OnXcpFileChanged;
                _xcpFileWatcher.Dispose();
                _xcpFileWatcher = null;
                Brio.Log.Debug("停止XCP文件监控");
            }
        }

        private void OnXcpFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // 延迟一小段时间以避免文件操作冲突
                Task.Delay(100).ContinueWith(_ =>
                {
                    Brio.Log.Debug($"检测到XCP文件变化: {e.Name} ({e.ChangeType})");
                    RefreshXcpCacheFiles();
                });
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"处理文件变化事件失败: {ex.Message}");
            }
        }

        private void RefreshXcpCacheFiles()
        {
            if (_cachedModInfoForEmote == null) return;

            try
            {
                var modRootDirectory = PenumbraManager.Instance?.GetModRootDirectory();
                if (!string.IsNullOrEmpty(modRootDirectory))
                {
                    var modPath = Path.Combine(modRootDirectory, _cachedModInfoForEmote.ModDirectory);
                    var xcpFolderPath = Path.Combine(modPath, "XCP");

                    _cachedXcpFiles.Clear();
                    if (Directory.Exists(xcpFolderPath))
                        _cachedXcpFiles.AddRange(Directory.GetFiles(xcpFolderPath, "*.xcp"));
                        
                    Brio.Log.Debug($"已刷新XCP文件缓存，找到 {_cachedXcpFiles.Count} 个文件");
                }
            }
            catch (Exception ex)
            {
                Brio.Log.Warning($"刷新XCP文件缓存失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopFileWatcher();
            
            if (PenumbraManager.Instance != null)
                PenumbraManager.Instance.ModInfoChanged -= OnPenumbraModInfoChanged;
        }
    }
} 