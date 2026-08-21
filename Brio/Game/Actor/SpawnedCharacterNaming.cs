using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Brio.Game.Actor;

public static class SpawnedCharacterNaming
{
    private const int ChineseMaxLength = 6;
    private const int InternationalCustomMaxLength = 20;
    private const int InternationalRawMaxLength = 14;
    private const int ChineseClientLanguage = 4;
    private const int ChineseWorldUserType = 101;

    public static bool IsChineseClient(IClientState clientState, IObjectTable? objectTable = null)
        => (int)clientState.ClientLanguage == ChineseClientLanguage
            || (objectTable != null && IsChineseWorld(objectTable));

    public static string ResolveSpawnName(IClientState clientState, IObjectTable objectTable, int spawnIndex, string? customName = null)
        => IsChineseClient(clientState, objectTable)
            ? ResolveChineseSpawnName(spawnIndex, customName)
            : ResolveInternationalSpawnName(spawnIndex, customName);

    public static string GetCustomNpcObjectName(IClientState clientState, string npcName, IObjectTable? objectTable = null)
        => IsChineseClient(clientState, objectTable)
            ? npcName
            : npcName.Split(' ')[0] + " Cnpc";

    public static string SanitizeChineseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "NPC";

        var sanitized = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (IsAllowedChineseNameCharacter(character))
                sanitized.Append(character);
        }

        if (sanitized.Length == 0)
            return "NPC";

        if (sanitized[0] is >= 'a' and <= 'z')
            sanitized[0] = char.ToUpperInvariant(sanitized[0]);

        if (sanitized.Length <= ChineseMaxLength)
            return sanitized.ToString();

        var fullName = sanitized.ToString();
        return fullName.Substring(0, 2) + CreateHashSuffix(fullName, ChineseMaxLength - 2);
    }

    public static bool IsPluginSpawnedObjectName(IClientState clientState, IObjectTable? objectTable, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (IsChineseClient(clientState, objectTable))
        {
            return string.Equals(name, "Cutscn", StringComparison.Ordinal)
                || (name.Length == ChineseMaxLength
                    && name.StartsWith("Aq", StringComparison.Ordinal)
                    && name.Skip(2).All(character => character is >= 'a' and <= 'z'));
        }

        return name.StartsWith("Reborn", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Cutscene", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCustomNpcObjectName(IClientState clientState, IObjectTable? objectTable, string objectName, IEnumerable<string> configuredNpcNames)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        if (IsChineseClient(clientState, objectTable))
        {
            return configuredNpcNames.Any(npcName =>
                string.Equals(SanitizeChineseName(npcName), objectName, StringComparison.OrdinalIgnoreCase));
        }

        return objectName.Contains("Cnpc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsChineseWorld(IObjectTable objectTable)
    {
        var player = objectTable.LocalPlayer;
        if (player == null)
            return false;

        var world = player.CurrentWorld.Value;
        return world.RowId > 1000 && world.UserType == ChineseWorldUserType;
    }

    private static bool IsAllowedChineseNameCharacter(char character)
        => character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '\u3400' and <= '\u4DBF'
            or >= '\u4E00' and <= '\u9FFF'
            or >= '\uF900' and <= '\uFAFF';

    private static string CreateHashSuffix(string value, int length)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        var suffix = new char[length];
        for (var index = length - 1; index >= 0; index--)
        {
            suffix[index] = (char)('a' + hash % 26);
            hash /= 26;
        }

        return new string(suffix);
    }

    private static string ResolveInternationalSpawnName(int spawnIndex, string? customName)
    {
        if (!string.IsNullOrEmpty(customName) && spawnIndex > 0)
        {
            return customName.Length > InternationalCustomMaxLength
                ? customName.Substring(0, InternationalCustomMaxLength)
                : customName;
        }

        if (spawnIndex == 0)
            return "Cutscene Player";

        var raw = "Reborn" + Regex.Replace(Guid.NewGuid().ToString(), @"[\d-]", string.Empty);
        var name = raw.Substring(0, Math.Clamp(raw.Length, 0, InternationalRawMaxLength));
        var length = name.Length / 2;
        return FirstCharToUpper(name.Substring(0, length)) + " " + FirstCharToUpper(name.Substring(length));
    }

    private static string ResolveChineseSpawnName(int spawnIndex, string? customName)
    {
        if (!string.IsNullOrEmpty(customName))
            return SanitizeChineseName(customName);

        if (spawnIndex == 0)
            return "Cutscn";

        var bytes = Guid.NewGuid().ToByteArray();
        return "Aq" + string.Concat(bytes.Take(4).Select(value => (char)('a' + value % 26)));
    }

    private static string FirstCharToUpper(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Name segment was empty.", nameof(input));

        return input.First().ToString().ToUpperInvariant() + string.Join(string.Empty, input.Skip(1));
    }
}
