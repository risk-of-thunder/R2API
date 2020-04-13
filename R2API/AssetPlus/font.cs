using System;
using System.Collections.Generic;

namespace R2API.AssetPlus {
    /// <summary>
    /// use this class to add fonts
    /// </summary>
    [Obsolete("Moved to R2API/FontAPI/")]
    public static class Fonts
    {
        /// <summary>
        /// for adding an TMP_FontAsset inside an seperate assetbundle (.font is loaded automatically)
        /// </summary>
        /// <param name="path">absolute path to the assetbundle</param>
        [Obsolete("Moved to R2API/FontAPI/Fonts")]
        public static void Add(string path)
        {
            FontAPI.Fonts.Add(path);
        }


        /// <summary>
        /// for adding an TMP_FontAsset while it is still in an assetbundle
        /// </summary>
        /// <param name="fontFile">the assetbundle file</param>
        [Obsolete("Moved to R2API/FontAPI/Fonts")]
        public static void Add(byte[] fontFile)
        {
            FontAPI.Fonts.Add(fontFile);
        }

        /// <summary>
        /// for adding an TMP_FontAsset directly
        /// </summary>
        /// <param name="fontAsset">The loaded fontasset</param>
        [Obsolete("Moved to R2API/FontAPI/Fonts")]
        public static void Add(TMPro.TMP_FontAsset fontAsset)
        {
            FontAPI.Fonts.Add(fontAsset);
        }
    }
}
