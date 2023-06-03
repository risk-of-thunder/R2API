using BepInEx;
using BepInEx.Logging;
using R2API.ContentManagement;

namespace R2API;

[BepInDependency(R2APIContentManager.PluginGUID)]
[BepInPlugin(ItemAPI.PluginGUID, ItemAPI.PluginName, ItemAPI.PluginVersion)]
public sealed class ItemsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        ItemAPI.UnsetHooks();
    }
}
