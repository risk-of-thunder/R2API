﻿using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Skills.Interop;
using RoR2;
using RoR2.UI;

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
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.UI.LoadoutPanelController.Rebuild -= LoadoutPanelControllerRebuildHook;
        IL.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot -= LoadoutPanelControllerRowFromSkillSlotHook;
        IL.RoR2.UI.CharacterSelectController.BuildSkillStripDisplayData -= CharacterSelectControllerBuildSkillStripDisplayDataHook;
    }

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
            x => x.MatchLdloc(out _),
            x => x.MatchNewobj<LoadoutPanelController.Row>()))
        {
            c.Index++;
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

    private static string LoadoutPanelControllerReplaceTitleToken(string token, GenericSkill skill)
    {
        var tokenOverride = skill.GetLoadoutTitleTokenOverride();
        if (!string.IsNullOrEmpty(tokenOverride))
        {
            return tokenOverride;
        }

        return token;
    }

    private struct GenericSkillComparer : IComparer<GenericSkill>
    {
        public readonly int Compare(GenericSkill x, GenericSkill y) => x.GetOrderPriority() - y.GetOrderPriority();
    }
}
