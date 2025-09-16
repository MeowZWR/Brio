using Brio.Config;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace Brio.UI.Windows;

public class UpdateWindow : Window
{
    //
    // Some code found here is inspired by CharacterSelect+
    //

    private readonly ConfigurationService _configurationService;
    private readonly ImBrioText _imBrioText;

    private bool _scrollToTop = false;
    private float _closeButtonWidth => 310f * ImGuiHelpers.GlobalScale;

    private readonly List<string> _supporters = [];
    private readonly List<string> _contributors = [];
    public UpdateWindow(ConfigurationService configurationService, ImBrioText imBrioText) : base($"  {Brio.Name} 欢迎###brio_welcomewindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDecoration)
    {
        Namespace = "brio_welcomewindow_namespace";

        _configurationService = configurationService;
        _imBrioText = imBrioText;

        Size = new Vector2(710, 745);

        ShowCloseButton = false;
        AllowClickthrough = false;
        AllowPinning = false;

        string? line;

        var kofiStream = ResourceProvider.Instance.GetRawResourceStream("Data.kofi.txt");
        var patreonStream = ResourceProvider.Instance.GetRawResourceStream("Data.patreon.txt");
        var contributorsStream = ResourceProvider.Instance.GetRawResourceStream("Data.Contributors.txt");

        using var streamReader = new StreamReader(kofiStream, Encoding.UTF8, true, 128);
        while((line = streamReader.ReadLine()) is not null)
            _supporters.Add(line);

        using var streamReader2 = new StreamReader(patreonStream, Encoding.UTF8, true, 128);
        while((line = streamReader2.ReadLine()) is not null)
            _supporters.Add(line);

        using var streamReader3 = new StreamReader(contributorsStream, Encoding.UTF8, true, 128);
        while((line = streamReader3.ReadLine()) is not null)
            _contributors.Add(line);
    }

    public override void OnOpen()
    {
        _scrollToTop = true;
    }

    public override void PreDraw()
    {
        ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X - Size!.Value.X) / 2, (ImGui.GetIO().DisplaySize.Y - Size!.Value.Y) / 2), ImGuiCond.Appearing);

        base.PreDraw();
    }

    int selected = 0;
    public override void Draw()
    {
        var windowPos = ImGui.GetWindowPos();
        var windowPadding = ImGui.GetStyle().WindowPadding;

        var headerWidth = 1000f - (windowPadding.X * 2) * ImGuiHelpers.GlobalScale;
        var headerHeight = 500f * ImGuiHelpers.GlobalScale;

        var headerStart = windowPos - new Vector2(-1, 0);

        //

        var image = ResourceProvider.Instance.GetResourceImage($"Images.Update.brio-artbk-aug-25-01.png");

        // Calculate scaling to fill width and maintain aspect ratio
        var imageAspect = (float)image.Width / image.Height;
        var scaledWidth = headerWidth / 1.4f * ImGuiHelpers.GlobalScale;
        var scaledHeight = scaledWidth / imageAspect;

        var imagePos = headerStart;

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddImage(image.Handle, imagePos, imagePos + new Vector2(scaledWidth, scaledHeight));

        headerStart = new Vector2(headerStart.X, headerStart.Y + scaledHeight);
        var headerEnd = headerStart + new Vector2(headerWidth, headerHeight);

        DrawBackground(headerStart, headerEnd);

        ImGui.SetCursorScreenPos(headerStart);

        //

        var segmentSize = ImGui.GetWindowSize().X / 4.15f;

        var buttonSize = new Vector2(segmentSize, ImGui.GetTextLineHeight() * 1.7f);

        ImGui.Separator();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);

        using(ImRaii.PushColor(ImGuiCol.Button, new Vector4(0, 224, 148, 200) / 255))
            if(ImGui.Button("在 KoFi 上支持我们", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/minmoosexiv", UseShellExecute = true });
        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Button, new Vector4(65, 90, 240, 200) / 255))
            if(ImGui.Button("Brio Discord", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/GCb4srgEaH ", UseShellExecute = true });
        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Button, new Vector4(96, 108, 246, 200) / 255))
            if(ImGui.Button("Aetherworks Discord", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/KvGJCCnG8t", UseShellExecute = true });
        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Button, new Vector4(29, 161, 242, 200) / 255))
            if(ImGui.Button("更多链接", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://etheirystools.carrd.co", UseShellExecute = true });

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);

        ImGui.TextColored(new Vector4(0.4f, 0.9f, 0.4f, 1.0f), $"Brio v0.6.0有什么新功能");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.75f, 0.75f, 0.85f, 1.0f), $"  -  MCDFs, 动态面部控制, & ?????");

        ImBrio.VerticalPadding(10);

        ImGui.Text("要再次打开此窗口，请点击场景管理器上的`信息`按钮！");

        ImBrio.ToggleButtonStrip("selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, [" 更新日志 ", "支持者 & 贡献者"]);

        using(ImRaii.PushColor(ImGuiCol.ChildBg, 0))
        using(var c = ImRaii.Child("###brio_update_text", new Vector2(ImGui.GetWindowHeight() - 55 * ImGuiHelpers.GlobalScale, ImBrio.GetRemainingHeight() - 35f), false,
            Flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            if(c.Success)
            {

                if(_scrollToTop)
                {
                    _scrollToTop = false;
                    ImGui.SetScrollHereY(0);
                }

                ImBrio.VerticalPadding(10);

                if(selected == 0)
                {
                    DrawChangelog();
                }
                else
                {
                    DrawSupporters();
                }

                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
            }

        // 

        ImGui.SetCursorPosX(((ImGui.GetWindowSize().Y - _closeButtonWidth) / 2));

        if(ImBrio.Button("关闭", FontAwesomeIcon.SquareXmark, new Vector2(_closeButtonWidth, 0), centerTest: true))
        {
            this.IsOpen = false;
        }
    }

    public void DrawSupporters()
    {
        ImBrio.VerticalPadding(5);
        ImGui.Text("维护与开发: Minmoose. 原始开发者: Asgard. 祝您玩地愉快!");
        ImBrio.VerticalPadding(10);

        var slotSizes = ImGui.GetContentRegionAvail() / new Vector2(2, .8f);
        using(var leftGearGroup = ImRaii.Child("leftGroup", slotSizes))
        {
            if(leftGearGroup.Success)
            {
                ImGui.Text("衷心感谢以下人员在KoFi / Patreon上的支持!");

                ImBrio.VerticalPadding(5);

                foreach(var item in _supporters)
                {
                    ImGui.BulletText(item);
                }
            }
        }

        ImGui.SameLine();

        using(var rightGearGroup = ImRaii.Child("rightGroup", slotSizes))
        {
            if(rightGearGroup.Success)
            {
                ImGui.Text("衷心感谢以下人员对Brio的贡献!");

                ImBrio.VerticalPadding(5);

                foreach(var item in _contributors)
                {
                    ImGui.BulletText(item);
                }
            }
        }
    }

    private void DrawChangelog()
    {
        if(CollapsingHeader(" v0.6.0 – ", "  -  MCDFs, 动态面部控制, & ???? ", new Vector4(0.5f, 0.9f, 0.5f, 1.0f), true))
        {
            ImBrio.VerticalPadding(10);

            ImGui.BulletText("");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.6f, 0.4f, 0.4f, 1.0f), $"更新日志尚未编写，请稍后再来查看!");

            ImBrio.VerticalPadding(10);
        }

        if(CollapsingHeader(" v0.5.3 – 2025年8月30日", "  -  那该死的船 ", new Vector4(0.75f, 0.75f, 0.85f, 1.0f), false))
        {
            DrawFeature(FontAwesomeIcon.None, "0.5.3.1", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

            ImGui.BulletText("- 修复在场景管理器中选择东西的问题 (AHAHH)");

            DrawFeature(FontAwesomeIcon.None, "0.5.3", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

            ImGui.BulletText("添加了Moonfire Faire节日2024 & 2025到节日列表");
            ImGui.BulletText("修复了您有时无法与场景管理器交互的问题");
            ImGui.BulletText("重新启用双击重命名角色 ");
            ImGui.BulletText("面部装备现在正确显示在高级外观窗口中 (感谢 sparqle)");
            ImGui.BulletText("修复了某些服装无法正确选择父级骨骼的问题 (感谢 sparqle)");
            ImGui.BulletText("修复了姿势预览图像在第二次查看时未显示在资产库中的问题 (感谢 sparqle)");

            ImBrio.VerticalPadding(10);
        }

        if(CollapsingHeader(" v0.5.2 – 2025年8月15日", "  - 7.3 支持  ", new Vector4(0.75f, 0.75f, 0.85f, 1.0f), false))
        {
            DrawFeature(FontAwesomeIcon.None, "0.5.2.2", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

            ImGui.BulletText("修复了一个罕见的崩溃 ");
            ImGui.BulletText("修复了ImGUI断言错误");
            ImGui.BulletText("禁用双击重命名角色和相机以修复一个bug (暂时) ");

            DrawFeature(FontAwesomeIcon.None, "0.5.2", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

            ImGui.BulletText("更新Brio以支持FFXIV 7.3 (感谢Asgard的帮助!)");

            ImBrio.VerticalPadding(10);
        }

        if(CollapsingHeader(" v0.5.1 – 2025年3月27日", "  - 7.2 支持 ", new Vector4(0.75f, 0.75f, 0.85f, 1.0f), false))
        {
            DrawFeature(FontAwesomeIcon.None, "0.5.1.1", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

            ImGui.BulletText("您现在可以双击角色或相机来重命名它们 (感谢 @Bronya-Rand)");

            DrawFeature(FontAwesomeIcon.None, "0.5.1", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

            ImGui.BulletText("修复了新的面部装备无法装备的问题");
            ImGui.BulletText("添加了旋转自由相机的能力! (感谢 @Bronya-Rand)");

            ImGui.BulletText("修复了保存带有道具或挂载的项目会导致项目无法加载的问题 (感谢 @Bronya-Rand)");
            ImGui.BulletText("修复了环境中的`时间`滑块的格式，使其可以编辑");
            ImGui.BulletText("修复了训练陆行鸟时的崩溃问题");
            ImGui.BulletText("修复了在某些过场动画中崩溃的问题");
            ImGui.BulletText("修复了在某些过场动画中相机突然跳转的问题");
            ImGui.BulletText("修复了一个潜在的内存泄漏问题");

            ImBrio.VerticalPadding(10);
        }
    }

    //
    // some code found here is modified and from CharacterSelect+
    // https://github.com/IcarusXIV/Character-Select- (link is includes the -)
    //

    private static void DrawBackground(Vector2 headerStart, Vector2 headerEnd)
    {
        var drawList = ImGui.GetWindowDrawList();
        uint gradientTop = ImGui.GetColorU32(new Vector4(0.2f, 0.4f, 0.8f, 0.15f));
        uint gradientBottom = ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.2f, 0.05f));
        drawList.AddRectFilledMultiColor(headerStart, headerEnd * ImGuiHelpers.GlobalScale, gradientTop, gradientTop, gradientBottom, gradientBottom);
    }

    private static bool CollapsingHeader(string title, string subTitle, Vector4 titleColor, bool defaultOpen)
    {
        var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;

        bool isOpen = false;
        using(ImRaii.PushColor(ImGuiCol.Text, titleColor))
        {
            isOpen = ImGui.CollapsingHeader(title, flags);
        }

        ImGui.SameLine();

        ImGui.TextColored(new Vector4(0.75f, 0.75f, 0.85f, 1.0f), subTitle);

        return isOpen;
    }

    private static void DrawFeature(FontAwesomeIcon icon, string title, Vector4 accentColor)
    {
        var drawList = ImGui.GetWindowDrawList();
        var startPos = ImGui.GetCursorScreenPos();

        // Feature section background
        var backgroundMin = startPos + new Vector2(-10, -5);
        var backgroundMax = startPos + new Vector2(ImGui.GetContentRegionAvail().X + 10, 25);
        drawList.AddRectFilled(backgroundMin, backgroundMax, ImGui.GetColorU32(new Vector4(0.12f, 0.12f, 0.15f, 0.6f)), 4f);
        drawList.AddRectFilled(backgroundMin, backgroundMin + new Vector2(3, backgroundMax.Y - backgroundMin.Y), ImGui.GetColorU32(accentColor), 2f);

        ImGui.Spacing();
        ImBrio.Icon(icon);
        ImGui.SameLine();
        ImGui.TextColored(accentColor, title);
        ImGui.Spacing();
    }
}
