﻿using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Skills.Interop;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using HarmonyLib;

namespace R2API;

[AutoVersion]
public static partial class SkillsAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".skills";
    public const string PluginName = R2API.PluginName + ".Skills";

    internal static void SetHooks()
    {
        IL.RoR2.UI.LoadoutPanelController.Rebuild += LoadoutPanelControllerRebuildHook;
        IL.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot += LoadoutPanelControllerRowFromSkillSlotHook;
        IL.RoR2.UI.CharacterSelectController.BuildSkillStripDisplayData += CharacterSelectControllerBuildSkillStripDisplayDataHook;
        IL.RoR2.GenericSkill.RecalculateMaxStock += GenericSkill_RecalculateMaxStock;
        IL.RoR2.UI.SkillIcon.Update += SkillIcon_Update;
    }
    internal static void UnsetHooks()
    {
        IL.RoR2.UI.LoadoutPanelController.Rebuild -= LoadoutPanelControllerRebuildHook;
        IL.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot -= LoadoutPanelControllerRowFromSkillSlotHook;
        IL.RoR2.UI.CharacterSelectController.BuildSkillStripDisplayData -= CharacterSelectControllerBuildSkillStripDisplayDataHook;
        IL.RoR2.GenericSkill.RecalculateMaxStock -= GenericSkill_RecalculateMaxStock;
        IL.RoR2.UI.SkillIcon.Update -= SkillIcon_Update;
    }

    /// <summary>
    /// Gets the value of cooldown refresh sound of a SkillDef.
    /// </summary>
    public static string GetCustomCooldownRefreshSound(this SkillDef skillDef) => SkillDefInterop.GetCustomCooldownRefreshSound(skillDef);

    /// <summary>
    /// Sets the value of cooldown refresh sound of a SkillDef.
    /// </summary>
    public static void SetCustomCooldownRefreshSound(this SkillDef skillDef, string value) => SkillDefInterop.SetCustomCooldownRefreshSound(skillDef, value);

    /// <summary>
    /// Gets the value of bonus stock multiplication of a SkillDef.
    /// </summary>
    public static int GetBonusStockMultiplier(this SkillDef skillDef) => SkillDefInterop.GetBonusStockMultiplier(skillDef);
    
     /// <summary>
    /// Sets the value of bonus stock multiplication of a SkillDef.
    /// </summary>
    public static void SetBonusStockMultiplier(this SkillDef skillDef, int value) => SkillDefInterop.SetBonusStockMultiplier(skillDef, value);
    
    /// <summary>
    /// Gets the value of whether a GenericSkill row should be hidden in loadout tab or not.
    /// </summary>
    public static bool GetHideInLoadout(this GenericSkill genericSkill) => GenericSkillInterop.GetHideInLoadout(genericSkill);
    
    /// <summary>
    /// Sets the value of whether a GenericSkill row should be hidden in loadout tab or not.
    /// </summary>
    public static void SetHideInLoadout(this GenericSkill genericSkill, bool value) => GenericSkillInterop.SetHideInLoadout(genericSkill, value);

    /// <summary>
    /// Gets the value of whether a GenericSkill row should be hidden in skills tab if the first skill is selected or not.
    /// </summary>
    public static bool GetHideInCharacterSelectIfFirstSkillSelected(this GenericSkill genericSkill) => GenericSkillInterop.GetHideInCharacterSelectIfFirstSkillSelected(genericSkill);

    /// <summary>
    /// Sets the value of whether a GenericSkill row should be hidden in skills tab if the first skill is selected or not.
    /// </summary>
    public static void SetHideInCharacterSelectIfFirstSkillSelected(this GenericSkill genericSkill, bool value) => GenericSkillInterop.SetHideInCharacterSelectIfFirstSkillSelected(genericSkill, value);

    /// <summary>
    /// Gets the value of a GenericSkill row order for sorting in skills and loadout tabs. Default value is 0.
    /// </summary>
    public static int GetOrderPriority(this GenericSkill genericSkill) => GenericSkillInterop.GetOrderPriority(genericSkill);

    /// <summary>
    /// Sets the value of a GenericSkill row order for sorting in skills and loadout tabs. Default value is 0.
    /// </summary>
    public static void SetOrderPriority(this GenericSkill genericSkill, int value) => GenericSkillInterop.SetOrderPriority(genericSkill, value);

    /// <summary>
    /// Gets the value of a GenericSkill title token that will be used instead of default one in loadout tabs.
    /// </summary>
    public static string GetLoadoutTitleTokenOverride(this GenericSkill genericSkill) => GenericSkillInterop.GetLoadoutTitleTokenOverride(genericSkill);
    
    /// <summary>
    /// Sets the value of a GenericSkill title token that will be used instead of default one in loadout tabs.
    /// </summary>
    public static void SetLoadoutTitleTokenOverride(this GenericSkill genericSkill, string value) => GenericSkillInterop.SetLoadoutTitleTokenOverride(genericSkill, value);

    private static void CharacterSelectControllerBuildSkillStripDisplayDataHook(ILContext il)
    {
        var intArrayType = il.Method.Module.TypeSystem.Int32.MakeArrayType();
        var originalIndices = new VariableDefinition(intArrayType);
        il.Body.Variables.Add(originalIndices);

        var c = new ILCursor(il);
        var skillSlotsIndex = -1;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(2),
            x => x.MatchLdfld<CharacterSelectController.BodyInfo>(nameof(CharacterSelectController.BodyInfo.skillSlots)),
            x => x.MatchStloc(out skillSlotsIndex)))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.Emit(OpCodes.Ldloca, skillSlotsIndex);
            c.EmitDelegate(CharacterSelectControllerRemoveAndSortSkills);
            c.Emit(OpCodes.Stloc, originalIndices);
        }
        else
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(CharacterSelectControllerBuildSkillStripDisplayDataHook)} 1");
            return;
        }

        if (c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloc(out _),
            x => x.MatchCallOrCallvirt<Loadout.BodyLoadoutManager>(nameof(Loadout.BodyLoadoutManager.GetSkillVariant))))
        {
            c.Emit(OpCodes.Ldloc, originalIndices);
            c.Index++;
            c.Emit(OpCodes.Ldelem_I4);
        }
        else
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(CharacterSelectControllerBuildSkillStripDisplayDataHook)} 2");
        }
    }

    private static void LoadoutPanelControllerRebuildHook(ILContext il)
    {
        var intArrayType = il.Method.Module.TypeSystem.Int32.MakeArrayType();
        var originalIndices = new VariableDefinition(intArrayType);
        il.Body.Variables.Add(originalIndices);

        var c = new ILCursor(il);

        var bodyIndex = -1;
        var genericSkillsIndex = -1;

        if (!c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt(typeof(BodyCatalog), nameof(BodyCatalog.GetBodyPrefabBodyComponent)),
            x => x.MatchStloc(out bodyIndex)))
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(LoadoutPanelControllerRebuildHook)} 1");
            return;
        }

        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt(typeof(GetComponentsCache<GenericSkill>).GetMethod(nameof(GetComponentsCache<GenericSkill>.GetGameObjectComponents))),
            x => x.MatchStloc(out genericSkillsIndex)))
        {
            c.Emit(OpCodes.Ldloc, genericSkillsIndex);
            c.Emit(OpCodes.Ldloc, bodyIndex);
            c.EmitDelegate(LoadoutPanelControllerRemoveAndSortSkills);
            c.Emit(OpCodes.Stloc, originalIndices);
        }
        else
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(LoadoutPanelControllerRebuildHook)} 2");
            return;
        }

        if (c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloc(out _),
            x => x.MatchLdloc(out _),
            x => x.MatchCallOrCallvirt<LoadoutPanelController.Row>(nameof(LoadoutPanelController.Row.FromSkillSlot))))
        {
            c.Emit(OpCodes.Ldloc, originalIndices);
            c.Index++;
            c.Emit(OpCodes.Ldelem_I4);
        }
        else
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(LoadoutPanelControllerRebuildHook)} 3");
        }
    }

    private static void LoadoutPanelControllerRowFromSkillSlotHook(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.Before,
            x => x.MatchNewobj<LoadoutPanelController.Row>()))
        {
            c.Emit(OpCodes.Ldarg_3);
            c.EmitDelegate(LoadoutPanelControllerReplaceTitleToken);
        }
        else
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(LoadoutPanelControllerRowFromSkillSlotHook)}");
        }
    }

    private static int[] LoadoutPanelControllerRemoveAndSortSkills(List<GenericSkill> list, CharacterBody body)
    {
        var sortedValues = list
            .Select((skill, index) => (skill, index))
            .Where((x) => !x.skill.GetHideInLoadout())
            .OrderBy(x => x.skill.GetOrderPriority());
        var originalIndices = sortedValues.Select(x => x.index).ToArray();
        var sortedSkills = sortedValues.Select(x => x.skill).ToArray();
        list.Clear();
        list.AddRange(sortedSkills);
        return originalIndices;
    }

    private static int[] CharacterSelectControllerRemoveAndSortSkills(Loadout loadout, in CharacterSelectController.BodyInfo bodyInfo, ref GenericSkill[] previousValue)
    {
        var bodyIndex = bodyInfo.bodyIndex;
        var sortedValues = previousValue
            .Select((skill, index) => (skill, index))
            .Where((x) => !x.skill.GetHideInCharacterSelectIfFirstSkillSelected() || loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, x.index) != 0)
            .OrderBy(x => x.skill.GetOrderPriority());
        previousValue = sortedValues.Select(x => x.skill).ToArray();
        return sortedValues.Select(x => x.index).ToArray();
    }
    
    private static void GenericSkill_SetBonusStockFromBody(On.RoR2.GenericSkill.orig_SetBonusStockFromBody orig, GenericSkill self, int newBonusStockFromBody)
    {
    if (self.skillDef)newBonusStockFromBody *= (int)System.MathF.Max(1, self.skillDef.GetBonusStockMultiplier());
    orig(self, newBonusStockFromBody);
    }

    private static string LoadoutPanelControllerReplaceTitleToken(string token, GenericSkill skill)
    {
        var tokenOverride = skill.GetLoadoutTitleTokenOverride();
        if (!string.IsNullOrEmpty(tokenOverride))
        {
            return tokenOverride;
        }

        return token;
    }
    
    private static void GenericSkill_RecalculateMaxStock(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchLdarg(0),
            x => x.MatchCall(typeof(GenericSkill).GetPropertyGetter(nameof(GenericSkill.maxStock))),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<GenericSkill>(nameof(GenericSkill.bonusStockFromBody)),
            x => x.MatchAdd()
            )
            )
        {
            c.Index--;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(MultiplyBonusStock);
            int MultiplyBonusStock(GenericSkill genericSkill)
            {
                SkillDef skillDef = genericSkill.skillDef;
                int multiplier = skillDef ? skillDef.GetBonusStockMultiplier() : 1;
                return multiplier == 0 ? 1 : multiplier;
            }
            c.Emit(OpCodes.Mul);

        }
        else
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(GenericSkill_RecalculateMaxStock)}");
            return;
        }
    }
    private static void SkillIcon_Update(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.Before,
            x => x.MatchLdstr(out _),
            x => x.MatchCall(typeof(RoR2Application).GetPropertyGetter(nameof(RoR2Application.instance))),
            x => x.MatchCallvirt(typeof(Component).GetPropertyGetter(nameof(Component.gameObject))),
            x => x.MatchCall(typeof(Util), nameof(Util.PlaySound))
            )
            )
        {
            Instruction instruction = c.Next;
            Instruction instruction2 = c.Next.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(CheckCustomCooldownRefreshSound);
            bool CheckCustomCooldownRefreshSound(SkillIcon skillIcon) => skillIcon.targetSkill.skillDef.GetCustomCooldownRefreshSound() == null;
            c.Emit(OpCodes.Brtrue_S, instruction);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetCustomCooldownRefreshSound);
            string GetCustomCooldownRefreshSound(SkillIcon skillIcon) => skillIcon.targetSkill.skillDef.GetCustomCooldownRefreshSound();
            c.Emit(OpCodes.Br, instruction2);
        }
        else
        {
            SkillsPlugin.Logger.LogError($"Failed to apply {nameof(SkillIcon_Update)}");
            return;
        }
    }
    private struct GenericSkillComparer : IComparer<GenericSkill>
    {
        public readonly int Compare(GenericSkill x, GenericSkill y) => x.GetOrderPriority() - y.GetOrderPriority();
    }
}
