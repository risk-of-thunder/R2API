using System;
using System.IO;
using BepInEx;
using R2API.Utils;

namespace R2API.AssetPlus {
    /// <summary>
    /// Simple class for adding all the individual of AssetPlus together
    /// </summary>
    ///     // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    [Obsolete("Use SoundAPI, LanguageAPI and FontAPI instead")]
    public static class AssetPlus
    {
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void RedirectAwake() {
            SoundAPI.SoundAwake();
            FontAPI.FontAwake();
            LanguageAPI.LanguageAwake();
        }
    }
}
