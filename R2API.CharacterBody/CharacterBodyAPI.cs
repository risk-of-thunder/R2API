using HarmonyLib;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Utils;
using Rewired;
using RoR2;
using RoR2.CameraModes;
using RoR2.ConVar;
using RoR2.Skills;
using RoR2.UI;
using RoR2BepInExPack.GameAssetPaths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
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
    public delegate bool CanAlwaysSprint(CharacterBody characterBody);
    public static Color DefaultSprintColor => new Color(0.816f, 0.9655f, 1f, 1f);
    private static List<CanAlwaysSprint> canAlwaysSprints = [];

    internal static void SetHooks()
    {   
        if (_hooksEnabled) return;
        _hooksEnabled = true;
        IL.RoR2.UI.SprintIcon.FixedUpdate += SprintIcon_FixedUpdate;
        On.EntityStates.GenericCharacterMain.HandleMovements += GenericCharacterMain_HandleMovements;
        IL.RoR2.PlayerCharacterMasterController.PollButtonInput += PlayerCharacterMasterController_PollButtonInput;
        IL.RoR2.CameraModes.CameraModePlayerBasic.CollectLookInputInternal += CameraModePlayerBasic_CollectLookInputInternal;
        IL.RoR2.Skills.SkillDef.OnExecute += SkillDef_OnExecute;
        IL.RoR2.Skills.SkillDef.OnFixedUpdate += SkillDef_OnFixedUpdate;
        IL.RoR2.CameraModes.CameraModePlayerBasic.UpdateInternal += CameraModePlayerBasic_UpdateInternal;
        IL.RoR2.UI.CrosshairManager.UpdateCrosshair += CrosshairManager_UpdateCrosshair;
    }
    internal static void UnsetHooks()
    {
        if (!_hooksEnabled) return;
        _hooksEnabled = false;
        IL.RoR2.UI.SprintIcon.FixedUpdate -= SprintIcon_FixedUpdate;
        On.EntityStates.GenericCharacterMain.HandleMovements -= GenericCharacterMain_HandleMovements;
        IL.RoR2.PlayerCharacterMasterController.PollButtonInput -= PlayerCharacterMasterController_PollButtonInput;
        IL.RoR2.CameraModes.CameraModePlayerBasic.CollectLookInputInternal -= CameraModePlayerBasic_CollectLookInputInternal;
        IL.RoR2.Skills.SkillDef.OnExecute -= SkillDef_OnExecute;
        IL.RoR2.Skills.SkillDef.OnFixedUpdate -= SkillDef_OnFixedUpdate;
        IL.RoR2.CameraModes.CameraModePlayerBasic.UpdateInternal -= CameraModePlayerBasic_UpdateInternal;
        IL.RoR2.UI.CrosshairManager.UpdateCrosshair -= CrosshairManager_UpdateCrosshair;
    }
    private static void GenericCharacterMain_HandleMovements(On.EntityStates.GenericCharacterMain.orig_HandleMovements orig, EntityStates.GenericCharacterMain self)
    {
        if (self != null && self.characterBody && self.characterBody.GetAlwaysSprint()) self.sprintInputReceived = true;
        orig(self);
    }
    private static void SprintIcon_FixedUpdate(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        Instruction lastInstruction = il.Instrs[il.Instrs.Count - 1];
        Instruction instruction = il.Instrs[0];
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(SetSprintIconCustomSprintColor);
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
    private static void PlayerCharacterMasterController_PollButtonInput(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (
            c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(16),
                x => x.MatchStloc(6)
            ))
        {
            c.Index++;
            Instruction instruction = c.Next;
            Instruction instruction2 = c.Next.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetFlag);
            bool GetFlag(PlayerCharacterMasterController playerCharacterMasterController) => playerCharacterMasterController.body.GetAlwaysSprint();
            c.Emit(OpCodes.Brfalse_S, instruction);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloc, 13);
            c.Emit(OpCodes.Ldc_I4, 18);
            c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.GetButton), [typeof(int)]));
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
        }
    }
    private static void CameraModePlayerBasic_CollectLookInputInternal(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel iLLabel = null;
        if (c.TryGotoNext(
                x => x.MatchLdsfld<CameraRigController>(nameof(CameraRigController.enableSprintSensitivitySlowdown)),
                x => x.MatchCallvirt<BoolConVar>("get_value"),
                x => x.MatchBrfalse(out iLLabel)
            ))
        {
            c.Emit(OpCodes.Ldarg_2);
            c.Emit<CameraModeBase.CameraModeContext>(OpCodes.Ldflda, nameof(CameraModeBase.CameraModeContext.targetInfo));
            c.Emit<CameraModeBase.TargetInfo>(OpCodes.Ldfld, nameof(CameraModeBase.TargetInfo.body));
            c.EmitDelegate<Func<CharacterBody, bool>>((cb) =>
            {
                return cb ? cb.GetAlwaysSprint() : false;

            });
            c.Emit(OpCodes.Brtrue_S, iLLabel);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
        }
    }
    private static void SkillDef_OnExecute(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel iLLabel = null;
        if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<SkillDef>(nameof(SkillDef.cancelSprintingOnActivation)),
                x => x.MatchBrfalse(out iLLabel)
            ))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<GenericSkill, bool>>((cb) =>
            {
                return cb.characterBody.GetAlwaysSprint();

            });
            c.Emit(OpCodes.Brtrue_S, iLLabel);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    private static void SkillDef_OnFixedUpdate(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel iLLabel = null;
        if (c.TryGotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchCallvirt<GenericSkill>("get_characterBody"),
                x => x.MatchCallvirt<CharacterBody>("get_isSprinting"),
                x => x.MatchBrfalse(out iLLabel)
            ))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<GenericSkill, bool>>((cb) =>
            {
                return cb && cb.characterBody ? cb.characterBody.GetAlwaysSprint() : false;

            });
            c.Emit(OpCodes.Brtrue_S, iLLabel);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    private static void CameraModePlayerBasic_UpdateInternal(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel iLLabel = null;
        if (
            c.TryGotoNext(
                x => x.MatchLdarg(2),
                x => x.MatchLdflda<CameraModeBase.CameraModeContext>(nameof(CameraModeBase.CameraModeContext.targetInfo)),
                x => x.MatchLdfld<CameraModeBase.TargetInfo>(nameof(CameraModeBase.TargetInfo.isSprinting)),
                x => x.MatchBrfalse(out iLLabel)
            ))
        {
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate(method1);
            bool method1(ref CameraModeBase.CameraModeContext cameraModeContext)
            {
                if (cameraModeContext.targetInfo.body != null)
                {
                    return cameraModeContext.targetInfo.body.GetAlwaysSprint();
                }
                else
                {
                    return true;
                }

            }
            c.Emit(OpCodes.Brtrue_S, iLLabel);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    private static void CrosshairManager_UpdateCrosshair(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel iLLabel = null;
        ILLabel iLLabel2 = null;
        if (
            c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CrosshairManager>(nameof(CrosshairManager.cameraRigController)),
                x => x.MatchCallvirt<CameraRigController>("get_hasOverride"),
                x => x.MatchBrtrue(out iLLabel)
            )
            &&
            c.TryGotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchCallvirt<CharacterBody>("get_isSprinting"),
                x => x.MatchBrfalse(out iLLabel2)
            ))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<CharacterBody, bool>>((cb) =>
            {
                return cb.GetAlwaysSprint();
            });
            c.Emit(OpCodes.Brtrue_S, iLLabel);
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    private static void NullcheckAndDeactivateCustomIconObject(GameObject gameObject) => gameObject?.SetActive(false);
    private static bool CheckCustomSprintIcon(SprintIcon sprintIcon) => sprintIcon.body && sprintIcon.body.GetCustomSprintIcon();
    private static void SetSprintIconCustomSprintColor(SprintIcon sprintIcon)
    {
        if (!sprintIcon || !sprintIcon.body) return;
        Color? color = sprintIcon.body.GetCustomSprintColor();
        if (sprintIcon.GetCurrentCustomSprintColor() != color)
        {
            SetGameObjectCustomSprintColor(sprintIcon.GetCustomIconObject(), color.HasValue ? color.Value : DefaultSprintColor);
            SetGameObjectCustomSprintColor(sprintIcon.descendIconObject, color.HasValue ? color.Value : DefaultSprintColor);
            SetGameObjectCustomSprintColor(sprintIcon.sprintIconObject, color.HasValue ? color.Value : DefaultSprintColor);
            sprintIcon.SetCurrentCustomSprintColor(color);
        }
    }
    private static void SetGameObjectCustomSprintColor(GameObject gameObject, Color color)
    {
        if (!gameObject) return;
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
    }
    private static void SetSprintIconCustomSprintIcon(SprintIcon sprintIcon)
    {
        Sprite sprite = sprintIcon.body.GetCustomSprintIcon();
        GameObject customIconObject = sprintIcon.GetCustomIconObject();
        if (customIconObject)
        {
            if (sprintIcon.GetCurrentCustomSprintIcon() != sprite)
            {
                Image image = customIconObject.GetComponent<Image>();
                image.sprite = sprite;
                sprintIcon.SetCurrentCustomSprintIcon(sprite);
            }
        }
        else
        {
            Transform transform = sprintIcon.transform.Find("SprintIcon");
            if (transform)
            {
                customIconObject = GameObject.Instantiate(transform.gameObject, sprintIcon.transform);
                customIconObject.transform.position = transform.position;
                customIconObject.transform.rotation = transform.rotation;
                customIconObject.transform.localScale = transform.localScale;
                sprintIcon.SetCustomIconObject(customIconObject);
                Image image = customIconObject.GetComponent<Image>();
                if (image)
                {
                    image.sprite = sprite;
                    sprintIcon.SetCurrentCustomSprintIcon(sprite);
                }
            }
        }
        customIconObject?.SetActive(true);
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
    /// <summary>
    /// Get custom sprint HUD icon color for this body.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <returns></returns>
    public static Color? GetCustomSprintColor(this CharacterBody characterBody) => CharacterBodyInterop.GetCustomSprintColor(characterBody);
    /// <summary>
    /// Set custom sprint HUD icon color for this body.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static void SetCustomSprintColor(this CharacterBody characterBody, Color? color) => CharacterBodyInterop.SetCustomSprintColor(characterBody, color);
    /// <summary>
    /// Get ability for the body to always sprint.
    /// </summary>
    /// <param name="characterBody"></param>
    /// <returns></returns>
    public static bool GetAlwaysSprint(this CharacterBody characterBody)
    {
        CharacterBodyAPI.SetHooks();
        foreach (CanAlwaysSprint canAlwaysSprint in canAlwaysSprints) if (canAlwaysSprint != null && canAlwaysSprint.Invoke(characterBody)) return true;
        return false;
    }
    /// <summary>
    /// Add a condition for if the body is able to always sprint
    /// </summary>
    /// <param name="canAlwaysSprint"></param>
    /// <returns></returns>
    public static void AddAlwaysSprintCondition(CanAlwaysSprint canAlwaysSprint) => canAlwaysSprints.Add(canAlwaysSprint);
    private static GameObject GetCustomIconObject(this SprintIcon sprintIcon) => CharacterBodyInterop.GetCustomIconObject(sprintIcon);
    private static void SetCustomIconObject(this SprintIcon sprintIcon, GameObject gameobject) => CharacterBodyInterop.SetCustomIconObject(sprintIcon, gameobject);
    private static Sprite GetCurrentCustomSprintIcon(this SprintIcon sprintIcon) => CharacterBodyInterop.GetCurrentCustomSprintIcon(sprintIcon);
    private static void SetCurrentCustomSprintIcon(this SprintIcon sprintIcon, Sprite sprite) => CharacterBodyInterop.SetCurrentCustomSprintIcon(sprintIcon, sprite);
    private static Color? GetCurrentCustomSprintColor(this SprintIcon sprintIcon) => CharacterBodyInterop.GetCurrentCustomSprintColor(sprintIcon);
    private static void SetCurrentCustomSprintColor(this SprintIcon sprintIcon, Color? color) => CharacterBodyInterop.SetCurrentCustomSprintColor(sprintIcon, color);
}
