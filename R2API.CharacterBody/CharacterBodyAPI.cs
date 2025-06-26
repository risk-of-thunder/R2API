using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using HarmonyLib;
using Mono.Cecil;
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
        IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        IL.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
    }
    internal static void UnsetHooks()
    {
        if (!_hooksEnabled) return;
        _hooksEnabled = false;
        IL.RoR2.HealthComponent.TakeDamageProcess -= HealthComponent_TakeDamageProcess;
        IL.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
    }
    public enum ModdedBodyFlag { };
    /// <summary>
    /// Reserve ModdedBodyFlag to use it with
    /// <see cref="AddModdedBodyFlag(CharacterBody, ModdedBodyFlag)"/>,
    /// <see cref="RemoveModdedBodyFlag(CharacterBody, ModdedBodyFlag)"/> and
    /// <see cref="HasModdedBodyFlag(CharacterBody, ModdedBodyFlag))"/>
    /// </summary>
    /// <returns></returns>
    public static ModdedBodyFlag ReserveBodyFlag()
    {
        SetHooks();
        if (ModdedBodyFlagCount >= CompressedFlagArrayUtilities.sectionsCount * CompressedFlagArrayUtilities.flagsPerSection)
        {
            //I doubt this is ever gonna happen, but just in case.
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
    public static bool RemoveModdedBodyFlag(this CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag) => RemoveModdedBodyFlagInternal(characterBody, moddedBodyFlag);
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
    public static bool HasModdedBodyFlag(this CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag) => HasModdedBodyFlagInternal(characterBody, moddedBodyFlag);
    /// <summary>
    /// Get damage multiplier for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <returns></returns>
    public static float GetDamageSourceDamageMultiplier(this CharacterBody characterBody, DamageSource damageSource) => CharacterBodyInterop.GetDamageSourceDamageMultiplier(characterBody, Enum.GetName(typeof(DamageSource), damageSource));
    /// <summary>
    /// Set damage multiplier for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static void SetDamageSourceDamageMultiplier(this CharacterBody characterBody, DamageSource damageSource, float value) => CharacterBodyInterop.SetDamageSourceDamageMultiplier(characterBody, Enum.GetName(typeof(DamageSource), damageSource), value);
    /// <summary>
    /// Get damage flat addition for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <returns></returns>
    public static float GetDamageSourceDamageAddition(this CharacterBody characterBody, DamageSource damageSource) => CharacterBodyInterop.GetDamageSourceDamageAddition(characterBody, Enum.GetName(typeof(DamageSource), damageSource));
    /// <summary>
    /// Set damage flat addition for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static void SetDamageSourceDamageAddition(this CharacterBody characterBody, DamageSource damageSource, float value) => CharacterBodyInterop.SetDamageSourceDamageAddition(characterBody, Enum.GetName(typeof(DamageSource), damageSource), value);
    /// <summary>
    /// Get damage vulnerability multiplier for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <returns></returns>
    public static float GetDamageSourceVulnerabilityMultiplier(this CharacterBody characterBody, DamageSource damageSource) => CharacterBodyInterop.GetDamageSourceVulnerabilityMultiplier(characterBody, Enum.GetName(typeof(DamageSource), damageSource));
    /// <summary>
    /// Set damage vulnerability multiplier for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static void SetDamageSourceVulnerabilityMultiplier(this CharacterBody characterBody, DamageSource damageSource, float value) => CharacterBodyInterop.SetDamageSourceVulnerabilityMultiplier(characterBody, Enum.GetName(typeof(DamageSource), damageSource), value);
    /// <summary>
    /// Get damage vulnerability flat addition for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <returns></returns>
    public static float GetDamageSourceVulnerabilityAddition(this CharacterBody characterBody, DamageSource damageSource) => CharacterBodyInterop.GetDamageSourceVulnerabilityAddition(characterBody, Enum.GetName(typeof(DamageSource), damageSource));
    /// <summary>
    /// Set damage vulnerability flat addition for the specified damage source. Value resets on Recalculate Stats
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="damageSource"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static void SetDamageSourceVulnerabilityAddition(this CharacterBody characterBody, DamageSource damageSource, float value) => CharacterBodyInterop.SetDamageSourceVulnerabilityAddition(characterBody, Enum.GetName(typeof(DamageSource), damageSource), value);
    private static void AddModdedBodyFlagInternal(CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag)
    {
        SetHooks();
        
        if (!CheckRange(characterBody, moddedBodyFlag)) return;

        var bodyFlags = CharacterBodyInterop.GetModdedBodyFlags(characterBody);
        CompressedFlagArrayUtilities.AddImmutable(ref bodyFlags, (int)moddedBodyFlag - 1);
        CharacterBodyInterop.SetModdedBodyFlags(characterBody, bodyFlags);
    }
    private static bool RemoveModdedBodyFlagInternal(CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag)
    {
        SetHooks();

        if (!CheckRange(characterBody, moddedBodyFlag)) return false;

        var bodyFlags = CharacterBodyInterop.GetModdedBodyFlags(characterBody);
        var removed = CompressedFlagArrayUtilities.RemoveImmutable(ref bodyFlags, (int)moddedBodyFlag - 1);
        CharacterBodyInterop.SetModdedBodyFlags(characterBody, bodyFlags);
        return removed;
    }
    private static bool HasModdedBodyFlagInternal(CharacterBody characterBody, ModdedBodyFlag moddedBodyFlag)
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
    private static void CharacterBody_RecalculateStats(ILContext il)
    {
        var c = new ILCursor(il);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(ResetDamageSourceValues);
        void ResetDamageSourceValues(CharacterBody characterBody)
        {
            CharacterBodyPlugin.Logger.LogMessage("balls");
            var values = Enum.GetValues(typeof(DamageSource)).Cast<DamageSource>();
            foreach (var value in values)
            {
                characterBody.SetDamageSourceDamageAddition(value, 0f);
                characterBody.SetDamageSourceDamageMultiplier(value, 1f);
                characterBody.SetDamageSourceVulnerabilityAddition(value, 0f);
                characterBody.SetDamageSourceVulnerabilityMultiplier(value, 1f);
            }
        }
    }
    private static void HealthComponent_TakeDamageProcess(ILContext il)
    {
        var c = new ILCursor(il);
        FieldReference fieldReference = null;
        FieldReference fieldReference2 = null;
        if (
            c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloca(0),
            x => x.MatchLdloc(0),
            x => x.MatchLdfld(out fieldReference),
            x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.attacker)),
            x => x.MatchCallvirt<GameObject>(nameof(GameObject.GetComponent)),
            x => x.MatchStfld(out fieldReference2)
            ))
        {
            c = new ILCursor(il);
            ILLabel iLLabel = null;
            int i = 7;
            if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<HealthComponent>(nameof(HealthComponent.body)),
            x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.DeathMark)),
            x => x.MatchCallvirt<CharacterBody>(nameof(CharacterBody.HasBuff)),
            x => x.MatchBrfalse(out iLLabel),
            x => x.MatchLdloc(out i)
            )
            )
            {
                c.Index -= 2;
                c.Remove();
                Instruction instruction = null;
                Instruction instruction2 = c.Next;
                c.GotoLabel(iLLabel);
                instruction = c.Emit(OpCodes.Ldloc_0).Prev;
                c.Goto(instruction2);
                c.Emit(OpCodes.Brfalse_S, instruction);
                c.Goto(instruction);
                c.Index++;
                c.Emit(OpCodes.Ldfld, fieldReference);
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldfld, fieldReference2);
                c.Emit(OpCodes.Ldloc, i);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(MultiplyByDamageSource);
                float MultiplyByDamageSource(DamageInfo damageInfo, CharacterBody characterBody, float damage, HealthComponent healthComponent)
                {
                    bool victimNullCheck = healthComponent.body;
                    bool attackerNullCheck = characterBody;
                    if (victimNullCheck) damage *= healthComponent.body.GetDamageSourceVulnerabilityMultiplier(damageInfo.damageType.damageSource);
                    if (attackerNullCheck) damage *= characterBody.GetDamageSourceDamageMultiplier(damageInfo.damageType.damageSource);
                    if (victimNullCheck) damage += healthComponent.body.GetDamageSourceVulnerabilityAddition(damageInfo.damageType.damageSource);
                    if (attackerNullCheck) damage += characterBody.GetDamageSourceDamageAddition(damageInfo.damageType.damageSource);
                    return damage;
                }
                c.Emit(OpCodes.Stloc, i);
            }
            else
            {
                CharacterBodyPlugin.Logger.LogError($"Failed to apply {nameof(HealthComponent_TakeDamageProcess)} 3");
            }
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError($"Failed to apply {nameof(HealthComponent_TakeDamageProcess)} 1");
        }
    }
}
