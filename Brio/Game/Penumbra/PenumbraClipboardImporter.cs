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

        /// <summary>
        /// 从剪贴板导入.xcp文件到指定模组的XCP文件夹
        /// </summary>
        /// <param name="modRootPath">模组根目录（如 D:/Mods/SomeMod ）</param>
        /// <returns>导入的文件数</returns>
        public int ImportXcpFromClipboard(string modRootPath)
        {
            if (string.IsNullOrEmpty(modRootPath) || !Directory.Exists(modRootPath))
            {
                _notify("模组路径无效，无法导入相机文件。");
                return 0;
            }

            var xcpFolder = Path.Combine(modRootPath, "XCP");
            if (!Directory.Exists(xcpFolder))
            {
                Directory.CreateDirectory(xcpFolder);
            }

            if (!System.Windows.Forms.Clipboard.ContainsFileDropList())
            {
                _notify("剪贴板中未检测到文件。");
                return 0;
            }

            var files = System.Windows.Forms.Clipboard.GetFileDropList().Cast<string>().ToList();
            var imported = 0;
            foreach (var file in files)
            {
                try
                {
                    var ext = Path.GetExtension(file)?.ToLowerInvariant();
                    if (!SupportedExtensions.Contains(ext))
                        continue;

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
                    _notify($"导入文件失败: {ex.Message}");
                }
            }

            if (imported > 0)
                _notify($"成功导入 {imported} 个.xcp文件到 {xcpFolder}");
            else
                _notify("未检测到可导入的.xcp文件。");
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