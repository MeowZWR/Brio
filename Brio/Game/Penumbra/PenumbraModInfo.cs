using System.Collections.Generic;

namespace Brio.Game.Penumbra
{
    public class PenumbraModInfo
    {
        public string ModName { get; set; } = string.Empty;
        public List<string> EmoteNames { get; set; } = new();
        public int Priority { get; set; }
        public List<string> XcpFiles { get; set; } = new();
        public bool IsEnabled { get; set; }
        public string ModDirectory { get; set; } = string.Empty;
    }
} 