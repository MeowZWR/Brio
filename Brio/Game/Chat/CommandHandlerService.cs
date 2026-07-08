using Brio.Services;
using Brio.UI;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.Chat;

public class CommandHandlerService : IDisposable
{
    private const string BrioCommandName = "/brio";
    private const string XATCommandName = "/xat";
    private const string MCDFCommandName = "/mcdf";

    private readonly ICommandManager _commandManager;
    private readonly IChatGui _chatGui;
    private readonly UIManager _uiManager;
    private readonly Mediator _mediator;

    public CommandHandlerService(ICommandManager commandManager, IChatGui chatGui, UIManager uiManager, Mediator mediator)
    {
        _commandManager = commandManager;
        _chatGui = chatGui;
        _uiManager = uiManager;
        _mediator = mediator;

        _commandManager.AddHandler(BrioCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "开关Brio窗口。",
            ShowInHelp = true,
        });
        _commandManager.AddHandler(XATCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "开关Brio窗口。",
            ShowInHelp = false,
        });
        _commandManager.AddHandler(MCDFCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "开关 Brio 的 MCDF 窗口。",
            ShowInHelp = false,
        });
    }

    private void OnCommand(string command, string arguments)
    {
        if(command == MCDFCommandName)
        {
            _uiManager.ToggleMCDFWindow();
            return;
        }

        if(arguments.Length == 0)
            arguments = "window";

        var argumentList = arguments.Split(' ', 2);

        switch(argumentList[0].ToLowerInvariant())
        {
            case "window":
                _uiManager.ToggleMainWindow();
                break;

            case "timeline":
                _uiManager.ToggleTimelineWindow();
                break;

            case "settings":
                _uiManager.ToggleSettingsWindow();
                break;

            case "about":
                _uiManager.ToggleWelcomeWindow();
                break;

            case "mcdf":
                _uiManager.ToggleMCDFWindow();
                break;

            case "mediator":
                _mediator.PrintSubscriberInfo();
                break;

            case "help":
            default:
                PrintHelp();
                break;
        }

    }

    private void PrintHelp()
    {
        _chatGui.Print("有效的 Brio 命令：");
        _chatGui.Print("<无参数> - 开关主 Brio 窗口");
        _chatGui.Print("window - 开关主 Brio 窗口");
        _chatGui.Print("settings - 开关 Brio 设置窗口");
        _chatGui.Print("about - 开关 Brio 信息窗口");
        _chatGui.Print("help - 显示此帮助");
    }

    public void Dispose()
    {
        _commandManager.RemoveHandler(BrioCommandName);
        _commandManager.RemoveHandler(XATCommandName);
        _commandManager.RemoveHandler(MCDFCommandName);
    }
}
