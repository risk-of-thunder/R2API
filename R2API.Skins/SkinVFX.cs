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
public static partial class SkinVFX
{
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

    private static bool hasCatalogInitOccured = false;


    internal static void SetHooks()
    {
        if (hooksSet)
        {
            return;
        }

        hooksSet = true;

        On.RoR2.EffectComponent.Start += ApplyModifier;
        On.RoR2.EffectManager.SpawnEffect_GameObject_EffectData_bool += ApplyReplacement;
        IL.RoR2.BulletAttack.FireSingle += ModifyBulletAttack;
        IL.RoR2.EffectManager.SimpleMuzzleFlash += ModifyMuzzleFlash;
        On.EntityStates.BasicMeleeAttack.BeginMeleeAttackEffect += ModifyGenericMelee;

        IL.RoR2.Orbs.GenericDamageOrb.Begin += ModifyGenericOrb;
    }

    internal static void UnsetHooks()
    {
        hooksSet = false;
        On.RoR2.EffectComponent.Start -= ApplyModifier;
        On.RoR2.EffectManager.SpawnEffect_GameObject_EffectData_bool -= ApplyReplacement;
        IL.RoR2.BulletAttack.FireSingle -= ModifyBulletAttack;
        IL.RoR2.EffectManager.SimpleMuzzleFlash -= ModifyMuzzleFlash;
        On.EntityStates.BasicMeleeAttack.BeginMeleeAttackEffect -= ModifyGenericMelee;

        IL.RoR2.Orbs.GenericDamageOrb.Begin -= ModifyGenericOrb;
    }

    [SystemInitializer(typeof(EffectCatalog))]
    private static void FindEffectIndexes()
    {
        hasCatalogInitOccured = true;

        for (int i = 0; i < skinVFXInfos.Count; i++)
        {
            SkinVFXInfo skinVFXInfo = skinVFXInfos[i];

            if (skinVFXInfo.EffectPrefab)
            {
                skinVFXInfo.TargetEffect = EffectCatalog.FindEffectIndexFromPrefab(skinVFXInfo.EffectPrefab);
                continue;
            }

            if (!String.IsNullOrEmpty(skinVFXInfo.EffectString))
            {
                EffectDef def = EffectCatalog.entries.FirstOrDefault(effectDef => effectDef.prefabName == skinVFXInfo.EffectString);

                if (def == null)
                {
                    SkinsPlugin.Logger.LogError($"Failed to find effect {skinVFXInfo.EffectString} for SkinVFXInfo!");
                    continue;
                }

                skinVFXInfo.TargetEffect = def.index;
            }
        }
    }

    private static SkinVFXInfo FindSkinVFXInfo(uint identifier)
    {
        if (identifier < BaseIdentifier || identifier >= (skinVFXInfos.Count + BaseIdentifier))
        {
            return null;
        }
        return skinVFXInfos[(int)(identifier - BaseIdentifier)];
    }

    private static SkinVFXInfo FindSkinVFXInfo(GameObject attacker, GameObject effectPrefab)
    {
        if (!attacker || !effectPrefab)
        {
            return null;
        }

        SkinDef skinDef = SkinCatalog.FindCurrentSkinDefForBodyInstance(attacker);
        EffectIndex index = EffectCatalog.FindEffectIndexFromPrefab(effectPrefab);

        return skinVFXInfos.FirstOrDefault(skinVFXInfo => skinVFXInfo.RequiredSkin == skinDef && (index == EffectIndex.Invalid ? skinVFXInfo.EffectPrefab == effectPrefab : skinVFXInfo.TargetEffect == index));
    }

    private static void ModifyGenericMelee(On.EntityStates.BasicMeleeAttack.orig_BeginMeleeAttackEffect orig, EntityStates.BasicMeleeAttack self)
    {
        SkinVFXInfo info = FindSkinVFXInfo(self.gameObject, self.swingEffectPrefab);
        bool first = !self.swingEffectInstance;

        if (info != null && info.ReplacementEffectPrefab)
        {
            self.swingEffectPrefab = info.ReplacementEffectPrefab;
        }

        orig(self);

        if (first && info != null && info.OnEffectSpawned != null && self.swingEffectInstance)
        {
            info.OnEffectSpawned(self.swingEffectInstance);
        }
    }

    private static void ApplyReplacement(On.RoR2.EffectManager.orig_SpawnEffect_GameObject_EffectData_bool orig, GameObject effectPrefab, EffectData effectData, bool transmit)
    {
        if (effectData == null)
        {
            orig(effectPrefab, effectData, transmit);
            return;
        }

        if (effectData.genericUInt < BaseIdentifier)
        {
            orig(effectPrefab, effectData, transmit);
            return;
        }

        SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(effectData.genericUInt);

        if (skinVFXInfo == null)
        {
            orig(effectPrefab, effectData, transmit);
            return;
        }

        if (skinVFXInfo.ReplacementEffectPrefab != null)
        {
            orig(skinVFXInfo.ReplacementEffectPrefab, effectData, transmit);
            return;
        }

        orig(effectPrefab, effectData, transmit);
    }

    private static void ApplyModifier(On.RoR2.EffectComponent.orig_Start orig, EffectComponent self)
    {
        orig(self);

        if (self.effectData == null) return;
        if (self.effectData.genericUInt < BaseIdentifier) return;

        SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(self.effectData.genericUInt);

        if (skinVFXInfo == null) return;

        skinVFXInfo.OnEffectSpawned?.Invoke(self.gameObject);
    }
    private static void ModifyMuzzleFlash(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetChildLocatorTransformReference))
        );

        if (!found)
        {
            SkinsPlugin.Logger.LogError($"Failed to apply SkinVFX IL hook on EffectManager.SimpleMuzzleFlash");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.Emit(OpCodes.Ldloc, 4);
        c.EmitDelegate<Action<GameObject, GameObject, EffectData>>((effectPrefab, owner, data) =>
        {
            SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(owner, effectPrefab);

            if (skinVFXInfo == null)
            {
                return;
            }

            data.genericUInt = skinVFXInfo.Identifier;
        });
    }

    private static void ModifyGenericOrb(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetHurtBoxReference))
        );

        if (!found)
        {
            SkinsPlugin.Logger.LogError($"Failed to apply SkinVFX IL hook on {il.Method.DeclaringType}.{il.Method.Name}");
            return;
        }

        c.Emit(OpCodes.Ldloc_0);
        c.Emit(OpCodes.Ldarg_0);

        c.EmitDelegate<Action<EffectData, GenericDamageOrb>>((data, orb) =>
        {
            if (data == null) return;
            if (!orb.attacker) return;

            SkinVFXInfo info = FindSkinVFXInfo(orb.attacker, orb.GetOrbEffect());

            if (info == null) return;

            data.genericUInt = info.Identifier;
        });
    }

    private static void ModifyBulletAttack(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetChildLocatorTransformReference))
        );

        if (!found)
        {
            SkinsPlugin.Logger.LogError($"Failed to apply SkinVFX IL hook on BulletAttack.FireSingle");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<BulletAttack>>((bulletAttack) =>
        {
            SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(bulletAttack.owner, bulletAttack.tracerEffectPrefab);

            if (skinVFXInfo == null) return;

            BulletAttack._effectData.genericUInt = skinVFXInfo.Identifier;
        });
    }
}
