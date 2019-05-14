// ReSharper disable InconsistentNaming

namespace RoR2 {
    internal class patch_SurvivorCatalog {
        public static SurvivorDef GetSurvivorDef(SurvivorIndex survivorIndex) {
            return R2API.SurvivorAPI.GetSurvivorDef(survivorIndex);
        }
    }
}
