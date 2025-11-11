using System;
using System.ComponentModel;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using RoR2BepInExPack.Utilities;

namespace R2API;

/// <summary>
/// API for computing bonuses granted by factors inside RecalculateStats.
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class RecalculateStatsAPI
{
    public class CustomStats
    {
        public bool barrierDecayFrozen = false;
        public float barrierDecayRateAdd = 0;
        public float barrierDecayRateMult = 1;

        public float luckAdd = 0;

        public float selfExecutionThresholdAdd = 0;
        public float selfExecutionThresholdBase = float.NegativeInfinity;

        internal void ResetStats()
        {
            barrierDecayFrozen = false;
            barrierDecayRateAdd = 0;
            barrierDecayRateMult = 1;

            luckAdd = 0;

            selfExecutionThresholdAdd = 0;
            selfExecutionThresholdBase = 0;
        }
    }

    public const string PluginGUID = R2API.PluginGUID + ".recalculatestats";
    public const string PluginName = R2API.PluginName + ".RecalculateStats";

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static bool _hooksEnabled = false;

    #region custom hooks
    static ILHook luckHook;
    internal static void InitHooks()
    {
        // Does nothing until the hook is applied in SetHooks
        var hookConfig = new ILHookConfig() { ManualApply = true };
        luckHook = new ILHook(typeof(CharacterMaster).GetMethod("get_luck"), ModifyLuckStat, ref hookConfig);
    }
    #endregion

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        IL.RoR2.CharacterBody.RecalculateStats += HookRecalculateStats;

        // Adding custom luck stat to CharacterMaster.get_luck
        luckHook.Apply();
        // Barrier Decay
        IL.RoR2.HealthComponent.ServerFixedUpdate += ModifyBarrierDecayRate;
        // Execution
        IL.RoR2.HealthComponent.TakeDamageProcess += ModifyExecutionThreshold;
        On.RoR2.HealthComponent.GetHealthBarValues += DisplayModifiedExecutionThreshold;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats -= HookRecalculateStats;
        luckHook.Undo();
        IL.RoR2.HealthComponent.ServerFixedUpdate -= ModifyBarrierDecayRate;
        IL.RoR2.HealthComponent.TakeDamageProcess -= ModifyExecutionThreshold;
        On.RoR2.HealthComponent.GetHealthBarValues -= DisplayModifiedExecutionThreshold;

        _hooksEnabled = false;
    }

    /// <summary>
    /// A collection of modifiers for various stats. It will be passed down the event chain of GetStatCoefficients; add to the contained values to modify stats.
    /// </summary>
    public class StatHookEventArgs : EventArgs
    {
        /// <remarks>(LEVEL - 1)</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const string _levelMultiplier = "Used for internal documentation";

        #region health
        /// <summary>Added to base health.</summary> <remarks>MAX_HEALTH ~ (BASE_HEALTH + baseHealthAdd + levelHealthAdd * <inheritdoc cref="_levelMultiplier"/>) * (HEALTH_MULT + healthMultAdd) * healthTotalMult</remarks>
        public float baseHealthAdd = 0f;

        /// <summary>Multiplied by character level and added to base health.</summary> <inheritdoc cref="baseHealthAdd"/>
        public float levelHealthAdd = 0f;

        /// <summary>Added to the direct multiplier to base health.</summary> <inheritdoc cref="baseHealthAdd"/>
        public float healthMultAdd = 0f;

        /// <summary>Base health is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseHealthAdd"/>
        public float healthTotalMult = 1f;
        #endregion

        #region shield
        /// <summary>Added to base shield.</summary> <remarks>MAX_SHIELD ~ (BASE_SHIELD + baseShieldAdd + levelShieldAdd * <inheritdoc cref="_levelMultiplier"/>) * (SHIELD_MULT + shieldMultAdd) * shieldTotalMult</remarks>remarks>
        public float baseShieldAdd = 0f;

        /// <summary>Multiplied by level and added to base shield.</summary> <inheritdoc cref="baseShieldAdd"/>
        public float levelShieldAdd = 0f;

        /// <summary>Added to the direct multiplier to shields.</summary> <inheritdoc cref="baseShieldAdd"/>
        public float shieldMultAdd = 0f;

        /// <summary>Base shield is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseShieldAdd"/>
        public float shieldTotalMult = 1f;
        #endregion

        #region regen

        /// <summary>Added to base health regen.</summary> <remarks>HEALTH_REGEN ~ (BASE_REGEN + baseRegenAdd + levelRegenAdd * <inheritdoc cref="_levelMultiplier"/>) * (REGEN_MULT + regenMultAdd) * regenTotalMult</remarks>
        public float baseRegenAdd = 0f;

        /// <summary>Multiplied by level and added to base health regen.</summary> <inheritdoc cref="baseRegenAdd"/>
        public float levelRegenAdd = 0f;

        /// <summary>Added to the direct multiplier to base health regen.</summary> <inheritdoc cref="baseRegenAdd"/>
        public float regenMultAdd = 0f;

        /// <summary>Base health regen is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseRegenAdd"/>
        public float regenTotalMult = 1f;
        #endregion

        #region moveSpeed
        /// <summary>Added to base move speed.</summary> <remarks>MOVE_SPEED ~ (BASE_MOVE_SPEED + baseMoveSpeedAdd + levelMoveSpeedAdd * <inheritdoc cref="_levelMultiplier"/>) * (MOVE_SPEED_MULT + moveSpeedMultAdd) * moveSpeedTotalMult / (MOVE_SPEED_REDUCTION_MULT + moveSpeedReductionMultAdd)</remarks>
        public float baseMoveSpeedAdd = 0f;

        /// <summary>Multiplied by level and added to base move speed.</summary> <inheritdoc cref="baseMoveSpeedAdd"/>
        public float levelMoveSpeedAdd = 0f;

        /// <summary>Added to the direct multiplier to move speed.</summary> <inheritdoc cref="baseMoveSpeedAdd"/>
        public float moveSpeedMultAdd = 0f;

        /// <summary>Base move speed is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseMoveSpeedAdd"/>
        public float moveSpeedTotalMult = 1f;

        /// <summary>Added reduction multiplier to move speed.</summary> <inheritdoc cref="baseMoveSpeedAdd"/>
        public float moveSpeedReductionMultAdd = 0f;

        /// <summary>Added to the direct multiplier to sprinting speed.</summary> <remarks>SPRINT SPEED ~ MOVE_SPEED * (BASE_SPRINT_MULT + sprintSpeedAdd) </remarks>
        public float sprintSpeedAdd = 0f;

        /// <summary>Amount of Root effects currently applied.</summary> <remarks>MOVE_SPEED ~ (moveSpeedRootCount > 0) ? 0 : MOVE_SPEED</remarks>
        public int moveSpeedRootCount = 0;
        #endregion

        #region jumpPower
        /// <summary>Added to base jump power.</summary> <remarks>JUMP_POWER ~ (BASE_JUMP_POWER + baseJumpPowerAdd + levelJumpPowerAdd * <inheritdoc cref="_levelMultiplier"/>) * (JUMP_POWER_MULT + jumpPowerMultAdd) * jumpPowerTotalMult</remarks>
        public float baseJumpPowerAdd = 0f;

        /// <summary>Multiplied by level and added to base jump power.</summary> <inheritdoc cref="baseJumpPowerAdd"/>
        public float levelJumpPowerAdd = 0f;

        /// <summary>Added to the direct multiplier to jump power.</summary> <inheritdoc cref="baseJumpPowerAdd"/>
        public float jumpPowerMultAdd = 0f;

        /// <summary>Base jump power is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseJumpPowerAdd"/>
        public float jumpPowerTotalMult = 1f;
        #endregion

        #region damage
        /// <summary>Added to base damage.</summary> <remarks>DAMAGE ~ (BASE_DAMAGE + baseDamageAdd + levelDamageAdd * <inheritdoc cref="_levelMultiplier"/>) * (DAMAGE_MULT + damageMultAdd) * damageTotalMult</remarks>
        public float baseDamageAdd = 0f;

        /// <summary>Multiplied by level and added to base damage.</summary> <inheritdoc cref="baseDamageAdd"/>
        public float levelDamageAdd = 0f;

        /// <summary>Added to the direct multiplier to base damage.</summary> <inheritdoc cref="baseDamageAdd"/>
        public float damageMultAdd = 0f;

        /// <summary>Base damage is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseDamageAdd"/>
        public float damageTotalMult = 1f;
        #endregion

        #region attackSpeed
        /// <summary>Added to attack speed.</summary> <remarks>ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd + levelAttackkSpeedAdd * <inheritdoc cref="_levelMultiplier"/>) * (ATTACK_SPEED_MULT + attackSpeedMultAdd) * attackSpeedTotalMult / (ATTACK_SPEED_REDUCTION_MULT + attackSpeedReductionMultAdd)</remarks>
        public float baseAttackSpeedAdd = 0f;

        /// <summary>Multiplied by level and added to attack speed.</summary> <inheritdoc cref="baseAttackSpeedAdd"/>
        public float levelAttackSpeedAdd = 0f;

        /// <summary>Added to the direct multiplier to attack speed.</summary> <inheritdoc cref="baseAttackSpeedAdd"/>
        public float attackSpeedMultAdd = 0f;

        /// <summary>Base attack speed is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseAttackSpeedAdd"/>
        public float attackSpeedTotalMult = 1f;

        /// <summary>Added reduction multiplier to attack speed.</summary> <inheritdoc cref="baseAttackSpeedAdd"/>
        public float attackSpeedReductionMultAdd = 0f;
        #endregion

        #region crit
        /// <summary>Added to crit chance.</summary> <remarks>CRIT_CHANCE ~ (BASE_CRIT_CHANCE + critAdd + levelCritAdd * <inheritdoc cref="_levelMultiplier"/>) * critTotalMult</remarks>
        public float critAdd = 0f;

        /// <summary>Multiplied by level and added to crit chance.</summary> <inheritdoc cref="critAdd"/>
        public float levelCritAdd = 0f;

        /// <summary>Crit chance is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="critAdd"/>
        public float critTotalMult = 1f;

        /// <summary>Added to the direct multiplier to crit damage.</summary> <remarks>CRIT_DAMAGE ~ DAMAGE * (BASE_CRIT_MULT + critDamageMultAdd) * critDamageTotalMult</remarks>
        public float critDamageMultAdd = 0;

        /// <summary>Crit damage is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="critDamageMultAdd"/>
        public float critDamageTotalMult = 1f;
        #endregion

        #region bleed
        /// <summary>Added to bleed chance.</summary> <remarks>BLEED_CHANCE ~ (BASE_BLEED_CHANCE + bleedChanceAdd) * bleedChanceMult</remarks>
        public float bleedChanceAdd = 0f;

        /// <summary>Bleed chance is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="bleedChanceAdd"/>
        public float bleedChanceMult = 1f;
        #endregion

        #region armor
        /// <summary>Added to armor.</summary> <remarks>ARMOR ~ (BASE_ARMOR + armorAdd + levelArmorAdd * <inheritdoc cref="_levelMultiplier"/>) * armorTotalMult</remarks>
        public float armorAdd = 0f;

        /// <summary>Multiplied by level and added to armor.</summary> <inheritdoc cref="armorAdd"/>
        public float levelArmorAdd = 0f;

        /// <summary>Armor is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="armorAdd"/>
        public float armorTotalMult = 1f;
        #endregion

        #region curse
        /// <summary> Added to Curse Penalty.</summary> <remarks><inheritdoc cref="baseHealthAdd"/> / ((BASE_CURSE_PENALTY + baseCurseAdd) * curseTotalMult)</remarks>
        public float baseCurseAdd = 0f;

        /// <summary>Curse penalty is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="baseCurseAdd"/>
        public float curseTotalMult = 1f;
        #endregion

        #region cooldowns
        /// <summary>Added to flat cooldown reduction.</summary> <remarks>COOLDOWN ~ BASE_COOLDOWN * (BASE_COOLDOWN_MULT + cooldownMultAdd) - (BASE_FLAT_REDUCTION + cooldownReductionAdd)</remarks>
        public float cooldownReductionAdd = 0f;

        /// <summary>Added to the direct multiplier to cooldown timers.</summary> <inheritdoc cref="cooldownReductionAdd"/>
        public float cooldownMultAdd = 0f;

        /// <summary>(Primary) Added to the direct multiplier to cooldown timers.</summary> <inheritdoc cref="cooldownReductionAdd"/>
        public float primaryCooldownMultAdd = 0f;

        /// <summary>(Secondary) Added to the direct multiplier to cooldown timers.</summary> <inheritdoc cref="cooldownReductionAdd"/>
        public float secondaryCooldownMultAdd = 0f;

        /// <summary>(Utility) Added to the direct multiplier to cooldown timers.</summary> <inheritdoc cref="cooldownReductionAdd"/>
        public float utilityCooldownMultAdd = 0f;

        /// <summary>(Special) Added to the direct multiplier to cooldown timers.</summary> <inheritdoc cref="cooldownReductionAdd"/>
        public float specialCooldownMultAdd = 0f;
        #endregion

        #region level
        /// <summary>Added to the body's current level.</summary> <remarks>EFFECTIVE LEVEL ~ (BASE LEVEL + levelFlatAdd * (BASE_LEVEL_SCALING + levelMultAdd) </remarks>
        public float levelFlatAdd = 0f;

        /// <summary>Added to the direct multiplier to level scaling.</summary> <inheritdoc cref="levelFlatAdd"/>
        public float levelMultAdd = 0f;
        #endregion

        #region barrier
        /// <summary>Amount of Barrier Freeze effects currently applied.</summary> <remarks>BARRIER_DECAY_RATE ~ (barrierFreezeCount > 0) ? 0 : BARRIER_DECAY_RATE</remarks>
        public int barrierFreezeCount = 0;

        /// <summary>Multiply to increase or decrease barrier decay rate.</summary> <remarks>BARRIER_DECAY_RATE ~ (BASE_DECAY_RATE + barrierDecayAdd) * (barrierDecayMult). Cannot be less than 0.</remarks>
        public float barrierDecayMult = 1;

        /// <summary>ADD to increase or decrease barrier decay rate. Expressed as a rate per second.</summary> <inheritdoc cref="barrierDecayMult"/>
        public float barrierDecayAdd = 0;
        #endregion

        #region luck
        /// <summary>Add to increase or decrease Luck. Can be negative.</summary> <remarks>LUCK ~ (MASTER_LUCK + luckAdd).</remarks>
        public float luckAdd = 0;
        #endregion

        #region execution
        /// <summary>Not to be touched directly. Use ModifySelfExecutionThreshold or selfExecutionThresholdAdd</summary> <remarks>EXECUTION_THRESHOLD ~ Max(BASE_THRESHOLD, newThreshold) + selfExecutionThresholdAdd. Cannot be less than 0.</remarks>
        internal float selfExecutionThresholdBase { get; private set; } = float.NegativeInfinity;

        /// <summary>Add to increase or decrease sender's execution threshold</summary> <inheritdoc cref="selfExecutionThresholdAdd"/>
        public float selfExecutionThresholdAdd = 0;

        /// <summary>Uses the largest execution threshold given (mimics vanilla's mutually-exclusive behavior).</summary> <inheritdoc cref="selfExecutionThresholdAdd"/>
        public float ModifySelfExecutionThreshold(float newThreshold) => _ModifySelfExecutionThreshold(newThreshold);
        //this was the ONLY way i could think of to circumvent the analyzer complaining about not setting hooks (its not even a hook :sob:)
        private float _ModifySelfExecutionThreshold(float newThreshold)
        {
            if (newThreshold <= 0 || selfExecutionThresholdBase >= 1)
                return selfExecutionThresholdBase;

            if (newThreshold > selfExecutionThresholdBase)
            {
                selfExecutionThresholdBase = newThreshold;
            }
            return selfExecutionThresholdBase;
        }
        #endregion
    }

    /// <summary>
    /// Used as the delegate type for the GetStatCoefficients event.
    /// </summary>
    /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
    /// <param name="args">An instance of StatHookEventArgs, passed to each subscriber to this event in turn for modification.</param>
    public delegate void StatHookEventHandler(CharacterBody sender, StatHookEventArgs args);

    private static event StatHookEventHandler _getStatCoefficients;

    /// <summary>
    /// Subscribe to this event to modify one of the stat hooks which StatHookEventArgs covers. Fired during CharacterBody.RecalculateStats.
    /// </summary>
    public static event StatHookEventHandler GetStatCoefficients
    {
        add
        {
            SetHooks();

            _getStatCoefficients += value;
        }

        remove
        {
            _getStatCoefficients -= value;

            if (_getStatCoefficients == null ||
                _getStatCoefficients.GetInvocationList()?.Length == 0)
            {
                UnsetHooks();
            }
        }
    }

    private static StatHookEventArgs StatMods;
    private static CustomStats BodyCustomStats;

    private static void HookRecalculateStats(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<CharacterBody>>(GetStatMods);

        FindLocLevelMultiplierIndex(c, out int locLevelMultiplierIndex);

        void EmitLevelMultiplier() => c.Emit(OpCodes.Ldloc, locLevelMultiplierIndex);

        void EmitFallbackLevelMultiplier() => c.Emit(OpCodes.Ldc_R4, 0f);

        Action emitLevelMultiplier = locLevelMultiplierIndex >= 0 ? EmitLevelMultiplier : EmitFallbackLevelMultiplier;

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<CharacterBody>>((body) => SetCustomStats(body));
        void SetCustomStats(CharacterBody body)
        {
            //get stats
            BodyCustomStats = GetCustomStatsFromBody(body);
            BodyCustomStats.ResetStats();

            BodyCustomStats.luckAdd = StatMods.luckAdd;

            BodyCustomStats.barrierDecayFrozen = StatMods.barrierFreezeCount > 0;
            BodyCustomStats.barrierDecayRateMult = StatMods.barrierDecayMult;
            if (BodyCustomStats.barrierDecayRateMult < 0)
                BodyCustomStats.barrierDecayRateMult = 0;
            BodyCustomStats.barrierDecayRateAdd = StatMods.barrierDecayAdd;
        }


        ModifyHealthStat(c, emitLevelMultiplier);
        ModifyShieldStat(c, emitLevelMultiplier);
        ModifyHealthRegenStat(c, emitLevelMultiplier);
        ModifyMovementSpeedStat(c, emitLevelMultiplier);
        ModifyJumpPowerStat(c, emitLevelMultiplier);
        ModifyDamageStat(c, emitLevelMultiplier);
        ModifyAttackSpeedStat(c, emitLevelMultiplier);
        ModifyCritStat(c, emitLevelMultiplier);
        ModifyBleedStat(c);
        ModifyArmorStat(c, emitLevelMultiplier);
        ModifyCurseStat(c);
        ModifyCooldownStat(c);
        ModifyLevelingStat(c);
    }

    private static void GetStatMods(CharacterBody characterBody)
    {
        StatMods = new StatHookEventArgs();

        if (_getStatCoefficients != null)
        {
            foreach (StatHookEventHandler @event in _getStatCoefficients.GetInvocationList())
            {
                try
                {
                    @event(characterBody, StatMods);
                }
                catch (Exception e)
                {
                    RecalculateStatsPlugin.Logger.LogError(
                        $"Exception thrown by : {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                }
            }
        }
    }

    private static void FindLocLevelMultiplierIndex(ILCursor c, out int locLevelMultiplierIndex)
    {
        c.Index = 0;
        int _locLevelMultiplierIndex = -1;
        c.TryGotoNext(
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.level))),
            x => x.MatchLdcR4(1),
            x => x.MatchSub(),
            x => x.MatchStloc(out _locLevelMultiplierIndex)
        );
        locLevelMultiplierIndex = _locLevelMultiplierIndex;
        if (locLevelMultiplierIndex < 0)
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(FindLocLevelMultiplierIndex)} failed! Level-scaled stats will be ignored!");
        }
    }

    private static void ModifyCurseStat(ILCursor c)
    {
        c.Index = 0;

        bool ILFound = c.TryGotoNext(MoveType.After,
            x => x.MatchLdcR4(10),
            x => x.MatchMul(),
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.cursePenalty))
            ));

        if (ILFound)
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterBody>>((body) => { body.cursePenalty += StatMods.baseCurseAdd; body.cursePenalty *= StatMods.curseTotalMult; });
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyCurseStat)} failed.");
        }
    }

    private static void ModifyCooldownStat(ILCursor c)
    {
        c.Index = 0;
        int ILFound = 0;
        while (c.TryGotoNext(
                   x => x.MatchCallOrCallvirt(
                       typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale)))
               ) && c.TryGotoNext(
                   x => x.MatchCallOrCallvirt(
                       typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction)))
               ))
        {
            ILFound++;
        }

        if (ILFound >= 4)
        {
            c.Index = 0;
            c.GotoNext(x =>
                x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.primaryCooldownMultAdd);
            });
            c.GotoNext(x =>
                x.MatchCallOrCallvirt(
                    typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown + StatMods.cooldownReductionAdd;
            });

            c.GotoNext(x =>
                x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.secondaryCooldownMultAdd);
            });
            c.GotoNext(x =>
                x.MatchCallOrCallvirt(
                    typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown + StatMods.cooldownReductionAdd;
            });

            c.GotoNext(x =>
                x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.utilityCooldownMultAdd);
            });
            c.GotoNext(x =>
                x.MatchCallOrCallvirt(
                    typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown + StatMods.cooldownReductionAdd;
            });


            c.GotoNext(x =>
                x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.specialCooldownMultAdd);
            });
            c.GotoNext(x =>
                x.MatchCallOrCallvirt(
                    typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
            c.EmitDelegate<Func<float, float>>((oldCooldown) =>
            {
                return oldCooldown + StatMods.cooldownReductionAdd;
            });
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyCooldownStat)} failed.");
        }
    }

    private static void ModifyLevelingStat(ILCursor c)
    {
        c.Index = 0;
        bool ILFound = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.level))),
            x => x.MatchLdcR4(1),
            x => x.MatchSub()
        );

        if (ILFound)
        {
            c.EmitDelegate<Func<float, float>>((oldScaling) =>
            {
                return (oldScaling + StatMods.levelFlatAdd) * (1 + StatMods.levelMultAdd);
            });
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyLevelingStat)} failed.");
        }
    }

    private static void ModifyArmorStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        bool ILFound = c.TryGotoNext(
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseArmor))
        ) && c.TryGotoNext(
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.armor)))
        );

        if (ILFound)
        {
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.armorAdd + StatMods.levelArmorAdd * levelMultiplier);
            c.Emit(OpCodes.Add);

            c.GotoNext(x => x.MatchLdsfld(typeof(DLC1Content.Buffs), nameof(DLC1Content.Buffs.PermanentDebuff)));
            c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.armor))));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterBody>>((body) => body.armor *= StatMods.armorTotalMult);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyArmorStat)} failed.");
        }
    }

    private static void ModifyAttackSpeedStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        int locBaseAttackSpeedIndex = -1;
        int locAttackSpeedMultIndex = -1;
        bool ILFound = c.TryGotoNext(
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseAttackSpeed)),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelAttackSpeed))
        ) && c.TryGotoNext(
            x => x.MatchStloc(out locBaseAttackSpeedIndex)
        ) && c.TryGotoNext(
            x => x.MatchLdloc(locBaseAttackSpeedIndex),
            x => x.MatchLdloc(out locAttackSpeedMultIndex),
            x => x.MatchMul(),
            x => x.MatchStloc(locBaseAttackSpeedIndex)
        );

        if (ILFound)
        {
            c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseAttackSpeed)));
            c.GotoNext(x => x.MatchStloc(locBaseAttackSpeedIndex));
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.baseAttackSpeedAdd + StatMods.levelAttackSpeedAdd * levelMultiplier);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.attackSpeedTotalMult);
            c.Emit(OpCodes.Mul);

            c.GotoNext(x => x.MatchStloc(locAttackSpeedMultIndex));
            c.EmitDelegate<Func<float>>(() => StatMods.attackSpeedMultAdd);
            c.Emit(OpCodes.Add);


            c.GotoNext(x => x.MatchDiv(), x => x.MatchStloc(locAttackSpeedMultIndex));
            c.EmitDelegate<Func<float, float>>((origSpeedReductionMult) => UnityEngine.Mathf.Max(UnityEngine.Mathf.Epsilon, origSpeedReductionMult + StatMods.attackSpeedReductionMultAdd));
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyAttackSpeedStat)} failed.");
        }
    }

    private static void ModifyCritStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        int locOrigCrit = -1;
        bool ILFound = c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdloc(out locOrigCrit),
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.crit)))
        ) && c.TryGotoPrev(MoveType.After,
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.critMultiplier)))
        );

        if (ILFound)
        {
            c.Index--;
            c.EmitDelegate<Func<float>>(() => StatMods.critDamageMultAdd);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.critDamageTotalMult);
            c.Emit(OpCodes.Mul);

            c.GotoNext(MoveType.After, x => x.MatchStloc(locOrigCrit));
            c.Emit(OpCodes.Ldloc, locOrigCrit);
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.critAdd + StatMods.levelCritAdd * levelMultiplier);
            c.Emit(OpCodes.Add);
            c.EmitDelegate<Func<float>>(() => StatMods.critTotalMult);
            c.Emit(OpCodes.Mul);
            c.Emit(OpCodes.Stloc, locOrigCrit);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyCritStat)} failed.");
        }
    }

    private static void ModifyBleedStat(ILCursor c)
    {
        c.Index = 0;

        bool ILFound = c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchLdcR4(10),
            x => x.MatchLdloc(out _),
            x => x.MatchConvR4(),
            x => x.MatchMul(),
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.bleedChance)))
        );

        if (ILFound)
        {
            c.Index--;
            c.EmitDelegate<Func<float>>(() => StatMods.bleedChanceAdd);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.bleedChanceMult);
            c.Emit(OpCodes.Mul);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyBleedStat)} failed.");
        }
    }

    private static void ModifyDamageStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        int locBaseDamageIndex = -1;
        int locDamageMultIndex = -1;
        bool ILFound = c.TryGotoNext(
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseDamage)),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelDamage))
        ) && c.TryGotoNext(
            x => x.MatchStloc(out locBaseDamageIndex)
        ) && c.TryGotoNext(
            x => x.MatchLdloc(locBaseDamageIndex),
            x => x.MatchLdloc(out locDamageMultIndex),
            x => x.MatchMul(),
            x => x.MatchStloc(locBaseDamageIndex)
        );

        if (ILFound)
        {
            c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseDamage)));
            c.GotoNext(x => x.MatchStloc(locBaseDamageIndex));
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.baseDamageAdd + StatMods.levelDamageAdd * levelMultiplier);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.damageTotalMult);
            c.Emit(OpCodes.Mul);

            c.GotoNext(x => x.MatchStloc(locDamageMultIndex));
            c.EmitDelegate<Func<float, float>>((origDamageMult) =>
            {
                return origDamageMult + StatMods.damageMultAdd;
            });
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyDamageStat)} failed.");
        }
    }

    private static void ModifyJumpPowerStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        bool ILFound = c.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseJumpPower)),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelJumpPower)),
            x => x.MatchLdloc(out _),
            x => x.MatchMul(),
            x => x.MatchAdd());

        if (ILFound)
        {
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float, float>>((origJumpPower, levelMultiplier) =>
            {
                return (origJumpPower + StatMods.baseJumpPowerAdd + StatMods.levelJumpPowerAdd * levelMultiplier) * (1 + StatMods.jumpPowerMultAdd) * StatMods.jumpPowerTotalMult;
            });
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyJumpPowerStat)} failed.");
        }
    }

    private static void ModifyHealthStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        int locBaseHealthIndex = -1;
        int locHealthMultIndex = -1;
        bool ILFound = c.TryGotoNext(
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseMaxHealth)),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMaxHealth))
        ) && c.TryGotoNext(
            x => x.MatchStloc(out locBaseHealthIndex)
        ) && c.TryGotoNext(
            x => x.MatchLdloc(locBaseHealthIndex),
            x => x.MatchLdloc(out locHealthMultIndex),
            x => x.MatchMul(),
            x => x.MatchStloc(locBaseHealthIndex)
        );

        if (ILFound)
        {
            c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseMaxHealth)));
            c.GotoNext(x => x.MatchStloc(locBaseHealthIndex));
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.baseHealthAdd + StatMods.levelHealthAdd * levelMultiplier);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.healthTotalMult);
            c.Emit(OpCodes.Mul);

            c.GotoNext(x => x.MatchStloc(locHealthMultIndex));
            c.EmitDelegate<Func<float>>(() => StatMods.healthMultAdd);
            c.Emit(OpCodes.Add);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyHealthStat)} failed.");
        }
    }

    private static void ModifyShieldStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        int locBaseShieldIndex = -1;
        bool ILFound = c.TryGotoNext(
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseMaxShield)),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMaxShield))
        ) && c.TryGotoNext(
            x => x.MatchStloc(out locBaseShieldIndex)
        ) && c.TryGotoNext(
            x => x.MatchLdloc(locBaseShieldIndex),
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.maxShield)))
        );

        if (ILFound)
        {
            c.Index++;
            c.EmitDelegate<Func<float>>(() => 1 + StatMods.shieldMultAdd);
            c.Emit(OpCodes.Mul);

            c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMaxShield)));
            c.GotoNext(x => x.MatchStloc(out locBaseShieldIndex));
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.baseShieldAdd + StatMods.levelShieldAdd * levelMultiplier);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.shieldTotalMult);
            c.Emit(OpCodes.Mul);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyShieldStat)} failed.");
        }
    }

    private static void ModifyHealthRegenStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        int locRegenMultIndex = -1;
        int locFinalRegenIndex = -1;
        bool ILFound = c.TryGotoNext(
            x => x.MatchLdloc(out locFinalRegenIndex),
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.regen)))
        ) && c.TryGotoPrev(
            x => x.MatchAdd(),
            x => x.MatchLdloc(out locRegenMultIndex),
            x => x.MatchMul(),
            x => x.MatchStloc(out locFinalRegenIndex)
        );

        if (ILFound)
        {
            c.GotoNext(x => x.MatchLdloc(out locRegenMultIndex));
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.baseRegenAdd + StatMods.levelRegenAdd * levelMultiplier);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.regenTotalMult);
            c.Emit(OpCodes.Mul);

            c.GotoNext(x => x.MatchMul());
            c.EmitDelegate<Func<float>>(() => StatMods.regenMultAdd);
            c.Emit(OpCodes.Add);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyHealthRegenStat)} failed.");
        }
    }

    private static void ModifyMovementSpeedStat(ILCursor c, Action emitLevelMultiplier)
    {
        c.Index = 0;

        int locBaseSpeedIndex = -1;
        int locSpeedMultIndex = -1;
        int locSpeedDivIndex = -1;
        bool ILFound = c.TryGotoNext(
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseMoveSpeed)),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMoveSpeed))
        ) && c.TryGotoNext(
            x => x.MatchStloc(out locBaseSpeedIndex)
        ) && c.TryGotoNext(
            x => x.MatchLdloc(locBaseSpeedIndex),
            x => x.MatchLdloc(out locSpeedMultIndex),
            x => x.MatchLdloc(out locSpeedDivIndex),
            x => x.MatchDiv(),
            x => x.MatchMul(),
            x => x.MatchStloc(locBaseSpeedIndex)
        ) && c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out _),
            x => x.MatchOr(),
            x => x.MatchLdloc(out _),
            x => x.MatchOr()
        );

        if (ILFound)
        {
            c.EmitDelegate<Func<bool>>(() => StatMods.moveSpeedRootCount > 0);
            c.Emit(OpCodes.Or);
            c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMoveSpeed)));
            c.GotoNext(x => x.MatchStloc(locBaseSpeedIndex));
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.baseMoveSpeedAdd + StatMods.levelMoveSpeedAdd * levelMultiplier);
            c.Emit(OpCodes.Add);

            c.EmitDelegate<Func<float>>(() => StatMods.moveSpeedTotalMult);
            c.Emit(OpCodes.Mul);

            c.GotoNext(x => x.MatchStloc(locSpeedMultIndex));
            c.EmitDelegate<Func<float>>(() => StatMods.moveSpeedMultAdd);
            c.Emit(OpCodes.Add);

            while (c.TryGotoNext(MoveType.After,x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.sprintingSpeedMultiplier)))){
                c.EmitDelegate<Func<float>>(() => StatMods.sprintSpeedAdd);
                c.Emit(OpCodes.Add);
            }

            c.GotoPrev(MoveType.After,x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.isSprinting))));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool,CharacterBody,bool>>((isSprinting,sender) =>{ return isSprinting && ((sender.sprintingSpeedMultiplier + StatMods.sprintSpeedAdd) != 0); });
            c.GotoNext(x => x.MatchStloc(locSpeedDivIndex));
            c.EmitDelegate<Func<float>>(() => StatMods.moveSpeedReductionMultAdd);
            c.Emit(OpCodes.Add);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyMovementSpeedStat)} failed.");
        }
    }

    private static void ModifyBarrierDecayRate(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        bool ILFound = c.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<HealthComponent>("barrier"),
            x => x.MatchLdcR4(out _)
            );
        if (ILFound)
        {
            bool ILFound2 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_barrierDecayRate")
                );
            if (ILFound2)
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, HealthComponent, float>>((barrierDecayRate, healthComponent) =>
                {
                    CustomStats stats = GetCustomStatsFromBody(healthComponent.body);
                    if (stats == null)
                        return barrierDecayRate;

                    barrierDecayRate += stats.barrierDecayRateAdd;
                    if (stats.barrierDecayFrozen || barrierDecayRate < 0)
                        barrierDecayRate = 0;
                    else
                        barrierDecayRate *= stats.barrierDecayRateMult;

                    return barrierDecayRate;
                });
            }
            else
            {
                RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyBarrierDecayRate)} failed.");
            }
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyBarrierDecayRate)} failed.");
        }
    }

    private static void ModifyLuckStat(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        c.Goto(il.Instrs.Last());
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Single, CharacterMaster, Single>>((baseLuck, master) =>
        {
            if (master == null || !master.hasBody)
                return baseLuck;

            CharacterBody body = master.GetBody();
            if (body == null)
                return baseLuck;
            CustomStats customStats = GetCustomStatsFromBody(body);
            float newLuck = baseLuck + customStats.luckAdd;
            float remainder = newLuck % 1;
            if (remainder < 0)
                remainder += 1;
            if (remainder > Single.Epsilon && Util.CheckRoll(remainder * 100, 0))
            {
                newLuck = (float)Math.Ceiling(newLuck);
            }
            else
            {
                newLuck = (float)Math.Floor(newLuck);
            }
            return newLuck;
        });
    }

    #region execution
    private static void ModifyExecutionThreshold(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        int thresholdPosition = 0;

        bool ILFound = c.TryGotoNext(MoveType.After,
            x => x.MatchLdcR4(float.NegativeInfinity),
            x => x.MatchStloc(out thresholdPosition)
            )
         && c.TryGotoNext(MoveType.Before,
            x => x.MatchLdarg(0),
            x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
            );

        if (ILFound)
        {
            c.Emit(OpCodes.Ldloc, thresholdPosition);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((currentThreshold, hc) =>
            {
                float newThreshold = currentThreshold;

                newThreshold = RecalculateExecutionThreshold(currentThreshold, hc);

                return newThreshold;
            });
            c.Emit(OpCodes.Stloc, thresholdPosition);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyExecutionThreshold)} failed.");
        }
    }

    private static float RecalculateExecutionThreshold(float currentThreshold, HealthComponent healthComponent, float mult = 1)
    {
        CharacterBody body = healthComponent.body;

        if (body != null)
        {
            if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
            {
                MoreStats stats = GetMoreStatsFromBody(body);
                float t = Math.Max(currentThreshold, stats.selfExecutionThresholdBase * mult);
                return t + stats.selfExecutionThresholdAdd;
            }
        }

        return currentThreshold;
    }

    private static HealthComponent.HealthBarValues DisplayModifiedExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
    {
        HealthComponent.HealthBarValues values = orig(self);

        float maxHealthFractionClamped = Math.Clamp(1f - (1f - 1f / self.body.cursePenalty), 0, 1);
        float threshold = RecalculateExecutionThreshold(values.cullFraction, self, maxHealthFractionClamped);
        values.cullFraction = Math.Clamp(threshold, 0, 1);

        return values;
    }
    #endregion

    #region custom stats
    public static FixedConditionalWeakTable<CharacterBody, CustomStats> characterCustomStats = new FixedConditionalWeakTable<CharacterBody, CustomStats>();
    internal static CustomStats GetCustomStatsFromBody(CharacterBody body)
    {
        if (body == null)
            return null;
        return characterCustomStats.GetOrCreateValue(body);
    }
    #endregion
}
