using RoR2;
using System;

namespace R2API {
    public class R2APIContentPackProvider {
        public string identifier => "R2API";

        internal static ContentPack ContentPack = new ContentPack();

        internal static Action<ContentPack> WhenContentPackReady;

        internal static void Init() {
            On.RoR2.ContentManager.SetContentPacks += AddCustomContent;
        }

        private static void AddCustomContent(On.RoR2.ContentManager.orig_SetContentPacks orig, System.Collections.Generic.List<ContentPack> newContentPacks) {
            if (WhenContentPackReady != null) {
                foreach (Action<ContentPack> @event in WhenContentPackReady.GetInvocationList()) {
                    try {
                        @event(ContentPack);
                    }
                    catch (Exception e) {
                        R2API.Logger.LogError(e);
                    }
                }
            }

            newContentPacks.Add(ContentPack);
            orig(newContentPacks);
        }
    }
}
