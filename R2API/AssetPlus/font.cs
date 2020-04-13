using System.Collections.Generic;

namespace R2API.AssetPlus {
    internal static class FontPlus
    {
        internal static void FontAwake()
        {
            var fontFiles = AssetPlus.GetFiles("font");

            foreach (var fontFile in fontFiles)
            {
                Fonts.Add(fontFile);
            }

            On.RoR2.UI.HGTextMeshProUGUI.OnCurrentLanguageChanged += HGTextMeshProUGUI_OnCurrentLanguageChanged;
            On.RoR2.Language.SetCurrentLanguage += Language_SetCurrentLanguage;
        }

        private static void Language_SetCurrentLanguage(On.RoR2.Language.orig_SetCurrentLanguage orig, string language)
        {
            orig(language);
            if(Fonts.fontAssets.Count != 0)
            {
                RoR2.UI.HGTextMeshProUGUI.defaultLanguageFont = Fonts.fontAssets[0];
            }
        }

        /// <summary>
        /// Checks for for amount of fonts after all Awake()
        /// </summary>
        internal static void FontStart()
        {
            if (Fonts.fontAssets.Count > 1)
            {
                UnityEngine.Debug.LogWarning("multiple fonts as custom font, loading the first added");
            }
        }

        private static void HGTextMeshProUGUI_OnCurrentLanguageChanged(On.RoR2.UI.HGTextMeshProUGUI.orig_OnCurrentLanguageChanged orig)
        {
            if (Fonts.fontAssets.Count != 0)
            {
                RoR2.UI.HGTextMeshProUGUI.defaultLanguageFont = Fonts.fontAssets[0];
            }
        }
    }

    /// <summary>
    /// use this class to add fonts
    /// </summary>
    public static class Fonts
    {
        /// <summary>
        /// for adding an TMP_FontAsset inside an seperate assetbundle (.font is loaded automatically)
        /// </summary>
        /// <param name="path">absolute path to the assetbundle</param>
        public static void Add(string path)
        {
            var fontBundle = UnityEngine.AssetBundle.LoadFromFile(path);
            var fonts = fontBundle.LoadAllAssets<TMPro.TMP_FontAsset>();
            foreach (var font in fonts)
            {
                Add(font);
            }
        }


        /// <summary>
        /// for adding an TMP_FontAsset while it is still in an assetbundle
        /// </summary>
        /// <param name="fontFile">the assetbundle file</param>
        public static void Add(byte[] fontFile)
        {
            var fonts = UnityEngine.AssetBundle.LoadFromMemory(fontFile).LoadAllAssets<TMPro.TMP_FontAsset>();
            foreach (var font in fonts)
            {
                fontAssets.Add(font);
            }
        }

        /// <summary>
        /// for adding an TMP_FontAsset directly
        /// </summary>
        /// <param name="fontAsset">The loaded fontasset</param>
        public static void Add(TMPro.TMP_FontAsset fontAsset)
        {
            fontAssets.Add(fontAsset);
        }

        internal static List<TMPro.TMP_FontAsset> fontAssets = new List<TMPro.TMP_FontAsset>();
    }
}
