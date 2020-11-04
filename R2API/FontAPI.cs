using BepInEx;
using R2API.Utils;

using System;
using System.Collections.Generic;
using System.IO;

namespace R2API {
    /// <summary>
    /// API for replacing the ingame font
    /// </summary>
    [R2APISubmodule]
    public static class FontAPI {
        public static bool Loaded {
            get; private set;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void FontAwake() {
            if (Loaded) {
                return;
            }

            Loaded = true;
            var fontFiles = Directory.GetFiles(Paths.PluginPath, "*.font", SearchOption.AllDirectories);

            foreach (var fontFile in fontFiles) {
                Fonts.Add(fontFile);
            }

            On.RoR2.UI.HGTextMeshProUGUI.OnCurrentLanguageChanged += HGTextMeshProUGUI_OnCurrentLanguageChanged;
            On.RoR2.Language.SetCurrentLanguage += Language_SetCurrentLanguage;
        }

        [R2APISubmoduleInit(Stage = InitStage.LoadCheck)]
        private static void ShouldLoad(out bool shouldload) {
            shouldload = Directory.GetFiles(Paths.PluginPath, "*.font", SearchOption.AllDirectories).Length > 0;
        }

        private static void Language_SetCurrentLanguage(On.RoR2.Language.orig_SetCurrentLanguage orig, string language) {
            orig(language);
            if (Fonts._fontAssets.Count != 0) {
                RoR2.UI.HGTextMeshProUGUI.defaultLanguageFont = Fonts._fontAssets[0];
            }
        }

        private static void HGTextMeshProUGUI_OnCurrentLanguageChanged(On.RoR2.UI.HGTextMeshProUGUI.orig_OnCurrentLanguageChanged orig) {
            if (Fonts._fontAssets.Count != 0) {
                RoR2.UI.HGTextMeshProUGUI.defaultLanguageFont = Fonts._fontAssets[0];
            }
        }

        /// <summary>
        /// use this class to add fonts
        /// </summary>
        public static class Fonts {
            /// <summary>
            /// for adding an TMP_FontAsset inside an seperate assetbundle (.font is loaded automatically)
            /// </summary>
            /// <param name="path">absolute path to the assetbundle</param>
            public static void Add(string? path) {
                if(!Loaded) {
                    throw new InvalidOperationException($"{nameof(FontAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(FontAPI)})]");
                }
                var fontBundle = UnityEngine.AssetBundle.LoadFromFile(path);
                var fonts = fontBundle.LoadAllAssets<TMPro.TMP_FontAsset>();
                foreach (var font in fonts) {
                    Add(font);
                }
            }

            /// <summary>
            /// for adding an TMP_FontAsset while it is still in an assetbundle
            /// </summary>
            /// <param name="fontFile">the assetbundle file</param>
            public static void Add(byte[]? fontFile) {
                var fonts = UnityEngine.AssetBundle.LoadFromMemory(fontFile).LoadAllAssets<TMPro.TMP_FontAsset>();
                foreach (var font in fonts) {
                    Fonts.Add(font);
                }
            }

            /// <summary>
            /// for adding an TMP_FontAsset directly
            /// </summary>
            /// <param name="fontAsset">The loaded fontasset</param>
            public static void Add(TMPro.TMP_FontAsset? fontAsset) {
                if(!Loaded) {
                    throw new InvalidOperationException($"{nameof(FontAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(FontAPI)})]");
                }
                _fontAssets.Add(fontAsset);
            }

            internal static List<TMPro.TMP_FontAsset> _fontAssets = new List<TMPro.TMP_FontAsset>();
        }
    }
}
