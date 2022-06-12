using System;
using System.Runtime.CompilerServices;
using BepInEx;

[assembly: InternalsVisibleTo("R2API.Items")]

namespace R2API.ContentManagement;

[BepInPlugin(R2APIContentManager.PluginGUID, R2APIContentManager.PluginName, R2APIContentManager.PluginVersion)]
internal sealed class ContentManagementPlugin : BaseUnityPlugin {
    private void OnEnable() {
        R2APIContentManager.SetHooks();
    }

    private void OnDisable() {
        R2APIContentManager.UnsetHooks();
    }
}
