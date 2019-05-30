namespace R2API {
    public static class Hooks {
        internal static void InitializeHooks() {
            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += orig => { };

            SurvivorAPI.InitHooks();
            AssetAPI.InitHooks();
            ItemDropAPI.SetHooks();
            InventoryAPI.InitHooks();
            LobbyConfigAPI.InitHooks();
        }
    }
}
