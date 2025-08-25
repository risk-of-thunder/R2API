using BepInEx;
using BepInEx.Logging;
using R2API.AutoVersionGen;
using System;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API;

[AutoVersion]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public sealed partial class AddressablesPlugin : BaseUnityPlugin
{
    public const string PluginGUID = R2API.PluginGUID + ".addressables";
    public const string PluginName = R2API.PluginName + ".Addressables";
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }
}
