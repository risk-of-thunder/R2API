using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.ScriptableObjects;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API;

[AutoVersion]
public static partial class ColorsAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".colors";
    public const string PluginName = R2API.PluginName + ".Colors";

    private static List<SerializableDamageColor> addedSerializableDamageColors = new List<SerializableDamageColor>();
    private static List<SerializableColorCatalogEntry> addedSerializableColors = new List<SerializableColorCatalogEntry>();

    [Obsolete(R2APISubmoduleDependency.propertyObsolete)]
    public static bool Loaded => true;

    private static bool _hookEnabled = false;

    #region Hooks
    internal static void SetHooks()
    {
        if (_hookEnabled)
        {
            return;
        }

        //Color Catalog
        IL.RoR2.ColorCatalog.GetColor += GetColorIL;
        IL.RoR2.ColorCatalog.GetColorHexString += GetColorIL;

        //DamageColor
        IL.RoR2.DamageColor.FindColor += GetColorIL;

        _hookEnabled = true;
    }

    internal static void UnsetHooks()
    {
        //Color Catalog
        IL.RoR2.ColorCatalog.GetColor -= GetColorIL;
        IL.RoR2.ColorCatalog.GetColorHexString -= GetColorIL;

        //DamageColor
        IL.RoR2.DamageColor.FindColor -= GetColorIL;

        _hookEnabled = false;
    }

    private static void GetColorIL(ILContext il)
    {
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
    public static DamageColorIndex RegisterDamageColor(Color color)
    {
        ColorsAPI.SetHooks();
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
    public static void AddSerializableDamageColor(SerializableDamageColor serializableDamageColor)
    {
        ColorsAPI.SetHooks();
        if (addedSerializableDamageColors.Contains(serializableDamageColor))
        {
            ColorsPlugin.Logger.LogError($"Attempted to add SerializableDamageColor {serializableDamageColor} twice! aborting.");
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
    public static ColorCatalog.ColorIndex RegisterColor(Color color)
    {
        ColorsAPI.SetHooks();
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
    public static void AddSerializableColor(SerializableColorCatalogEntry serializableColor)
    {
        ColorsAPI.SetHooks();
        if (addedSerializableColors.Contains(serializableColor))
        {
            ColorsPlugin.Logger.LogError($"Attempted to add SerializableColor {serializableColor} twice! aborting.");
            return;
        }

        serializableColor.ColorIndex = RegisterColor(serializableColor.color32);
    }
    #endregion
}
