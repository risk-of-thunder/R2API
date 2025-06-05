using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using RoR2BepInExPack.GameAssetPaths;
using UnityEngine;

namespace R2API;

/// <summary>
/// API for adding various stuff to Character Body such as: Modded Body Flags
/// </summary>

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type

public static partial class CharacterBodyAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".character_body";
    public const string PluginName = R2API.PluginName + ".CharacterBody";

    internal static void SetHooks()
    {   
        if (_hooksEnabled) return;
        _hooksEnabled = true;
        //IL.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        //IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        //IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess1;
    }
    

    /// <summary>
    /// Used as the delegate type for the GetDamageSourceCoefficients event.
    /// </summary>
    /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
    public delegate void DamageSourceHookEventHandler(CharacterBody sender);

    /// <summary>
    /// Subscribe to this event to properly modify new damage source fields after all fields have been reset by RecalculateStats. Fired during CharacterBody.RecalculateStats.
    /// </summary>
    public static event DamageSourceHookEventHandler GetDamageSourceCoefficients;
    internal static void UnsetHooks()
    {
        if (!_hooksEnabled) return;
        _hooksEnabled = false;
        //IL.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
        //IL.RoR2.HealthComponent.TakeDamageProcess -= HealthComponent_TakeDamageProcess;
        //IL.RoR2.HealthComponent.TakeDamageProcess -= HealthComponent_TakeDamageProcess1;
    }
    public enum ModdedBodyFlag { };
    /// <summary>
    /// Reserve ModdedDamageType to use it with
    /// <see cref="AddModdedBodyFlag(CharacterBody, ModdedBodyFlag)"/>,
    /// <see cref="RemoveModdedBodyFlag(CharacterBody, ModdedBodyFlag)"/> and
    /// <see cref="HasModdedBodyFlag(CharacterBody, ModdedBodyFlag))"/>
    /// </summary>
    /// <returns></returns>
    public static ModdedBodyFlag ReserveDamageType()
    {
        SetHooks();
        if (ModdedBodyFlagCount >= CompressedFlagArrayUtilities.sectionsCount * CompressedFlagArrayUtilities.flagsPerSection)
        {
            //I doubt this is ever gonna happen, but just in case.
            // Upper comment is not mine
            throw new IndexOutOfRangeException($"Reached the limit of {CompressedFlagArrayUtilities.sectionsCount * CompressedFlagArrayUtilities.flagsPerSection} ModdedBodyFlags. Please contact R2API developers to increase the limit");
        }

        ModdedBodyFlagCount++;

        return (ModdedBodyFlag)ModdedBodyFlagCount;
    }
    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;
    private static bool _hooksEnabled = false;
    /// <summary>
    /// Reserved ModdedBodyFlagCount count
    /// </summary>
    public static int ModdedBodyFlagCount { get; private set; }
    /// <summary>
    /// Adding ModdedBodyFlag to CharacterBody. You can add more than one body flag to one CharacterBody
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="moddedBodyFlag"></param>
    public static void AddModdedBodyFlag(this CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag) => AddModdedBodyFlagInternal(characterBody, moddedBodyFlag);
    /// <summary>
    /// Removing ModdedBodyFlag from CharacterBody instance.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="moddedBodyFlag"></param>
    public static bool RemoveModdedBodyFlag(this CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag) => RemoveModdedDamageTypeInternal(characterBody, moddedBodyFlag);
    /// <summary>
    /// Checks if CharacterBody instance has any ModdedBodyFlag assigned. One CharacterBody can have more than one body flag.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <returns></returns>
    public static bool HasAnyModdedBodyFlag(this CharacterBody characterBody)
    {
        SetHooks();

        var bodtFlags = CharacterBodyInterop.GetModdedBodyFlags(characterBody);
        return bodtFlags is not null && bodtFlags.Length > 0;
    }

    /// <summary>
    /// Checks if CharacterBody instance has ModdedBodyFlag assigned. One CharacterBody can have more than one body flag.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="moddedBodyFlag"></param>
    /// <returns></returns>
    public static bool HasModdedBodyFlag(this CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag) => HasModdedDamageTypeInternal(characterBody, moddedBodyFlag);
    private static void AddModdedBodyFlagInternal(CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag)
    {
        SetHooks();
        
        if (!CheckRange(characterBody, moddedBodyFlag)) return;

        var bodyFlags = CharacterBodyInterop.GetModdedBodyFlags(characterBody);
        CompressedFlagArrayUtilities.AddImmutable(ref bodyFlags, (int)moddedBodyFlag - 1);
        CharacterBodyInterop.SetModdedBodyFlags(characterBody, bodyFlags);
    }
    private static bool RemoveModdedDamageTypeInternal(CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag)
    {
        SetHooks();

        if (!CheckRange(characterBody, moddedBodyFlag)) return false;

        var bodyFlags = CharacterBodyInterop.GetModdedBodyFlags(characterBody);
        var removed = CompressedFlagArrayUtilities.RemoveImmutable(ref bodyFlags, (int)moddedBodyFlag - 1);
        CharacterBodyInterop.SetModdedBodyFlags(characterBody, bodyFlags);
        return removed;
    }
    private static bool HasModdedDamageTypeInternal(CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag)
    {
        SetHooks();

        if (!CheckRange(characterBody, moddedBodyFlag)) return false;

        var bodyFlags = CharacterBodyInterop.GetModdedBodyFlags(characterBody);
        return CompressedFlagArrayUtilities.Has(bodyFlags, (int)moddedBodyFlag - 1);
    }
    private static bool CheckRange(CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag)
    {
        if ((int)moddedBodyFlag > ModdedBodyFlagCount || (int)moddedBodyFlag < 1)
        {
            CharacterBodyPlugin.Logger.LogError($"Parameter '{nameof(moddedBodyFlag)}' with value {moddedBodyFlag} is out of range of registered types (1-{ModdedBodyFlagCount})\n{new StackTrace(true)}");
            return false;
        }
        return true;
    }
    // TODO: Implement this later
    /*
    private static void HealthComponent_TakeDamageProcess1(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (
            c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<HealthComponent>(nameof(HealthComponent.isShieldRegenForced))
            ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            CharacterBodyPlugin.Logger.LogMessage("Loaded HealthComponent_TakeDamageProcess1 hook");
            c.EmitDelegate(UseAdditiveDamageSourceValues);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    
    private static void HealthComponent_TakeDamageProcess(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (
            c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HealthComponent>(nameof(HealthComponent.ospTimer))
            ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            CharacterBodyPlugin.Logger.LogMessage("Loaded HealthComponent_TakeDamageProcess hook");
            c.EmitDelegate(UseMutiplicativeDamageSourceValues);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
    }

    private static void CharacterBody_RecalculateStats(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (
            c.TryGotoNext(
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(57)
            ))
        {
            c.Emit(OpCodes.Ldarg_0);
            CharacterBodyPlugin.Logger.LogMessage("Loaded CharacterBody_RecalculateStats hook");
            c.EmitDelegate(SetDamageSourceValues);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    private static void UseMutiplicativeDamageSourceValues(HealthComponent healthComponent, ref DamageInfo damageInfo)
    {
        CharacterBodyPlugin.Logger.LogMessage("UseMutiplicativeDamageSourceValues");
        CharacterBodyPlugin.Logger.LogMessage("damage before: " + damageInfo.damage);
        damageInfo.damage *= healthComponent.body.GetDamageSourceVulnerabilityMultiplier(damageInfo.damageType.damageSource);
        CharacterBody attackerBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
        if (attackerBody != null)
        {
            damageInfo.damage *= attackerBody.GetDamageSourceDamageMultiplier(damageInfo.damageType.damageSource);
        }
        CharacterBodyPlugin.Logger.LogMessage("damage after: " + damageInfo.damage);
    }
    private static void UseAdditiveDamageSourceValues(HealthComponent healthComponent, DamageInfo damageInfo)
    {
        CharacterBodyPlugin.Logger.LogMessage("UseAdditiveDamageSourceValues");
        CharacterBodyPlugin.Logger.LogMessage("damage before: " + damageInfo.damage);
        damageInfo.damage += healthComponent.body.GetDamageSourceVulnerabilityAddition(damageInfo.damageType.damageSource);
        CharacterBody attackerBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
        if (attackerBody != null)
        {
            damageInfo.damage += attackerBody.GetDamageSourceDamageAddition(damageInfo.damageType.damageSource);
        }
        CharacterBodyPlugin.Logger.LogMessage("damage after: " + damageInfo.damage);
    }
    private static void SetDamageSourceValues(CharacterBody characterBody)
    {
        CharacterBodyAPI.SetHooks();
        characterBody.SetDamageSourceDamageAddition(DamageSource.Primary, 0f);
        characterBody.SetDamageSourceDamageAddition(DamageSource.Secondary, 0f);
        characterBody.SetDamageSourceDamageAddition(DamageSource.Utility, 0f);
        characterBody.SetDamageSourceDamageAddition(DamageSource.Special, 0f);
        characterBody.SetDamageSourceDamageAddition(DamageSource.DOT, 0f);
        characterBody.SetDamageSourceDamageAddition(DamageSource.Hazard, 0f);
        characterBody.SetDamageSourceDamageMultiplier(DamageSource.Primary, 1f);
        characterBody.SetDamageSourceDamageMultiplier(DamageSource.Secondary, 1f);
        characterBody.SetDamageSourceDamageMultiplier(DamageSource.Utility, 1f);
        characterBody.SetDamageSourceDamageMultiplier(DamageSource.Special, 1f);
        characterBody.SetDamageSourceDamageMultiplier(DamageSource.DOT, 1f);
        characterBody.SetDamageSourceDamageMultiplier(DamageSource.Hazard, 1f);
        characterBody.SetDamageSourceVulnerabilityAddition(DamageSource.Primary, 0f);
        characterBody.SetDamageSourceVulnerabilityAddition(DamageSource.Secondary, 0f);
        characterBody.SetDamageSourceVulnerabilityAddition(DamageSource.Utility, 0f);
        characterBody.SetDamageSourceVulnerabilityAddition(DamageSource.Special, 0f);
        characterBody.SetDamageSourceVulnerabilityAddition(DamageSource.DOT, 0f);
        characterBody.SetDamageSourceVulnerabilityAddition(DamageSource.Hazard, 0f);
        characterBody.SetDamageSourceVulnerabilityMultiplier(DamageSource.Primary, 1f);
        characterBody.SetDamageSourceVulnerabilityMultiplier(DamageSource.Secondary, 1f);
        characterBody.SetDamageSourceVulnerabilityMultiplier(DamageSource.Utility, 1f);
        characterBody.SetDamageSourceVulnerabilityMultiplier(DamageSource.Special, 1f);
        characterBody.SetDamageSourceVulnerabilityMultiplier(DamageSource.DOT, 1f);
        characterBody.SetDamageSourceVulnerabilityMultiplier(DamageSource.Hazard, 1f);
        GetDamageSourceCoefficients?.Invoke(characterBody);
    }*/
    /*
    private static float GetDamageSourceDamageAddition(this CharacterBody characterBody, DamageSource damageSource)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                return CharacterBodyInterop.GetPrimarySkillDamageAddition(characterBody);
            case DamageSource.Secondary:
                return CharacterBodyInterop.GetSecondarySkillDamageAddition(characterBody);
            case DamageSource.Utility:
                return CharacterBodyInterop.GetUtilitySkillDamageAddition(characterBody);
            case DamageSource.Special:
                return CharacterBodyInterop.GetSpecialSkillDamageAddition(characterBody);
            case DamageSource.DOT:
                return CharacterBodyInterop.GetDOTDamageAddition(characterBody);
            case DamageSource.Equipment:
                return CharacterBodyInterop.GetEquipmentDamageAddition(characterBody);
            case DamageSource.Hazard:
                return CharacterBodyInterop.GetHazardDamageAddition(characterBody);
            default:
                return 1f;
        }
    }
    private static void SetDamageSourceDamageAddition(this CharacterBody characterBody, DamageSource damageSource, float value)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                CharacterBodyInterop.SetPrimarySkillDamageAddition(characterBody, value);
                break;
            case DamageSource.Secondary:
                CharacterBodyInterop.SetSecondarySkillDamageAddition(characterBody, value);
                break;
            case DamageSource.Utility:
                CharacterBodyInterop.SetUtilitySkillDamageAddition(characterBody, value);
                break;
            case DamageSource.Special:
                CharacterBodyInterop.SetSpecialSkillDamageAddition(characterBody, value);
                break;
            case DamageSource.DOT:
                CharacterBodyInterop.SetDOTDamageAddition(characterBody, value);
                break;
            case DamageSource.Equipment:
                CharacterBodyInterop.SetEquipmentDamageAddition(characterBody, value);
                break;
            case DamageSource.Hazard:
                CharacterBodyInterop.SetHazardDamageAddition(characterBody, value);
                break;
            default:
                break;
        }
    }
    private static float GetDamageSourceDamageMultiplier(this CharacterBody characterBody, DamageSource damageSource)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                return CharacterBodyInterop.GetPrimarySkillDamageMultiplier(characterBody);
            case DamageSource.Secondary:
                return CharacterBodyInterop.GetSecondarySkillDamageMultiplier(characterBody);
            case DamageSource.Utility:
                return CharacterBodyInterop.GetUtilitySkillDamageMultiplier(characterBody);
            case DamageSource.Special:
                return CharacterBodyInterop.GetSpecialSkillDamageMultiplier(characterBody);
            case DamageSource.DOT:
                return CharacterBodyInterop.GetDOTDamageMultiplier(characterBody);
            case DamageSource.Equipment:
                return CharacterBodyInterop.GetEquipmentDamageMultiplier(characterBody);
            case DamageSource.Hazard:
                return CharacterBodyInterop.GetHazardDamageMultiplier(characterBody);
            default:
                return 1f;
        }
    }
    private static void SetDamageSourceDamageMultiplier(this CharacterBody characterBody, DamageSource damageSource, float value)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                CharacterBodyInterop.SetPrimarySkillDamageMultiplier(characterBody, value);
                break;
            case DamageSource.Secondary:
                CharacterBodyInterop.SetSecondarySkillDamageMultiplier(characterBody, value);
                break;
            case DamageSource.Utility:
                CharacterBodyInterop.SetUtilitySkillDamageMultiplier(characterBody, value);
                break;
            case DamageSource.Special:
                CharacterBodyInterop.SetSpecialSkillDamageMultiplier(characterBody, value);
                break;
            case DamageSource.DOT:
                CharacterBodyInterop.SetDOTDamageMultiplier(characterBody, value);
                break;
            case DamageSource.Equipment:
                CharacterBodyInterop.SetEquipmentDamageMultiplier(characterBody, value);
                break;
            case DamageSource.Hazard:
                CharacterBodyInterop.SetHazardDamageMultiplier(characterBody, value);
                break;
            default:
                break;
        }
    }
    private static float GetDamageSourceVulnerabilityAddition(this CharacterBody characterBody, DamageSource damageSource)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                return CharacterBodyInterop.GetPrimarySkillVulnerabilityAddition(characterBody);
            case DamageSource.Secondary:
                return CharacterBodyInterop.GetSecondarySkillVulnerabilityAddition(characterBody);
            case DamageSource.Utility:
                return CharacterBodyInterop.GetUtilitySkillVulnerabilityAddition(characterBody);
            case DamageSource.Special:
                return CharacterBodyInterop.GetSpecialSkillVulnerabilityAddition(characterBody);
            case DamageSource.DOT:
                return CharacterBodyInterop.GetDOTVulnerabilityAddition(characterBody);
            case DamageSource.Equipment:
                return CharacterBodyInterop.GetEquipmentVulnerabilityAddition(characterBody);
            case DamageSource.Hazard:
                return CharacterBodyInterop.GetHazardVulnerabilityAddition(characterBody);
            default:
                return 1f;
        }
    }
    private static void SetDamageSourceVulnerabilityAddition(this CharacterBody characterBody, DamageSource damageSource, float value)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                CharacterBodyInterop.SetPrimarySkillVulnerabilityAddition(characterBody, value);
                break;
            case DamageSource.Secondary:
                CharacterBodyInterop.SetSecondarySkillVulnerabilityAddition(characterBody, value);
                break;
            case DamageSource.Utility:
                CharacterBodyInterop.SetUtilitySkillVulnerabilityAddition(characterBody, value);
                break;
            case DamageSource.Special:
                CharacterBodyInterop.SetSpecialSkillVulnerabilityAddition(characterBody, value);
                break;
            case DamageSource.DOT:
                CharacterBodyInterop.SetDOTVulnerabilityAddition(characterBody, value);
                break;
            case DamageSource.Equipment:
                CharacterBodyInterop.SetEquipmentVulnerabilityAddition(characterBody, value);
                break;
            case DamageSource.Hazard:
                CharacterBodyInterop.SetHazardVulnerabilityAddition(characterBody, value);
                break;
            default:
                break;
        }
    }
    private static float GetDamageSourceVulnerabilityMultiplier(this CharacterBody characterBody, DamageSource damageSource)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                return CharacterBodyInterop.GetPrimarySkillVulnerabilityMultiplier(characterBody);
            case DamageSource.Secondary:
                return CharacterBodyInterop.GetSecondarySkillVulnerabilityMultiplier(characterBody);
            case DamageSource.Utility:
                return CharacterBodyInterop.GetUtilitySkillVulnerabilityMultiplier(characterBody);
            case DamageSource.Special:
                return CharacterBodyInterop.GetSpecialSkillVulnerabilityMultiplier(characterBody);
            case DamageSource.DOT:
                return CharacterBodyInterop.GetDOTVulnerabilityMultiplier(characterBody);
            case DamageSource.Equipment:
                return CharacterBodyInterop.GetEquipmentVulnerabilityMultiplier(characterBody);
            case DamageSource.Hazard:
                return CharacterBodyInterop.GetHazardVulnerabilityMultiplier(characterBody);
            default:
                return 1f;
        }
    }
    private static void SetDamageSourceVulnerabilityMultiplier(this CharacterBody characterBody, DamageSource damageSource, float value)
    {
        switch (damageSource)
        {
            case DamageSource.Primary:
                CharacterBodyInterop.SetPrimarySkillVulnerabilityMultiplier(characterBody, value);
                break;
            case DamageSource.Secondary:
                CharacterBodyInterop.SetSecondarySkillVulnerabilityMultiplier(characterBody, value);
                break;
            case DamageSource.Utility:
                CharacterBodyInterop.SetUtilitySkillVulnerabilityMultiplier(characterBody, value);
                break;
            case DamageSource.Special:
                CharacterBodyInterop.SetSpecialSkillVulnerabilityMultiplier(characterBody, value);
                break;
            case DamageSource.DOT:
                CharacterBodyInterop.SetDOTVulnerabilityMultiplier(characterBody, value);
                break;
            case DamageSource.Equipment:
                CharacterBodyInterop.SetEquipmentVulnerabilityMultiplier(characterBody, value);
                break;
            case DamageSource.Hazard:
                CharacterBodyInterop.SetHazardVulnerabilityMultiplier(characterBody, value);
                break;
            default:
                break;
        }
    }*/
}
