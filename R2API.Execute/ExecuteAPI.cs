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
        On.RoR2.GlobalEventManager.ServerDamageDealt -= GlobalEventManager_ServerDamageDealt;
        On.RoR2.HealthComponent.GetHealthBarValues -= HealthComponent_GetHealthBarValues;
        IL.RoR2.UI.HealthBar.UpdateBarInfos -= HealthBar_UpdateBarInfos;
        _hooksEnabled = false;
    }
    #endregion

    #region public-facing methods
    public delegate void CalculateExecuteThresholdEventHandler(CharacterBody victimBody, ref float executeFractionAdd);

    [Tooltip("Calculates the additive execute threshold. Final threshold is calculated by the function: 1 - 1 / (1 + executeFractionAdd)")]
    public static CalculateExecuteThresholdEventHandler CalculateExecuteThreshold;

    public delegate void CalculateExecuteThresholdForViewerEventHandler(CharacterBody victimBody, CharacterBody viewerBody, ref float executeFractionAdd);

    [Tooltip("Calculates the additive execute threshold, factoring in viewer bodies. Final threshold is calculated by the function: 1 - 1 / (1 + executeFractionAdd)")]
    public static CalculateExecuteThresholdForViewerEventHandler CalculateExecuteThresholdForViewer;
    #endregion

    #region internal utility methods
    private static void TryExecuteServer(CharacterBody victimBody, DamageReport damageReport)
    {
        HealthComponent victimHealth = victimBody.healthComponent;
        float executeFractionAdd = 0f;
        ExecuteAPI.CalculateExecuteThreshold?.Invoke(victimBody, ref executeFractionAdd);
        if (damageReport.attackerBody) ExecuteAPI.CalculateExecuteThresholdForViewer?.Invoke(victimBody, damageReport.attackerBody, ref executeFractionAdd);

        float victimHealthFraction = victimHealth.combinedHealthFraction;
        float executeFraction = ExecuteAPI.GetFlatExecuteFraction(executeFractionAdd);

        if (executeFraction > 0f && victimHealthFraction <= executeFraction)
        {
            float executionHealthLost = Mathf.Max(victimHealth.combinedHealth, 0f);

            if (victimHealth.barrier > 0f) victimHealth.barrier = 0f;
            if (victimHealth.shield > 0f) victimHealth.shield = 0f;
            if (victimHealth.health > 0f) victimHealth.health = 0f;

            GlobalEventManager.ServerCharacterExecuted(damageReport, executionHealthLost);
        }
    }

    private static float GetFlatExecuteFraction(float executeFractionAdd)
    {
        return 1f - (1f / (1f + executeFractionAdd));
    }

    private static HealthComponent.HealthBarValues UpdateHealthBarValues(CharacterBody victimBody, CharacterBody viewerBody, HealthComponent.HealthBarValues hbv)
    {
        if (victimBody && victimBody.healthComponent)
        {
            float executeFractionAdd = 0f;
            ExecuteAPI.CalculateExecuteThreshold?.Invoke(victimBody, ref executeFractionAdd);
            if (viewerBody) ExecuteAPI.CalculateExecuteThresholdForViewer?.Invoke(victimBody, viewerBody, ref executeFractionAdd);
            float executeFraction = ExecuteAPI.GetFlatExecuteFraction(executeFractionAdd);
            float healthbarFraction = (1f - hbv.curseFraction) / victimBody.healthComponent.fullCombinedHealth;

            float newCullFraction = Mathf.Clamp01(executeFraction * victimBody.healthComponent.fullCombinedHealth * healthbarFraction);

            //ExecuteAPI execute will not interact with non-ExecuteAPI executes.
            if (hbv.cullFraction < newCullFraction) hbv.cullFraction = newCullFraction;
        }
        return hbv;
    }
    #endregion

    #region hooks
    private static void HealthBar_UpdateBarInfos(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int healthBarValueLoc = -1;
        if (c.TryGotoNext(x => x.MatchLdloc(out healthBarValueLoc), x => x.MatchLdfld<HealthComponent.HealthBarValues>("cullFraction"))
            && healthBarValueLoc >= 0
            && c.TryGotoNext(x => x.MatchStfld<HealthBar.BarInfo>("normalizedXMax")))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, healthBarValueLoc);
            c.EmitDelegate<Func<float, HealthBar, HealthComponent.HealthBarValues, float>>((originalCullFraction, self, healthBarValues) =>
            {
                if (self.source && self.source.body)
                {
                    healthBarValues = UpdateHealthBarValues(self.source.body, self.viewerBody, healthBarValues);
                }
                return Mathf.Max(originalCullFraction, healthBarValues.cullFraction);
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
