using System.Diagnostics;
using BepInEx;
using BepInEx.Logging;
using R2API.ContentManagement;

namespace R2API;

[BepInDependency(R2APIContentManager.PluginGUID)]
[BepInPlugin(EliteAPI.PluginGUID, EliteAPI.PluginName, EliteAPI.PluginVersion)]
public sealed class ElitesPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        Stopwatch s = new Stopwatch();
        s.Start();

        EliteAPI.SetHooks();
        EliteRamp.SetHooks();

        s.Stop();
        Logger.LogDebug("startup done in: " + s.Elapsed.TotalMilliseconds + "ms");
    }

    private void OnDestroy()
    {
        EliteAPI.UnsetHooks();
        EliteRamp.UnsetHooks();
    }
}
