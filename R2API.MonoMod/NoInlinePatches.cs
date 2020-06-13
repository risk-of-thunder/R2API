// ReSharper disable InconsistentNaming

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
