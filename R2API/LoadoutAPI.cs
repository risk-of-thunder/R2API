using R2API.Utils;

namespace R2API {
    [R2APISubmodule]
    public static class LoadoutAPI {
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkillVariantLocked += CheckSkillVariantValid;
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkinLocked += CheckSkinValid;
        }

        internal static void UnsetHooks() {
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkillVariantLocked -= CheckSkillVariantValid;
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkinLocked -= CheckSkinValid;
        }

        private static bool CheckSkinValid(On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.orig_IsSkinLocked orig, object self, RoR2.UserProfile userProfile) {
            return !self.InvokeMethod<bool>("IsSkinValid") || orig(self, userProfile);
        }

        private static bool CheckSkillVariantValid(On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.orig_IsSkillVariantLocked orig, object self, int skillSlotIndex, RoR2.UserProfile userProfile) {
            return !self.InvokeMethod<bool>("IsSkillVariantValid", skillSlotIndex) || orig(self, skillSlotIndex, userProfile);
        }
    }
}
