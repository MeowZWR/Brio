using System;
using Dalamud.Bindings.ImGui;
using System.IO;
using System.Linq;
using Brio.Core;

namespace Brio.Game.Penumbra
{
    public class PenumbraClipboardImporter
    {
        private static readonly string[] SupportedExtensions = { ".xcp" };
        private readonly Action<string> _notify;
        private readonly Action? _refreshCallback;

        public PenumbraClipboardImporter(Action<string> notify, Action? refreshCallback = null) 
        {
            _notify = notify;
            _refreshCallback = refreshCallback;
        }

        private void ShowNotification(string content, Dalamud.Interface.ImGuiNotification.NotificationType type)
        {
            Service.NotificationManager?.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Content = content,
                Title = "镜头文件",
                Type = type,
                Minimized = false,
                InitialDuration = TimeSpan.FromSeconds(3)
            });
        }

        public int ImportXcpFromClipboard(string modRootPath)
        {
            if (string.IsNullOrEmpty(modRootPath) || !Directory.Exists(modRootPath))
                return NotifyError("模组路径无效，无法导入相机文件。");

            var xcpFolder = Path.Combine(modRootPath, "XCP");
            if (!System.Windows.Forms.Clipboard.ContainsFileDropList())
                return NotifyError("剪贴板中未检测到文件。");

            var files = System.Windows.Forms.Clipboard.GetFileDropList().Cast<string>().ToList();
            var xcpFiles = files.Where(file => SupportedExtensions.Contains(Path.GetExtension(file)?.ToLowerInvariant())).ToList();
            if (xcpFiles.Count == 0)
                return NotifyError("未检测到可导入的.xcp文件。");

            if (!Directory.Exists(xcpFolder))
                Directory.CreateDirectory(xcpFolder);

            int imported = 0;
            foreach (var file in xcpFiles)
            {
                try
                {
                    var ext = Path.GetExtension(file)?.ToLowerInvariant();
                    var fileName = Path.GetFileName(file);
                    var destPath = Path.Combine(xcpFolder, fileName);
                    var uniqueDestPath = GetUniqueFilePath(destPath);
                    File.Copy(file, uniqueDestPath);
                    imported++;
                }
                catch (Exception ex)
                {
                    NotifyError($"导入文件失败: {ex.Message}");
                }
            }
            if (imported > 0)
            {
                var msg = $"成功导入 {imported} 个.xcp文件到 {xcpFolder}";
                _notify(msg);
                ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Success);
                
                _refreshCallback?.Invoke();
            }
            else
                NotifyError("未检测到可导入的.xcp文件。");
            return imported;
        }

        private int NotifyError(string msg)
        {
            _notify(msg);
            ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
            return 0;
        }

        private string GetUniqueFilePath(string originalPath)
        {
            if (!File.Exists(originalPath))
                return originalPath;

            var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);

            const int maxRetries = 1000;
            
            for (int count = 1; count <= maxRetries; count++)
            {
                var newFileName = $"{fileNameWithoutExt}_{count}{extension}";
                var newPath = Path.Combine(directory, newFileName);
                
                if (!File.Exists(newPath))
                    return newPath;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var fallbackFileName = $"{fileNameWithoutExt}_{timestamp}{extension}";
            return Path.Combine(directory, fallbackFileName);
        }

        public void DrawPasteButton(string modName, string modRootPath)
        {
            var buttonLabel = $"粘贴导入相机文件 ({modName})";
            if (ImGui.Button(buttonLabel))
                ImportXcpFromClipboard(modRootPath);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"目标模组: {modName}");
                ImGui.Text($"文件夹: {modRootPath}\\XCP");
                ImGui.Text("将剪贴板中的.xcp文件导入到该模组的XCP文件夹");
                ImGui.EndTooltip();
            }
        }
    }
} 