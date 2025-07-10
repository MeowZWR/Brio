using System;
using System.Collections.Generic;

namespace Brio.Game.Penumbra
{
    public class PenumbraModInfo
    {
        public string ModName { get; set; } = string.Empty;
        public string EmoteName { get; set; } = string.Empty;
        public int Priority { get; set; }
        public List<string> XcpFiles { get; set; }

        public PenumbraModInfo()
        {
            XcpFiles = new List<string>();
        }
    }
} 