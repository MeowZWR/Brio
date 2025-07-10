using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Brio.Core;

namespace Brio.Game.Penumbra
{
    public class PenumbraClipboardImporter
    {
        private static readonly string[] SupportedExtensions = { ".xcp" };
        private readonly Action<string> _notify;

        public PenumbraClipboardImporter(Action<string> notify)
        {
            _notify = notify;
        }

        private void ShowNotification(string content, Dalamud.Interface.ImGuiNotification.NotificationType type)
        {
            Service.NotificationManager?.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Content = content,
                Title = "卫月通知",
                Type = type,
                Minimized = false,
                InitialDuration = TimeSpan.FromSeconds(3)
            });
        }

        /// <summary>
        /// 从剪贴板导入.xcp文件到指定模组的XCP文件夹
        /// </summary>
        /// <param name="modRootPath">模组根目录（如 D:/Mods/SomeMod ）</param>
        /// <returns>导入的文件数</returns>
        public int ImportXcpFromClipboard(string modRootPath)
        {
            if (string.IsNullOrEmpty(modRootPath) || !Directory.Exists(modRootPath))
            {
                string msg = "模组路径无效，无法导入相机文件。";
                _notify(msg);
                ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
                return 0;
            }

            var xcpFolder = Path.Combine(modRootPath, "XCP");

            if (!System.Windows.Forms.Clipboard.ContainsFileDropList())
            {
                string msg = "剪贴板中未检测到文件。";
                _notify(msg);
                ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
                return 0;
            }

            var files = System.Windows.Forms.Clipboard.GetFileDropList().Cast<string>().ToList();
            // 先筛选出所有.xcp文件
            var xcpFiles = files.Where(file => SupportedExtensions.Contains(Path.GetExtension(file)?.ToLowerInvariant())).ToList();
            if (xcpFiles.Count == 0)
            {
                string msg = "未检测到可导入的.xcp文件。";
                _notify(msg);
                ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
                return 0;
            }

            if (!Directory.Exists(xcpFolder))
            {
                Directory.CreateDirectory(xcpFolder);
            }

            var imported = 0;
            foreach (var file in xcpFiles)
            {
                try
                {
                    var ext = Path.GetExtension(file)?.ToLowerInvariant();
                    var fileName = Path.GetFileName(file);
                    var destPath = Path.Combine(xcpFolder, fileName);
                    // 若重名则加后缀
                    var uniqueDestPath = destPath;
                    int count = 1;
                    while (File.Exists(uniqueDestPath))
                    {
                        uniqueDestPath = Path.Combine(xcpFolder, Path.GetFileNameWithoutExtension(fileName) + $"_{count}" + ext);
                        count++;
                    }
                    File.Copy(file, uniqueDestPath);
                    imported++;
                }
                catch (Exception ex)
                {
                    string msg = $"导入文件失败: {ex.Message}";
                    _notify(msg);
                    ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
                }
            }

            if (imported > 0)
            {
                string msg = $"成功导入 {imported} 个.xcp文件到 {xcpFolder}";
                _notify(msg);
                ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Success);
            }
            else
            {
                string msg = "未检测到可导入的.xcp文件。";
                _notify(msg);
                ShowNotification(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
            }
            return imported;
        }

        /// <summary>
        /// 在ImGui中绘制粘贴导入按钮，显示模组名和路径，点击后导入
        /// </summary>
        /// <param name="modName">模组名称</param>
        /// <param name="modRootPath">模组根目录</param>
        public void DrawPasteButton(string modName, string modRootPath)
        {
            var buttonLabel = $"粘贴导入相机文件 ({modName})";
            if (ImGuiNET.ImGui.Button(buttonLabel))
            {
                ImportXcpFromClipboard(modRootPath);
            }
            if (ImGuiNET.ImGui.IsItemHovered())
            {
                ImGuiNET.ImGui.BeginTooltip();
                ImGuiNET.ImGui.Text($"目标模组: {modName}");
                ImGuiNET.ImGui.Text($"文件夹: {modRootPath}\\XCP");
                ImGuiNET.ImGui.Text("将剪贴板中的.xcp文件导入到该模组的XCP文件夹");
                ImGuiNET.ImGui.EndTooltip();
            }
        }
    }
} 