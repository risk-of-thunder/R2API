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
using UnityEngine.UI;

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
        IL.RoR2.UI.SprintIcon.FixedUpdate += SprintIcon_FixedUpdate;
    }

    private static void SprintIcon_FixedUpdate(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        Instruction lastInstruction = il.Instrs[il.Instrs.Count - 1];
        Instruction instruction = il.Instrs[0];
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(CheckCustomSprintIcon);
        c.Emit(OpCodes.Brfalse_S, instruction);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(SetSprintIconCustomSprintIcon);
        c.Emit(OpCodes.Br, lastInstruction);
        if (
            c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<SprintIcon>(nameof(SprintIcon.sprintIconObject)),
                x => x.MatchLdcI4(0),
                x => x.MatchCallvirt<GameObject>(nameof(GameObject.SetActive))
            ))
        {
            Instruction instruction2 = c.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetCustomIconObject);
            c.EmitDelegate(NullcheckAndDeactivateCustomIconObject);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
        }
        if (
            c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<SprintIcon>(nameof(SprintIcon.sprintIconObject)),
                x => x.MatchLdcI4(1),
                x => x.MatchCallvirt<GameObject>(nameof(GameObject.SetActive))
            ))
        {
            Instruction instruction2 = c.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetCustomIconObject);
            c.EmitDelegate(NullcheckAndDeactivateCustomIconObject);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 2 failed!");
        }
    }

    internal static void UnsetHooks()
    {
        if (!_hooksEnabled) return;
        _hooksEnabled = false;
    }
    private static void NullcheckAndDeactivateCustomIconObject(GameObject gameObject) => gameObject?.SetActive(false);
    private static bool CheckCustomSprintIcon(SprintIcon sprintIcon) => sprintIcon.body && sprintIcon.body.GetCustomSprintIcon();
    private static void SetSprintIconCustomSprintIcon(SprintIcon sprintIcon)
    {
        Sprite sprite = sprintIcon.body.GetCustomSprintIcon();
        GameObject gameObject = sprintIcon.GetCustomIconObject();
        if (gameObject)
        {
            if (sprintIcon.GetCurrentCustomSprintIcon() != sprite)
            {
                Image image = gameObject.GetComponent<Image>();
                image.sprite = sprite;
                sprintIcon.SetCurrentCustomSprintIcon(sprite);
            }
        }
        else
        {
            Transform transform = sprintIcon.transform.Find("SprintIcon");
            if (transform)
            {
                gameObject = GameObject.Instantiate(transform.gameObject, sprintIcon.transform);
                gameObject.transform.position = transform.position;
                gameObject.transform.rotation = transform.rotation;
                gameObject.transform.localScale = transform.localScale;
                sprintIcon.SetCustomIconObject(gameObject);
                Image image = gameObject.GetComponent<Image>();
                if (image)
                {
                    image.sprite = sprite;
                    sprintIcon.SetCurrentCustomSprintIcon(sprite);
                }
            }
        }
        gameObject?.SetActive(true);
        sprintIcon.descendIconObject?.SetActive(false);
        sprintIcon.sprintIconObject?.SetActive(false);
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
    /// <summary>
    /// Get custom sprint HUD icon sprite for this body.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <returns></returns>
    public static Sprite GetCustomSprintIcon(this CharacterBody characterBody) => CharacterBodyInterop.GetCustomSprintIcon(characterBody);
    /// <summary>
    /// Set custom sprint HUD icon sprite for this body.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="sprite"></param>
    /// <returns></returns>
    public static void SetCustomSprintIcon(this CharacterBody characterBody, Sprite sprite) => CharacterBodyInterop.SetCustomSprintIcon(characterBody, sprite);
    private static GameObject GetCustomIconObject(this SprintIcon sprintIcon) => CharacterBodyInterop.GetCustomIconObject(sprintIcon);
    private static void SetCustomIconObject(this SprintIcon sprintIcon, GameObject gameobject) => CharacterBodyInterop.SetCustomIconObject(sprintIcon, gameobject);
    private static Sprite GetCurrentCustomSprintIcon(this SprintIcon sprintIcon) => CharacterBodyInterop.GetCurrentCustomSprintIcon(sprintIcon);
    private static void SetCurrentCustomSprintIcon(this SprintIcon sprintIcon, Sprite sprite) => CharacterBodyInterop.SetCurrentCustomSprintIcon(sprintIcon, sprite);
}
