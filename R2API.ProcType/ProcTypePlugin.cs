using BepInEx;
using BepInEx.Logging;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API;

[BepInPlugin(ProcTypeAPI.PluginGUID, ProcTypeAPI.PluginName, ProcTypeAPI.PluginVersion)]
public sealed class ProcTypePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        ProcTypeAPI.UnsetHooks();
    }
}
