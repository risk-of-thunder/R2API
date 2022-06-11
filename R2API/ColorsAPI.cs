using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using R2API.ScriptableObjects;

namespace R2API {
    /// <summary>
    /// An API for adding colors to the <see cref="RoR2.ColorCatalog"/> and <see cref="RoR2.DamageColor"/> for use with DamageNumbers
    /// </summary>
    [R2APISubmodule]
    public static class ColorsAPI {
        private static List<DamageColorIndex> registeredDColorIndices = new List<DamageColorIndex>();
        private static List<SerializableDamageColor> addedSerializableDamageColors = new List<SerializableDamageColor>();

        private static List<ColorCatalog.ColorIndex> registeredColorIndices = new List<ColorCatalog.ColorIndex>();
        private static List<SerializableColor> addedSerializableColors = new List<SerializableColor>();

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        #region Hooks
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            //Color Catalog
            On.RoR2.ColorCatalog.GetColor += GetCustomColor;
            On.RoR2.ColorCatalog.GetColorHexString += GetCustomColorHexString;

            //Damage Color
            On.RoR2.DamageColor.FindColor += FindCustomColor;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            //Color Catalog
            On.RoR2.ColorCatalog.GetColor -= GetCustomColor;
            On.RoR2.ColorCatalog.GetColorHexString -= GetCustomColorHexString;

            //Damage Color
            On.RoR2.DamageColor.FindColor -= FindCustomColor;
        }
        #endregion

        #region Color Catalog Hook Implementation
        private static Color32 GetCustomColor(On.RoR2.ColorCatalog.orig_GetColor orig, RoR2.ColorCatalog.ColorIndex colorIndex) {
            return registeredColorIndices.Contains(colorIndex) ? ColorCatalog.indexToColor32[(int)colorIndex] : orig(colorIndex);
        }

        private static string GetCustomColorHexString(On.RoR2.ColorCatalog.orig_GetColorHexString orig, RoR2.ColorCatalog.ColorIndex colorIndex) {
            return registeredColorIndices.Contains(colorIndex) ? ColorCatalog.indexToHexString[(int)colorIndex] : orig(colorIndex);
        }
        #endregion

        #region Damage Color Hook Implementation
        private static Color FindCustomColor(On.RoR2.DamageColor.orig_FindColor orig, RoR2.DamageColorIndex dColorIndex) {
            return registeredDColorIndices.Contains(dColorIndex) ? DamageColor.colors[(int)dColorIndex] : orig(dColorIndex);
        }
        #endregion

        #region Damage Color Public Methods
        /// <summary>
        /// Adds a new DamageColor to the game
        /// </summary>
        /// <param name="color">The color for the new DamageColor</param>
        /// <returns>The <see cref="DamageColorIndex"/> associated with <paramref name="color"/></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static DamageColorIndex RegisterDamageColor(Color color) {
            if (!Loaded)
                throw new InvalidOperationException($"{nameof(ColorsAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}({nameof(ColorsAPI)})]");

            int nextColorIndex = DamageColor.colors.Length;
            DamageColorIndex newIndex = (DamageColorIndex)nextColorIndex;
            HG.ArrayUtils.ArrayAppend(ref DamageColor.colors, color);
            registeredDColorIndices.Add(newIndex);
            return newIndex;
        }

        /// <summary>
        /// Adds a new DamageColor using a <see cref="SerializableDamageColor"/>
        /// <para>The <see cref="DamageColorIndex"/> is set in <paramref name="serializableDamageColor"/>'s <see cref="SerializableDamageColor.DamageColorIndex"/> property</para>
        /// </summary>
        /// <param name="serializableDamageColor">The <see cref="SerializableDamageColor"/> to add</param>
        public static void AddSerializableDamageColor(SerializableDamageColor serializableDamageColor) {
            if (!Loaded)
                throw new InvalidOperationException($"{nameof(ColorsAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}({nameof(ColorsAPI)})]");

            if (addedSerializableDamageColors.Contains(serializableDamageColor)) {
                R2API.Logger.LogError($"Attempted to add SerializableDamageColor {serializableDamageColor} twice! aborting.");
                return;
            }

            serializableDamageColor.DamageColorIndex = RegisterDamageColor(serializableDamageColor.color);
        }
        #endregion

        #region Color Catalog Public Methods
        /// <summary>
        /// Adds a new Color to the game's <see cref="ColorCatalog"/>
        /// </summary>
        /// <param name="color">The color for the new Color for the <see cref="ColorCatalog"/></param>
        /// <returns>The <see cref="ColorCatalog.ColorIndex"/> associated with <paramref name="color"/></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ColorCatalog.ColorIndex RegisterColor(Color color) {
            if (!Loaded)
                throw new InvalidOperationException($"{nameof(ColorsAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}({nameof(ColorsAPI)})]");

            int nextColorIndex = ColorCatalog.indexToColor32.Length;
            ColorCatalog.ColorIndex newIndex = (ColorCatalog.ColorIndex)nextColorIndex;
            HG.ArrayUtils.ArrayAppend(ref ColorCatalog.indexToColor32, color);
            HG.ArrayUtils.ArrayAppend(ref ColorCatalog.indexToHexString, Util.RGBToHex(color));
            registeredColorIndices.Add(newIndex);
            return newIndex;
        }

        /// <summary>
        /// Adds a new Color to the <see cref="ColorCatalog"/> using a <see cref="SerializableColor"/>
        /// <para>The <see cref="ColorCatalog.ColorIndex"/> is set in <paramref name="serializableColor"/>'s <see cref="SerializableColor.ColorIndex"/></para>
        /// </summary>
        /// <param name="serializableColor">The <see cref="SerializableDamageColor"/> to add</param>
        public static void AddSerializableColor(SerializableColor serializableColor) {
            if (!Loaded)
                throw new InvalidOperationException($"{nameof(ColorsAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}({nameof(ColorsAPI)})]");

            if (addedSerializableColors.Contains(serializableColor)) {
                R2API.Logger.LogError($"Attempted to add SerializableColor {serializableColor} twice! aborting.");
                return;
            }

            serializableColor.ColorIndex = RegisterColor(serializableColor.color32);
        }
        #endregion
    }
}
