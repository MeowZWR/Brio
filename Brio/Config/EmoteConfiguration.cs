using System.Collections.Generic;

namespace Brio.Config;

public class EmoteConfiguration
{
    public HashSet<string> Favorites { get; set; } = [];
}

public class EmoteFavoritesFile
{
    public HashSet<string> Favorites { get; set; } = [];
    public bool HideStartAnimations { get; set; }
    public bool TwoColumnLayout { get; set; } = true;
} 