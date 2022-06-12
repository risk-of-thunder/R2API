using BepInEx;
using System;

namespace R2API;

[BepInPlugin(UnlockableAPI.PluginGUID, UnlockableAPI.PluginName, UnlockableAPI.PluginVersion)]
[Obsolete(UnlockableAPI.ObsoleteMessage)]
public sealed class ItemsPlugin : BaseUnityPlugin {
    private void OnEnable() {
        UnlockableAPI.SetHooks();
    }

    private void OnDisable() {
        UnlockableAPI.UnsetHooks();
    }
}
