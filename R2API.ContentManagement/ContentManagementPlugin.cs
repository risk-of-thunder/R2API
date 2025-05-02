using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;

[assembly: InternalsVisibleTo("R2API.Items")]
[assembly: InternalsVisibleTo("R2API.Elites")]
[assembly: InternalsVisibleTo("R2API.Unlockable")]
[assembly: InternalsVisibleTo("R2API.TempVisualEffect")]
[assembly: InternalsVisibleTo("R2API.Loadout")]
[assembly: InternalsVisibleTo("R2API.Sound")]
[assembly: InternalsVisibleTo("R2API.Stages")]
[assembly: InternalsVisibleTo("R2API.Prefab")]
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API.ContentManagement;

[BepInPlugin(R2APIContentManager.PluginGUID, R2APIContentManager.PluginName, R2APIContentManager.PluginVersion)]
internal sealed class ContentManagementPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        R2APIContentPackProvider.Init();
        GameModeFixes.AddModdedGameModeSupport();
    }

    private void OnDestroy()
    {
        R2APIContentManager.UnsetHooks();
    }
}
