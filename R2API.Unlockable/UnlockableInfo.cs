using System;
using System.Collections.Generic;
using System.Text;

namespace R2API {
    [Obsolete(UnlockableAPI.ObsoleteMessage)]
    internal struct UnlockableInfo {
        public string Name;
        public Func<string> HowToUnlockString;
        public Func<string> UnlockedString;
        public int SortScore;
    }
}
