using BepInEx;
using BepInEx.Logging;

namespace R2API;

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
