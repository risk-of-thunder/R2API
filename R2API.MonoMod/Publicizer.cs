using MonoMod;

namespace RoR2 {
    class Publicizer {
        [MonoModPublic]
        [MonoModPatch("RoR2.SteamworksServerManager")]
        public class PublicSteamworksServerManager { }

        [MonoModPublic]
        [MonoModPatch("RoR2.DotController/DotDef")]
        public class PublicDotDef { }

        [MonoModPublic]
        [MonoModPatch("RoR2.DotController/DotStack")]
        public class PublicDotStack { }
    }
}
