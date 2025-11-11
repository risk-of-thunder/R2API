using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using RoR2;
using RoR2.UI;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

/// <summary>
/// API for stacking character execute thresholds with diminishing returns.
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class ExecuteAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".execute";
    public const string PluginName = R2API.PluginName + ".Execute";

    #region hook management
    private static bool _hooksEnabled = false;
    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.GlobalEventManager.ServerDamageDealt += GlobalEventManager_ServerDamageDealt;
        On.RoR2.HealthComponent.GetHealthBarValues += HealthComponent_GetHealthBarValues;
        IL.RoR2.UI.HealthBar.UpdateBarInfos += HealthBar_UpdateBarInfos;
        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        if (!_hooksEnabled)
        {
            return;
        }

        On.RoR2.GlobalEventManager.ServerDamageDealt -= GlobalEventManager_ServerDamageDealt;
        On.RoR2.HealthComponent.GetHealthBarValues -= HealthComponent_GetHealthBarValues;
        IL.RoR2.UI.HealthBar.UpdateBarInfos -= HealthBar_UpdateBarInfos;
        _hooksEnabled = false;
    }
    #endregion

    #region public-facing methods
    public delegate void CalculateAdditiveExecuteThresholdEventHandler(CharacterBody victimBody, ref float executeFractionAdd);

    [Tooltip("For stackable executes with cross-mod compat. Calculates the additive execute threshold. Final threshold is calculated by the function: 1 - 1 / (1 + executeFractionAdd)")]
    public static CalculateAdditiveExecuteThresholdEventHandler CalculateAdditiveExecuteThreshold;

    public delegate void CalculateAdditiveExecuteThresholdForViewerEventHandler(CharacterBody victimBody, CharacterBody viewerBody, ref float executeFractionAdd);

    [Tooltip("For stackable executes with cross-mod compat. Calculates the additive execute threshold, factoring in viewer bodies. Final threshold is calculated by the function: 1 - 1 / (1 + executeFractionAdd)")]
    public static CalculateAdditiveExecuteThresholdForViewerEventHandler CalculateAdditiveExecuteThresholdForViewer;

    public delegate void CalculateExecuteThresholdEventHandler(CharacterBody victimBody, ref float highestExecuteThreshold);

    [Tooltip("For vanilla-like executes that don't stack. Calculates the flat execute threshold.")]
    public static CalculateExecuteThresholdEventHandler CalculateExecuteThreshold;

    public delegate void CalculateExecuteThresholdForViewerEventHandler(CharacterBody victimBody, CharacterBody viewerBody, ref float highestExecuteThreshold);

    [Tooltip("For vanilla-like executes that don't stack. Calculates the flat execute threshold, factoring in viewer bodies.")]
    public static CalculateExecuteThresholdForViewerEventHandler CalculateExecuteThresholdForViewer;
    #endregion

    #region internal utility methods
    private static void TryExecuteServer(CharacterBody victimBody, DamageReport damageReport)
    {
        HealthComponent victimHealth = victimBody.healthComponent;
        float victimHealthFraction = victimHealth.combinedHealthFraction;
        float executeFraction = CalculateExecuteFraction(victimBody, damageReport.attackerBody);

        if (executeFraction > 0f && victimHealthFraction <= executeFraction)
        {
            float executionHealthLost = Mathf.Max(victimHealth.combinedHealth, 0f);

            if (victimHealth.barrier > 0f) victimHealth.barrier = 0f;
            if (victimHealth.shield > 0f) victimHealth.shield = 0f;
            if (victimHealth.health > 0f) victimHealth.health = 0f;

            GlobalEventManager.ServerCharacterExecuted(damageReport, executionHealthLost);
        }
    }

    private static float ConvertAdditiveFractionToFlat(float executeFractionAdd)
    {
        return 1f - (1f / (1f + executeFractionAdd));
    }

    private static float CalculateExecuteFraction(CharacterBody victimBody, CharacterBody viewerBody)
    {
        float executeFractionAdd = 0f;
        float executeFractionFlat = 0f;

        ExecuteAPI.CalculateAdditiveExecuteThreshold?.Invoke(victimBody, ref executeFractionAdd);
        ExecuteAPI.CalculateExecuteThreshold?.Invoke(victimBody, ref executeFractionFlat);

        if (viewerBody)
        {
            ExecuteAPI.CalculateAdditiveExecuteThresholdForViewer?.Invoke(victimBody, viewerBody, ref executeFractionAdd);
            ExecuteAPI.CalculateExecuteThresholdForViewer?.Invoke(victimBody, viewerBody, ref executeFractionFlat);
        }
        return Mathf.Max(ExecuteAPI.ConvertAdditiveFractionToFlat(executeFractionAdd), executeFractionFlat);
    }

    private static HealthComponent.HealthBarValues UpdateHealthBarValues(CharacterBody victimBody, CharacterBody viewerBody, HealthComponent.HealthBarValues hbv)
    {
        if (victimBody && victimBody.healthComponent)
        {
            float executeFraction = CalculateExecuteFraction(victimBody, viewerBody);
            float healthbarFraction = (1f - hbv.curseFraction) / victimBody.healthComponent.fullCombinedHealth;
            float newCullFraction = Mathf.Clamp01(executeFraction * victimBody.healthComponent.fullCombinedHealth * healthbarFraction);

            //ExecuteAPI execute will not interact with non-ExecuteAPI executes.
            hbv.cullFraction = Mathf.Max(hbv.cullFraction, newCullFraction);
        }
        return hbv;
    }
    #endregion

    #region hooks
    private static void HealthBar_UpdateBarInfos(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int healthBarValueLoc = -1;
        if (c.TryGotoNext(MoveType.After, x => x.MatchLdloc(out int healthBarValueLoc), x => x.MatchLdfld<HealthComponent.HealthBarValues>("cullFraction")))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, healthBarValueLoc);
            c.EmitDelegate<Func<float,  HealthBar, HealthComponent.HealthBarValues, float>>((origCullFraction, self, healthBarValues) =>
            {
                if (self.source && self.source.body)
                {
                    healthBarValues = UpdateHealthBarValues(self.source.body, self.viewerBody, healthBarValues);
                    return Mathf.Max(origCullFraction, healthBarValues.cullFraction);
                }
                return origCullFraction;
            });
        }
        else
        {
            ExecutePlugin.Logger.LogError("HealthBar_UpdateBarInfos IL hook failed.");
        }
    }

    private static HealthComponent.HealthBarValues HealthComponent_GetHealthBarValues(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
    {
        var hbv = orig(self);
        hbv = UpdateHealthBarValues(self.body, null, hbv);
        return hbv;
    }

    private static void GlobalEventManager_ServerDamageDealt(On.RoR2.GlobalEventManager.orig_ServerDamageDealt orig, DamageReport damageReport)
    {
        orig(damageReport);
        if (NetworkServer.active
            && damageReport.victimBody
            && (damageReport.victimBody.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) == 0
            && damageReport.victimBody.healthComponent
            && damageReport.victimBody.healthComponent.alive)
        {
            TryExecuteServer(damageReport.victimBody, damageReport);
        }
    }
    #endregion
}
