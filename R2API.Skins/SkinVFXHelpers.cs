using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using RoR2.Orbs;


namespace R2API;

// this is partial to avoid cluttering with the multiple overloads of AddSkinVFX
public static partial class SkinVFX {

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinDef">The SkinDef that should be required for the replacement to occur.</param>
    /// <param name="targetEffect">The EffectIndex of the effect that should be replaced.</param>
    /// <param name="replacementPrefab">A replacement prefab to spawn instead of the effect. To modify the normal prefab, see the overload with OnEffectSpawnedDelegate.</param>
    /// <returns>The SkinVFXInfo created from the input.</returns>
    public static SkinVFXInfo AddSkinVFX(SkinDef skinDef, EffectIndex targetEffect, GameObject replacementPrefab) {
        SetHooks();

        SkinVFXInfo skinVFXInfo = new SkinVFXInfo {
            RequiredSkin = skinDef,
            TargetEffect = targetEffect,
            ReplacementEffectPrefab = replacementPrefab,
        };

        AddSkinVFX(skinVFXInfo);

        return skinVFXInfo;
    }

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinDef">The SkinDef that should be required for the replacement to occur.</param>
    /// <param name="targetEffect">The name of the effect that should be replaced.</param>
    /// <param name="replacementPrefab">A delegate that will be called when the effect is spawned by a character with a matching SkinDef.</param>
    /// <returns>The SkinVFXInfo created from the input.</returns>
    public static SkinVFXInfo AddSkinVFX(SkinDef skinDef, string targetEffect, GameObject replacementPrefab) {
        SetHooks();

        SkinVFXInfo skinVFXInfo = new SkinVFXInfo {
            RequiredSkin = skinDef,
            EffectString = targetEffect,
            ReplacementEffectPrefab = replacementPrefab,
        };

        AddSkinVFX(skinVFXInfo);

        return skinVFXInfo;
    }

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinDef">The SkinDef that should be required for the replacement to occur.</param>
    /// <param name="targetEffect">The the effect that should be replaced.</param>
    /// <param name="replacementPrefab">A delegate that will be called when the effect is spawned by a character with a matching SkinDef.</param>
    /// <returns>The SkinVFXInfo created from the input.</returns>
    public static SkinVFXInfo AddSkinVFX(SkinDef skinDef, GameObject targetEffect, GameObject replacementPrefab) {
        SetHooks();

        SkinVFXInfo skinVFXInfo = new SkinVFXInfo {
            RequiredSkin = skinDef,
            EffectPrefab = targetEffect,
            ReplacementEffectPrefab = replacementPrefab,
        };

        AddSkinVFX(skinVFXInfo);

        return skinVFXInfo;
    }

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinDef">The SkinDef that should be required for the replacement to occur.</param>
    /// <param name="targetEffect">The EffectIndex of the effect that should be replaced.</param>
    /// <param name="onEffectSpawned">A delegate that will be called when the effect is spawned by a character with a matching SkinDef.</param>
    /// <returns>The SkinVFXInfo created from the input.</returns>
    public static SkinVFXInfo AddSkinVFX(SkinDef skinDef, EffectIndex targetEffect, OnEffectSpawnedDelegate onEffectSpawned) {
        SetHooks();

        SkinVFXInfo skinVFXInfo = new SkinVFXInfo {
            RequiredSkin = skinDef,
            TargetEffect = targetEffect,
            OnEffectSpawned = onEffectSpawned,
        };

        AddSkinVFX(skinVFXInfo);

        return skinVFXInfo;
    }

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinDef">The SkinDef that should be required for the replacement to occur.</param>
    /// <param name="targetEffect">Name of the effect that should be replaced.</param>
    /// <param name="onEffectSpawned">A delegate that will be called when the effect is spawned by a character with a matching SkinDef.</param>
    /// <returns>The SkinVFXInfo created from the input.</returns>
    public static SkinVFXInfo AddSkinVFX(SkinDef skinDef, string targetEffect, OnEffectSpawnedDelegate onEffectSpawned) {
        SetHooks();

        SkinVFXInfo skinVFXInfo = new SkinVFXInfo {
            RequiredSkin = skinDef,
            EffectString = targetEffect,
            OnEffectSpawned = onEffectSpawned,
        };

        AddSkinVFX(skinVFXInfo);

        return skinVFXInfo;
    }

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinDef">The SkinDef that should be required for the replacement to occur.</param>
    /// <param name="targetEffect">The effect prefab that should be replaced.</param>
    /// <param name="onEffectSpawned">A delegate that will be called when the effect is spawned by a character with a matching SkinDef.</param>
    /// <returns>The SkinVFXInfo created from the input.</returns>
    public static SkinVFXInfo AddSkinVFX(SkinDef skinDef, GameObject targetEffect, OnEffectSpawnedDelegate onEffectSpawned) {
        SetHooks();

        SkinVFXInfo skinVFXInfo = new SkinVFXInfo {
            RequiredSkin = skinDef,
            EffectPrefab = targetEffect,
            OnEffectSpawned = onEffectSpawned,
        };

        AddSkinVFX(skinVFXInfo);

        return skinVFXInfo;
    }

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinVFXInfo">The SkinVFXInfo to register. Its Identifier field will be automatically assigned by this method.</param>
    /// <returns>True on success, false otherwise.</returns>
    public static bool AddSkinVFX(SkinVFXInfo skinVFXInfo) {
        SetHooks();

        if (hasCatalogInitOccured && skinVFXInfo.EffectPrefab != null) {
            skinVFXInfo.TargetEffect = EffectCatalog.FindEffectIndexFromPrefab(skinVFXInfo.EffectPrefab);

            EffectDef def = EffectCatalog.entries.FirstOrDefault(effectDef => effectDef.prefabName == skinVFXInfo.EffectString);

            if (def == null) {
                SkinsPlugin.Logger.LogError($"Failed to find effect {skinVFXInfo.EffectString} for SkinVFXInfo!");
                return false;
            }

            skinVFXInfo.TargetEffect = def.index;
        }

        if (skinVFXInfo.RequiredSkin == null) {
            SkinsPlugin.Logger.LogError($"Cannot add a SkinVFXInfo with no assigned SkinDef.");
            return false;
        }

        if (skinVFXInfo.TargetEffect == EffectIndex.Invalid && skinVFXInfo.EffectPrefab == null && skinVFXInfo.EffectString == null) {
            SkinsPlugin.Logger.LogError($"SkinVFXInfo may not have a TargetEffect of EffectIndex.Invalid, or must also specify an EffectPrefab or EffectString.");
            return false;
        }

        if (skinVFXInfo.ReplacementEffectPrefab == null && skinVFXInfo.OnEffectSpawned == null) {
            SkinsPlugin.Logger.LogError($"SkinVFXInfo must have either a ReplacementEffectPrefab or an OnEffectSpawnedDelegate assigned.");
            return false;
        }

        skinVFXInfo.Identifier = nextIdentifier;
        skinVFXInfos.Add(skinVFXInfo);

        return true;
    }
}