// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
using MonoMod;

namespace RoR2 {

    internal class patch_SurvivorCatalog {

        [MonoModIgnore] [NoInlining]
        public static extern SurvivorDef GetSurvivorDef(SurvivorIndex survivorIndex);
    }

    internal class patch_DifficultyCatalog {
        [MonoModIgnore] [NoInlining]
        public static extern DifficultyDef GetDifficultyDef(DifficultyIndex difficultyIndex);
    }
}
#pragma warning restore IDE1006 // Naming Styles
