using System;
using System.Runtime.CompilerServices;
using BepInEx;

[assembly: InternalsVisibleTo("R2API.Items")]
[assembly: InternalsVisibleTo("R2API.Elites")]
[assembly: InternalsVisibleTo("R2API.Unlockable")]
[assembly: InternalsVisibleTo("R2API.TempVisualEffect")]
[assembly: InternalsVisibleTo("R2API.Loadout")]
[assembly: InternalsVisibleTo("R2API.Sound")]
[assembly: InternalsVisibleTo("R2API.Prefab")]

namespace R2API.ContentManagement;

[BepInPlugin(R2APIContentManager.PluginGUID, R2APIContentManager.PluginName, R2APIContentManager.PluginVersion)]
internal sealed class ContentManagementPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        R2APIContentManager.UnsetHooks();
    }
}
