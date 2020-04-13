using System.IO;
using BepInEx;
using R2API.Utils;

namespace R2API.AssetPlus {
    /// <summary>
    /// Simple class for adding all the individual of AssetPlus together
    /// </summary>
    ///     // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class AssetPlus
    {
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        public static void Awake()
        {
            SoundPlus.SoundAwake();
            FontPlus.FontAwake();
            TextPlus.LanguageAwake();
        }

        internal static string[] GetFiles(string Extension) {
            return Directory.GetFiles(Paths.PluginPath, "*." + Extension, SearchOption.AllDirectories);
        }
    }
}
