using BepInEx;
using BepInEx.Logging;

namespace R2API.Utils;

#pragma warning disable CS0618 // Type or member is obsolete
[BepInPlugin(CommandHelper.PluginGUID, CommandHelper.PluginName, CommandHelper.PluginVersion)]
#pragma warning restore CS0618 // Type or member is obsolete
public sealed class CommandHelperPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        CommandHelper.UnsetHooks();
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
