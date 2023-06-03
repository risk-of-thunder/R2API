using BepInEx;
using BepInEx.Logging;
using R2API.ContentManagement;

namespace R2API;

[BepInDependency(R2APIContentManager.PluginGUID)]
[BepInPlugin(PrefabAPI.PluginGUID, PrefabAPI.PluginName, PrefabAPI.PluginVersion)]
public sealed class PrefabPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }
}
