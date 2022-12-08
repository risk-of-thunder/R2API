using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(PrefabAPI.PluginGUID, PrefabAPI.PluginName, PrefabAPI.PluginVersion)]
public sealed class PrefabPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }
}
