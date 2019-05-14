// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;
using MonoMod;

namespace RoR2 {
    internal class patch_SurvivorCatalog {
        [MonoModIgnore]
        public static extern SurvivorDef orig_GetSurvivorDef(SurvivorIndex survivorIndex);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SurvivorDef GetSurvivorDef(SurvivorIndex survivorIndex) {
            return orig_GetSurvivorDef(survivorIndex);
        }
    }
}
