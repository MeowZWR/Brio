using System;
using System.IO;
using System.Linq;
using Dalamud.Plugin.Services;

namespace Brio.Core;

public static class ClientRegionHelper
{
    private static bool? s_isChineseClient;

    public static bool IsChineseClient()
    {
        if(s_isChineseClient.HasValue)
            return s_isChineseClient.Value;

        if(global::Brio.Brio.TryGetService<IPlayerState>(out var playerState)
           && playerState.IsLoaded
           && playerState.HomeWorld.IsValid)
        {
            var homeWorldName = playerState.HomeWorld.Value.Name.ToString();
            var result = ContainsCjk(homeWorldName);
            s_isChineseClient = result;
            return result;
        }

        return false;
    }

    public static string GetDalamudLogPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var launcher = IsChineseClient() ? "XIVLauncherCN" : "XIVLauncher";
        return Path.Join(appData, launcher, "dalamud.log");
    }

    private static bool ContainsCjk(string value)
        => value.Any(c => c is >= '\u4E00' and <= '\u9FFF');
}
