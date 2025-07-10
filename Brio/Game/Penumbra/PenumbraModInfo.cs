using System;
using System.Collections.Generic;

namespace Brio.Game.Penumbra
{
    public class PenumbraModInfo
    {
        public string ModName { get; set; } = string.Empty;
        public List<string> EmoteNames { get; set; }
        public int Priority { get; set; }
        public List<string> XcpFiles { get; set; }
        public bool IsEnabled { get; set; } = false;
        public string ModDirectory { get; set; } = string.Empty;

        public PenumbraModInfo()
        {
            XcpFiles = new List<string>();
            EmoteNames = new List<string>();
        }
    }
} 