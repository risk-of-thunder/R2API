using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.ScriptableObjects;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace R2API {
    /// <summary>
    /// An API for adding colors to the <see cref="RoR2.ColorCatalog"/> and <see cref="RoR2.DamageColor"/> for use with DamageNumbers
    /// </summary>
    [R2APISubmodule]
    public static class ColorsAPI {
        private static List<SerializableDamageColor> addedSerializableDamageColors = new List<SerializableDamageColor>();
        private static List<SerializableColorCatalogEntry> addedSerializableColors = new List<SerializableColorCatalogEntry>();

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
            IL.RoR2.ColorCatalog.GetColor += GetColorIL;
            IL.RoR2.ColorCatalog.GetColorHexString += GetColorIL;

            //DamageColor
            IL.RoR2.DamageColor.FindColor += GetColorIL;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            //Color Catalog
            IL.RoR2.ColorCatalog.GetColor -= GetColorIL;
            IL.RoR2.ColorCatalog.GetColorHexString -= GetColorIL;

            //DamageColor
            IL.RoR2.DamageColor.FindColor -= GetColorIL;
        }

        private static void GetColorIL(ILContext il) {
            var c = new ILCursor(il);
            Mono.Cecil.FieldReference arrayToGetLength = null;
            //Get the array (indexToColor32/ indexToHexString)
            c.GotoNext(inst => inst.MatchLdsfld(out arrayToGetLength));
            //Move to right before where ColorIndex.Count is loaded
            c.GotoPrev(MoveType.After, inst => inst.MatchLdarg(0));
            //replace with loadArray (from previously)
            c.Emit(OpCodes.Ldsfld, arrayToGetLength);
            //get array length
            c.Emit(OpCodes.Ldlen);
            //type safety?
            c.Emit(OpCodes.Conv_I4);
            //Remove the old stuff.
            c.Index++;
            c.Emit(OpCodes.Pop);
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
            return newIndex;
        }

        /// <summary>
        /// Adds a new Color to the <see cref="ColorCatalog"/> using a <see cref="SerializableColorCatalogEntry"/>
        /// <para>The <see cref="ColorCatalog.ColorIndex"/> is set in <paramref name="serializableColor"/>'s <see cref="SerializableColorCatalogEntry.ColorIndex"/></para>
        /// </summary>
        /// <param name="serializableColor">The <see cref="SerializableDamageColor"/> to add</param>
        public static void AddSerializableColor(SerializableColorCatalogEntry serializableColor) {
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
