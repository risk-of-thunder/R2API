using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using RoR2BepInExPack.Utilities;
using UnityEngine;

namespace R2API;

/// <summary>
/// API for modifying the HealthBar
/// </summary>

public static class HealthBarAPI {
    private static bool _hooksEnabled;
    private static bool alreadyLoaded;
    private static bool valuesHookFailed;
    private static bool overlaysHookFailed;
    private static List<BarOverlayInfo> OverlayInfos = new();
    private static FixedConditionalWeakTable<HealthComponent, AssignedOverlaysData> HealthBarData = new();
    private static MethodInfo get_source;
    private static MethodInfo get_combinedHealth;
    private static int lastClaimedBarIndex = 0;
    internal static void SetHooks(bool reset = false) {
        if (!reset) {
            // Evaluate all on game load to save on hook applications.
            RoR2Application.onLoad += () => {
                alreadyLoaded = true;
                if (OverlayInfos.Count > 0) {
                    EvaluateHooks();
                }
            };

            get_source = AccessTools.PropertyGetter(typeof(HealthBar), nameof(HealthBar.source));
            get_combinedHealth = AccessTools.PropertyGetter(typeof(HealthComponent), nameof(HealthComponent.combinedHealth));
            return;
        }

        _hooksEnabled = true;
        if (!overlaysHookFailed) IL.RoR2.UI.HealthBar.ApplyBars += HandleBarInfos;
        if (!valuesHookFailed) IL.RoR2.UI.HealthBar.UpdateHealthbar += HandleBarValues;

        if (valuesHookFailed) {
            IL.RoR2.UI.HealthBar.UpdateHealthbar -= HandleBarValues;
        }

        if (overlaysHookFailed) {
            IL.RoR2.UI.HealthBar.ApplyBars -= HandleBarInfos;
        }
    }
    internal static void UnsetHooks() {
        _hooksEnabled = false;
        if (!overlaysHookFailed) IL.RoR2.UI.HealthBar.ApplyBars -= HandleBarInfos;
        if (!valuesHookFailed) IL.RoR2.UI.HealthBar.UpdateHealthbar -= HandleBarValues;
    }
    // Rebuild the hooks based on currently registered overlays
    internal static void EvaluateHooks() {
        if (_hooksEnabled) {
            UnsetHooks();
        }

        SetHooks(true);
    }
    internal static void HandleBarValues(ILContext il) {
        ILCursor c = new(il);
        TypeReference refFloat = il.Import(typeof(float));

        VariableDefinition currentHealthVariable = new(refFloat);
        il.Method.Body.Variables.Add(currentHealthVariable);

        VariableDefinition overlayDataVariable = new(il.Import(typeof(AssignedOverlaysData)));
        il.Method.Body.Variables.Add(overlayDataVariable);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<HealthBar, AssignedOverlaysData>>((source) => {
            if (source.source) {
                return HealthBarData.GetOrCreateValue(source.source);
            }
            return null;
        });
        c.Emit(OpCodes.Stloc, overlayDataVariable);

        int fullHealthIndex = -1;
        c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<HealthComponent>("get_fullHealth"),
            x => x.MatchStloc(out fullHealthIndex)
        );
        if (fullHealthIndex < 0) {
            valuesHookFailed = true;
            return;
        }

