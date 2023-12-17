using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using RoR2.Orbs;

namespace R2API;

/// <summary>
/// Class for adding skin-specific effect replacements for SkinDefs.
/// </summary>
public static class SkinVFX {
    private static List<SkinVFXInfo> skinVFXInfos = new List<SkinVFXInfo>();
    private static bool hooksSet = false;
    private const uint BaseIdentifier = 24000; // arbitrary, but we shouldn't hit 24,000 unique items for a substantial amount of time.
    private static uint currentIdentifier = BaseIdentifier; 
    private static uint nextIdentifier => currentIdentifier++;

    /// <summary>
    /// Called when a skin-specific effect is ready to be modified.
    /// </summary>
    /// <param name="spawnedEffect">The effect that was spawned.</param>
    public delegate void OnEffectSpawnedDelegate(GameObject spawnedEffect);

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

        AddSkinVFX(ref skinVFXInfo);

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

        AddSkinVFX(ref skinVFXInfo);

        return skinVFXInfo;
    }

    /// <summary>
    /// Adds a skin-specific effect replacement.
    /// </summary>
    /// <param name="skinVFXInfo">The SkinVFXInfo to register. Its Identifier field will be automatically assigned by this method.</param>
    /// <returns>True on success, false otherwise.</returns>
    public static bool AddSkinVFX(ref SkinVFXInfo skinVFXInfo) {
        SetHooks();

        if (skinVFXInfo.RequiredSkin == null) {
            SkinsPlugin.Logger.LogError($"Cannot add a SkinVFXInfo with no assigned SkinDef.");
            return false;
        }

        if (skinVFXInfo.TargetEffect == EffectIndex.Invalid) {
            SkinsPlugin.Logger.LogError($"SkinVFXInfo may not have a TargetEffect of EffectIndex.Invalid");
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

    internal static void SetHooks() {
        if (hooksSet) {
            return;
        }

        hooksSet = true;

        On.RoR2.EffectComponent.Start += ApplyModifier;
        On.RoR2.EffectManager.SpawnEffect_GameObject_EffectData_bool += ApplyReplacement;
        IL.RoR2.BulletAttack.FireSingle += ModifyBulletAttack;

        IL.RoR2.Orbs.GenericDamageOrb.Begin += ModifyGenericOrb;
    }

    internal static void UnsetHooks() {
        hooksSet = false;
        On.RoR2.EffectComponent.Start -= ApplyModifier;
        On.RoR2.EffectManager.SpawnEffect_GameObject_EffectData_bool -= ApplyReplacement;

        IL.RoR2.Orbs.GenericDamageOrb.Begin -= ModifyGenericOrb;
    }

    private static SkinVFXInfo FindSkinVFXInfo(uint identifier) {
        return skinVFXInfos.FirstOrDefault(skinVFXInfo => skinVFXInfo.Identifier == identifier);
    }

    private static SkinVFXInfo FindSkinVFXInfo(GameObject attacker, GameObject effectPrefab) {
        if (!attacker || !effectPrefab) {
            return default(SkinVFXInfo);
        }

        SkinDef skinDef = SkinCatalog.FindCurrentSkinDefForBodyInstance(attacker);
        EffectIndex index = EffectCatalog.FindEffectIndexFromPrefab(effectPrefab);

        return skinVFXInfos.FirstOrDefault(skinVFXInfo => skinVFXInfo.RequiredSkin == skinDef && skinVFXInfo.TargetEffect == index);
    }

    private static void ApplyReplacement(On.RoR2.EffectManager.orig_SpawnEffect_GameObject_EffectData_bool orig, GameObject effectPrefab, EffectData effectData, bool transmit)
    {
        if (effectData == null) {
            orig(effectPrefab, effectData, transmit);
            return;
        }

        if (effectData.genericUInt < BaseIdentifier) {
            orig(effectPrefab, effectData, transmit);
            return;
        }

        SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(effectData.genericUInt);

        if (skinVFXInfo.Identifier == uint.MaxValue) {
            orig(effectPrefab, effectData, transmit);
            return;
        }

        if (skinVFXInfo.ReplacementEffectPrefab != null) {
            orig(skinVFXInfo.ReplacementEffectPrefab, effectData, transmit);
            return;
        }

        orig(effectPrefab, effectData, transmit);
    }

    private static void ApplyModifier(On.RoR2.EffectComponent.orig_Start orig, EffectComponent self) {
        orig(self);

        if (self.effectData == null) return;
        if (self.effectData.genericUInt < BaseIdentifier) return;

        SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(self.effectData.genericUInt);

        if (skinVFXInfo.Identifier == uint.MaxValue) return;

        skinVFXInfo.OnEffectSpawned?.Invoke(self.gameObject);
    }

    private static void ModifyGenericOrb(ILContext il) {
        ILCursor c = new ILCursor(il);

        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetHurtBoxReference))
        );

        if (!found) {
            SkinsPlugin.Logger.LogError($"Failed to apply SkinVFX IL hook on {il.Method.DeclaringType}.{il.Method.Name}");
            return;
        }

        c.Emit(OpCodes.Ldloc_0);
        c.Emit(OpCodes.Ldarg_0);

        c.EmitDelegate<Action<EffectData, GenericDamageOrb>>((data, orb) => {
            if (data == null) return;
            if (!orb.attacker) return;

            SkinVFXInfo info = FindSkinVFXInfo(orb.attacker, orb.GetOrbEffect());

            if (info.Identifier == uint.MaxValue) return;

            data.genericUInt = info.Identifier;
        });
    }

    private static void ModifyBulletAttack(ILContext il) {
        ILCursor c = new ILCursor(il);

        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetChildLocatorTransformReference))
        );

        if (!found) {
            SkinsPlugin.Logger.LogError($"Failed to apply SkinVFX IL hook on BulletAttack.FireSingle");
            return;
        }

        c.Emit(OpCodes.Ldloc_S, (byte)14);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<EffectData, BulletAttack>>((effectData, bulletAttack) => {
            SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(bulletAttack.owner, bulletAttack.tracerEffectPrefab);

            if (skinVFXInfo.Identifier == uint.MaxValue) return;

            effectData.genericUInt = skinVFXInfo.Identifier;
        });
    }
}