using System;
using System.ComponentModel;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;

namespace R2API;

/// <summary>
/// API for computing bonuses granted by factors inside RecalculateStats.
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class RecalculateStatsAPI
{
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

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        IL.RoR2.CharacterBody.RecalculateStats += HookRecalculateStats;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats -= HookRecalculateStats;

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
        /// <summary>Added to base health.</summary> <remarks>MAX_HEALTH ~ (BASE_HEALTH + baseHealthAdd + levelHealthAdd * <inheritdoc cref="_levelMultiplier"/>) * (HEALTH_MULT + healthMultAdd)</remarks>
        public float baseHealthAdd = 0f;

        /// <summary>Multiplied by character level and added to base health.</summary> <inheritdoc cref="baseHealthAdd"/>
        public float levelHealthAdd = 0f;

        /// <summary>Added to the direct multiplier to base health.</summary> <inheritdoc cref="baseHealthAdd"/>
        public float healthMultAdd = 0f;
        #endregion

        #region shield
        /// <summary>Added to base shield.</summary> <remarks>MAX_SHIELD ~ (BASE_SHIELD + baseShieldAdd + levelShieldAdd * <inheritdoc cref="_levelMultiplier"/>) * (SHIELD_MULT + shieldMultAdd)</remarks>remarks>
        public float baseShieldAdd = 0f;

        /// <summary>Multiplied by level and added to base shield.</summary> <inheritdoc cref="baseShieldAdd"/>
        public float levelShieldAdd = 0f;

        /// <summary>Added to the direct multiplier to shields.</summary> <inheritdoc cref="baseShieldAdd"/>
        public float shieldMultAdd = 0f;
        #endregion

        #region regen

        /// <summary>Added to base health regen.</summary> <remarks>HEALTH_REGEN ~ (BASE_REGEN + baseRegenAdd + levelRegenAdd * <inheritdoc cref="_levelMultiplier"/>) * (REGEN_MULT + regenMultAdd)</remarks>
        public float baseRegenAdd = 0f;

        /// <summary>Multiplied by level and added to base health regen.</summary> <inheritdoc cref="baseRegenAdd"/>
        public float levelRegenAdd = 0f;

        /// <summary>Added to the direct multiplier to base health regen.</summary> <inheritdoc cref="baseRegenAdd"/>
        public float regenMultAdd = 0f;
        #endregion

        #region moveSpeed
        /// <summary>Added to base move speed.</summary> <remarks>MOVE_SPEED ~ (BASE_MOVE_SPEED + baseMoveSpeedAdd + levelMoveSpeedAdd * <inheritdoc cref="_levelMultiplier"/>) * (MOVE_SPEED_MULT + moveSpeedMultAdd) / (MOVE_SPEED_REDUCTION_MULT + moveSpeedReductionMultAdd)</remarks>
        public float baseMoveSpeedAdd = 0f;

        /// <summary>Multiplied by level and added to base move speed.</summary> <inheritdoc cref="baseMoveSpeedAdd"/>
        public float levelMoveSpeedAdd = 0f;

        /// <summary>Added to the direct multiplier to move speed.</summary> <inheritdoc cref="baseMoveSpeedAdd"/>
        public float moveSpeedMultAdd = 0f;

        /// <summary>Added reduction multiplier to move speed.</summary> <inheritdoc cref="baseMoveSpeedAdd"/>
        public float moveSpeedReductionMultAdd = 0f;

        /// <summary>Added to the direct multiplier to sprinting speed.</summary> <remarks>SPRINT SPEED ~ MOVE_SPEED * (BASE_SPRINT_MULT + sprintSpeedAdd) </remarks>
        public float sprintSpeedAdd = 0f;

        /// <summary>Amount of Root effects currently applied.</summary> <remarks>MOVE_SPEED ~ (moveSpeedRootCount > 0) ? 0 : MOVE_SPEED</remarks>
        public int moveSpeedRootCount = 0;
        #endregion

        #region jumpPower
        /// <summary>Added to base jump power.</summary> <remarks>JUMP_POWER ~ (BASE_JUMP_POWER + baseJumpPowerAdd + levelJumpPowerAdd * <inheritdoc cref="_levelMultiplier"/>) * (JUMP_POWER_MULT + jumpPowerMultAdd)</remarks>
        public float baseJumpPowerAdd = 0f;

        /// <summary>Multiplied by level and added to base jump power.</summary> <inheritdoc cref="baseJumpPowerAdd"/>
        public float levelJumpPowerAdd = 0f;

        /// <summary>Added to the direct multiplier to jump power.</summary> <inheritdoc cref="baseJumpPowerAdd"/>
        public float jumpPowerMultAdd = 0f;
        #endregion

        #region damage
        /// <summary>Added to base damage.</summary> <remarks>DAMAGE ~ (BASE_DAMAGE + baseDamageAdd + levelDamageAdd * <inheritdoc cref="_levelMultiplier"/>) * (DAMAGE_MULT + damageMultAdd)</remarks>
        public float baseDamageAdd = 0f;

        /// <summary>Multiplied by level and added to base damage.</summary> <inheritdoc cref="baseDamageAdd"/>
        public float levelDamageAdd = 0f;

        /// <summary>Added to the direct multiplier to base damage.</summary> <inheritdoc cref="baseDamageAdd"/>
        public float damageMultAdd = 0f;
        #endregion

        #region attackSpeed
        /// <summary>Added to attack speed.</summary> <remarks>ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd + levelAttackkSpeedAdd * <inheritdoc cref="_levelMultiplier"/>) * (ATTACK_SPEED_MULT + attackSpeedMultAdd) / (ATTACK_SPEED_REDUCTION_MULT + attackSpeedReductionMultAdd)</remarks>
        public float baseAttackSpeedAdd = 0f;

        /// <summary>Multiplied by level and added to attack speed.</summary> <inheritdoc cref="baseAttackSpeedAdd"/>
        public float levelAttackSpeedAdd = 0f;

        /// <summary>Added to the direct multiplier to attack speed.</summary> <inheritdoc cref="baseAttackSpeedAdd"/>
        public float attackSpeedMultAdd = 0f;

        /// <summary>Added reduction multiplier to attack speed.</summary> <inheritdoc cref="baseAttackSpeedAdd"/>
        public float attackSpeedReductionMultAdd = 0f;
        #endregion

        #region crit
        /// <summary>Added to crit chance.</summary> <remarks>CRIT_CHANCE ~ BASE_CRIT_CHANCE + critAdd + levelCritAdd * <inheritdoc cref="_levelMultiplier"/></remarks>
        public float critAdd = 0f;

        /// <summary>Multiplied by level and added to crit chance.</summary> <inheritdoc cref="critAdd"/>
        public float levelCritAdd = 0f;

        /// <summary>Added to the direct multiplier to crit damage.</summary> <remarks>CRIT_DAMAGE ~ DAMAGE * (BASE_CRIT_MULT + critDamageMultAdd) </remarks>
        public float critDamageMultAdd = 0;
        #endregion

        #region armor
        /// <summary>Added to armor.</summary> <remarks>ARMOR ~ BASE_ARMOR + armorAdd + levelArmorAdd * <inheritdoc cref="_levelMultiplier"/></remarks>
        public float armorAdd = 0f;

        /// <summary>Multiplied by level and added to armor.</summary> <inheritdoc cref="armorAdd"/>
        public float levelArmorAdd = 0f;
        #endregion

        #region curse
        /// <summary> Added to Curse Penalty.</summary> <remarks><inheritdoc cref="baseHealthAdd"/> / (BASE_CURSE_PENALTY + baseCurseAdd)</remarks>
        public float baseCurseAdd = 0f;
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

    private static void HookRecalculateStats(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<CharacterBody>>(GetStatMods);

        FindLocLevelMultiplierIndex(c, out int locLevelMultiplierIndex);

        void EmitLevelMultiplier() => c.Emit(OpCodes.Ldloc, locLevelMultiplierIndex);

        void EmitFallbackLevelMultiplier() => c.Emit(OpCodes.Ldc_R4, 0f);

        Action emitLevelMultiplier = locLevelMultiplierIndex >= 0 ? EmitLevelMultiplier : EmitFallbackLevelMultiplier;

        ModifyHealthStat(c, emitLevelMultiplier);
        ModifyShieldStat(c, emitLevelMultiplier);
        ModifyHealthRegenStat(c, emitLevelMultiplier);
        ModifyMovementSpeedStat(c, emitLevelMultiplier);
        ModifyJumpStat(c, emitLevelMultiplier);
        ModifyDamageStat(c, emitLevelMultiplier);
        ModifyAttackSpeedStat(c, emitLevelMultiplier);
        ModifyCritStat(c, emitLevelMultiplier);
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
            c.EmitDelegate<Action<CharacterBody>>((body) => { body.cursePenalty += StatMods.baseCurseAdd; });
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
        ) && c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.armor)))
        );

        if (ILFound)
        {
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.armorAdd + StatMods.levelArmorAdd * levelMultiplier);
            c.Emit(OpCodes.Add);
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

            c.GotoNext(MoveType.After, x => x.MatchStloc(locOrigCrit));
            c.Emit(OpCodes.Ldloc, locOrigCrit);
            emitLevelMultiplier();
            c.EmitDelegate<Func<float, float>>((levelMultiplier) => StatMods.critAdd + StatMods.levelCritAdd * levelMultiplier);
            c.Emit(OpCodes.Add);
            c.Emit(OpCodes.Stloc, locOrigCrit);
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyCritStat)} failed.");
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

    private static void ModifyJumpStat(ILCursor c, Action emitLevelMultiplier)
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
                return (origJumpPower + StatMods.baseJumpPowerAdd + StatMods.levelJumpPowerAdd * levelMultiplier) * (1 + StatMods.jumpPowerMultAdd);
            });
        }
        else
        {
            RecalculateStatsPlugin.Logger.LogError($"{nameof(ModifyJumpStat)} failed.");
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
}
