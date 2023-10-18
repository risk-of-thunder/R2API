using BepInEx;
using BepInEx.Logging;
using R2API.Utils;

namespace R2API;

[BepInPlugin(StringSerializerExtensions.PluginGUID, StringSerializerExtensions.PluginName, StringSerializerExtensions.PluginVersion)]
internal sealed class StringSerializerExtensionsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnEnable()
    {
        StringSerializerExtensions.ApplyHooks();
    }

    private void OnDisable()
    {
        StringSerializerExtensions.UndoHooks();
    }

    private void OnDestroy()
    {
        StringSerializerExtensions.FreeHooks();
    }
}
