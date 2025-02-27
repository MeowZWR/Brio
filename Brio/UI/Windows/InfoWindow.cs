using Brio.Config;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;

namespace Brio.UI.Windows;

public class InfoWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly UpdateWindow _updateWindow;

    public InfoWindow(ConfigurationService configurationService, UpdateWindow updateWindow) : base($"{Brio.Name} Welcome###brio_info_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_info_namespace";

        this.AllowClickthrough = false;
        this.AllowPinning = false;
        this.ForceMainWindow = true;

        _configurationService = configurationService;
        _updateWindow = updateWindow;

        Size = new Vector2(580, -1);
    }

    public override void Draw()
    {
        var segmentSize = ImGui.GetWindowSize().X / 3.1f;
        var buttonSize = new Vector2(segmentSize, ImGui.GetTextLineHeight() * 1.8f);

        using(var textGroup = ImRaii.Group())
        {
            if(textGroup.Success)
            {
                var text = $"""
                    欢迎使用Brio，当前版本为 {_configurationService.Version}!

                    Brio是一套为您增强GPosing体验的工具。
                    目前仍处于alpha版本，因此可能存在bug。如果您遇到了问题，请向我们报告它们。

                    特别感谢以下各位通过Kofi对我们的支持！
                    (Sufferhymn), (Night Song), (Alvar Valo), (@Conna), (Yasumi),
                    (YikesXD), (Selitha), (AquilaHK), (LotusEcho), & (Yume);
                                        
                    感谢以下人员对Brio代码的贡献！
                    (@AsgardXIV), (@Ashadow700), (@ashna_ff14), (@Bronya-Rand), (@Caraxi),
                    (@Cazzar), (@danma3x), (@Enth), (@gris-fuego), (@HoloWise), 
                    (@MKhayle), (@WorstAquaPlayer), (@snaeling), (@Yuki-Codes);

                    维护与开发：Minmoose.
                    原始开发者: Asgard.
                    
                    汉化：MeowZWR.

                    祝您玩地愉快！

                    """;

                ImGui.PushTextWrapPos(segmentSize * 3);
                ImGui.TextWrapped(text);
                ImGui.PopTextWrapPos();
            }
        }

        using var buttonGroup = ImRaii.Group();
        if(buttonGroup.Success)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 100, 255, 255) / 255);
            if(ImGui.Button("支持开发", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/minmoosexiv", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(86, 98, 246, 255) / 255);
            if(ImGui.Button("加入Discord", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/KvGJCCnG8t", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 0, 0, 255) / 255);
            if(ImGui.Button("报告问题", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/Etheirys/Brio/issues", UseShellExecute = true });
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
            if(ImGui.Button("查看更新日志", buttonSize))
            {
                ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X - 630) / 2, (ImGui.GetIO().DisplaySize.Y - 535) / 2));
                _updateWindow.IsOpen = true;
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 70, 0, 255) / 255);
            if(ImGui.Button("许可&归属", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/Etheirys/Brio/blob/main/Acknowledgements.md", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(29, 161, 242, 255) / 255);
            if(ImGui.Button("更多链接", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://etheirystools.carrd.co", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }
    }
}