        // gather modified values from all overlays

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Call, get_source);
        c.Emit(OpCodes.Call, get_combinedHealth);
        c.Emit(OpCodes.Stloc, currentHealthVariable);

        foreach (BarOverlayInfo overlay in OverlayInfos) {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, overlayDataVariable);
            c.Emit(OpCodes.Ldloca, currentHealthVariable);
            c.Emit(OpCodes.Ldloca, fullHealthIndex);
            c.EmitDelegate<HealthValuesRefPasser>((HealthBar source, AssignedOverlaysData data, ref float currentHealth, ref float maxHealth) => {
                if (data == null || (overlay.BodySpecific && !data.AssignedOverlays[(int)overlay.OverlayIndex]))
                {
                    return;
                }

                overlay.ModifyHealthValues?.Invoke(source, ref currentHealth, ref maxHealth);
            });
        }

        int index = c.Next.Offset;

        // swap them out in the two handlers for health numbers
        Assert(c.TryGotoNext(MoveType.After, x => x.MatchLdloc(fullHealthIndex) && x.Offset > index));
        Assert(c.TryGotoPrev(MoveType.Before, x => x.MatchStloc(out _) && x.Offset > index));
        if (valuesHookFailed) return;
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldloc, currentHealthVariable);

        Assert(c.TryGotoNext(MoveType.Before,
            x => x.MatchCallOrCallvirt(get_combinedHealth),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<HealthBar>("oldHealth")
        ));
        if (valuesHookFailed) return;
        c.Index++;
        c.Emit(OpCodes.Pop);
        c.Emit(OpCodes.Ldloc, currentHealthVariable);

        for (int i = 0; i < 2; i++) {
            Assert(c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt(get_combinedHealth)));
            if (valuesHookFailed) return;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloc, currentHealthVariable);
        }

        static void Assert(bool val) { if (!val) { valuesHookFailed = true; CharacterBodyPlugin.Logger.LogError("Failed to apply HandleBarValues hook."); }}
    }

    // cannot pass a ref via action or func
    internal delegate void HealthValuesRefPasser(HealthBar source, AssignedOverlaysData data, ref float current, ref float max);

    // bar info handler never runs
    internal static void HandleBarInfos(ILContext il) {
        ILCursor c = new(il);
        MethodReference handleBar = null;
        VariableDefinition allocator = null;
        int allocatorIndex = -1;

        bool foundHandleBar = c.TryGotoNext(x => 
            x.MatchCallOrCallvirt(out handleBar) && handleBar != null && handleBar.Name.StartsWith("<ApplyBars>g__HandleBar|")
        );
        bool foundAllocator = c.TryGotoPrev(x => x.MatchLdloca(out allocatorIndex));
        
        if (!foundHandleBar || !foundAllocator || allocatorIndex < 0 || allocatorIndex >= il.Method.Body.Variables.Count) {
            overlaysHookFailed = true;
            CharacterBodyPlugin.Logger.LogError("Failed to apply IL hook for ApplyBars.");
            return;
        }
        allocator = il.Method.Body.Variables[allocatorIndex];
        TypeReference refBarInfo = il.Import(typeof(HealthBar.BarInfo));

        c.Index = 0;

        VariableDefinition overlayDataVariable = new(il.Import(typeof(AssignedOverlaysData)));
        il.Method.Body.Variables.Add(overlayDataVariable);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<HealthBar, AssignedOverlaysData>>((source) => {
            if (source.source) {
                return HealthBarData.GetOrCreateValue(source.source);
            }
            return null;
        });
        c.Emit(OpCodes.Stloc, overlayDataVariable);
        int index = c.Index;

        foreach (BarOverlayInfo overlay in OverlayInfos) {
            c.Index = index;
            VariableDefinition barInfoVariable = new(refBarInfo);
            il.Method.Body.Variables.Add(barInfoVariable);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, overlayDataVariable);
            c.EmitDelegate<Func<HealthBar, AssignedOverlaysData, HealthBar.BarInfo>>((source, data) => {
                HealthBar.BarInfo barInfo = new() {
                    enabled = overlay.BarInfo.enabled,
                    color = overlay.BarInfo.color,
                    sprite = overlay.BarInfo.sprite,
                    imageType = overlay.BarInfo.imageType,
                    sizeDelta = overlay.BarInfo.sizeDelta,
                    normalizedXMax = overlay.BarInfo.normalizedXMax,
                    normalizedXMin = overlay.BarInfo.normalizedXMin,
                };

                if (data == null || (overlay.BodySpecific && !data.AssignedOverlays[(int)overlay.OverlayIndex])) {
                    barInfo.enabled = false;
                    return barInfo;
                }

                overlay.ModifyBarInfo?.Invoke(source, ref barInfo);
                return barInfo;
            });
            c.Emit(OpCodes.Stloc, barInfoVariable);

            bool foundGetActiveCount = c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<HealthBar.BarInfoCollection>(nameof(HealthBar.BarInfoCollection.GetActiveCount)));
            if (!foundGetActiveCount) {
                overlaysHookFailed = true;
                CharacterBodyPlugin.Logger.LogError("Failed to find GetActiveCount in ApplyBars.");
                return;
            }

            c.Emit(OpCodes.Ldloca, barInfoVariable);
            c.EmitDelegate((int count, in HealthBar.BarInfo barInfo) => {
                if (barInfo.enabled) {
                    count++;
                }

                return count;
            });

            c.TryGotoNext(MoveType.Before, x => x.MatchRet());
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, barInfoVariable);
            c.Emit(OpCodes.Ldloca, allocator);
            c.Emit(OpCodes.Call, handleBar);
        }
    }
    
    #pragma warning disable R2APISubmodulesAnalyzer
    /// <summary>Registers a new HealthBar overlay. Must be called before RoR2Application.onLoad finishes.</summary>
    /// <param name="overlayInfo">The BarOverlayInfo defining your overlay.</param>
    /// <returns>The BarOverlayIndex that corresponds to your overlay, or BarOverlayIndex.None if called too late. Use this if you need to assign the bar to a specific body.</returns>
    public static BarOverlayIndex RegisterBarOverlay(BarOverlayInfo overlayInfo) {
        if (alreadyLoaded) {
            CharacterBodyPlugin.Logger.LogError("Mod attempted to add a BarOverlayInfo after game load. This is not allowed.");
            return BarOverlayIndex.None;
        }

        overlayInfo.OverlayIndex = (BarOverlayIndex)lastClaimedBarIndex;
        lastClaimedBarIndex++;
        OverlayInfos.Add(overlayInfo);
        foreach (var kvp in HealthBarData) {
            kvp.Value.UpdateSize();
        }

        return overlayInfo.OverlayIndex;
    }

    /// <summary>Marks a HealthBar overlay as applying to a specific body.</summary>
    /// <param name="body">The body to apply to.</param>
    /// <param name="index">The index of your overlay.</param>
    public static void AddOverlayToBody(CharacterBody body, BarOverlayIndex index) {
        if (index < 0) return;

        AssignedOverlaysData data = HealthBarData.GetOrCreateValue(body.healthComponent);
        if (data.AssignedOverlays.Length >= (int)index) {
            data.AssignedOverlays[(int)index] = true;
        }
    }

    /// <summary>Unmarks a HealthBar overlay as applying to a specific body.</summary>
    /// <param name="body">The body to remove from.</param>
    /// <param name="index">The index of your overlay.</param>
    public static void RemoveOverlayFromBody(CharacterBody body, BarOverlayIndex index) {
        if (index < 0) return;
        
        AssignedOverlaysData data = HealthBarData.GetOrCreateValue(body.healthComponent);
        if (data.AssignedOverlays.Length >= (int)index) {
            data.AssignedOverlays[(int)index] = false;
        }
    }
    #pragma warning restore R2APISubmodulesAnalyzer

    /// <summary>Defines on a modded HealthBar overlay.</summary>
    public class BarOverlayInfo {
        /// <summary>A template BarInfo that will be applied to the HealthBar if assigned.</summary>
        public HealthBar.BarInfo BarInfo = default;
        /// <summary>Causes your bar to only be handled on bodies it is assigned to, via AddOverlayToCharacterBody.</summary>
        public bool BodySpecific = false;
        /// <summary>A delegate which will run when HealthBar overlays are updated. Use this to set things like whether your bar is enabled or its size.</summary>
        public ModifyBarInfoCallback ModifyBarInfo;
        /// <summary>A delegate that which will run when HealthBar display values are being calculated. Use this to modify the numerical health values before they render on the HUD.</summary>
        public ModifyHealthValuesCallback ModifyHealthValues;
        /// <summary>A delegate that can be run when HealthBar overlays are updated. Use this to set things like whether your bar is enabled or its size.</summary>
        /// <param name="source">The HealthBar the overlay is being handled on.</param>
        /// <param name="barInfo">A reference to the BarInfo that is being used to define your overlay.</param> 
        public delegate void ModifyBarInfoCallback(HealthBar source, ref HealthBar.BarInfo barInfo);
        /// <summary>A delegate that can be run when HealthBar display values are being calculated. Use this to modify the numerical health values before they render on the HUD.</summary>
        /// <param name="source">The HealthBar the display values are being modified on.</param>
        /// <param name="currentHealth">A reference to the current health value that will be displayed on the healthbar. Modify this in-place.</param> 
        /// <param name="maxHealth">A reference to the current health value that will be displayed on the healthbar. Modify this in-place.</param> 
        public delegate void ModifyHealthValuesCallback(HealthBar source, ref float currentHealth, ref float maxHealth);
        /// <summary>The identifier for this overlay type.</summary>
        public BarOverlayIndex OverlayIndex;
    }

    /// <summary>An identifier for a modded HealthBar overlay.</summary>
    public enum BarOverlayIndex {
        None = -1
    }
    internal class AssignedOverlaysData {
        internal bool[] AssignedOverlays = new bool[OverlayInfos.Count];

        internal void UpdateSize() {
            if (OverlayInfos.Count > AssignedOverlays.Length) {
                bool[] temp = new bool[AssignedOverlays.Length + 32];
                for (int i = 0; i < AssignedOverlays.Length; i++) {
                    temp[i] = AssignedOverlays[i];
                }
                AssignedOverlays = temp;
            }
        }
    }
}