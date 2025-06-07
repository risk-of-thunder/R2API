using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
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
    }
    internal static void UnsetHooks()
    {
        if (!_hooksEnabled) return;
        _hooksEnabled = false;
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
}
