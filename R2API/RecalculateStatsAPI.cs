﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;

namespace R2API {

    /// <summary>
    /// API for computing bonuses granted by factors inside RecalculateStats.
    /// </summary>
    [R2APISubmodule]
    public static class RecalculateStatsAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.CharacterBody.RecalculateStats += HookRecalculateStats;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.CharacterBody.RecalculateStats -= HookRecalculateStats;
        }

        /// <summary>
        /// A collection of modifiers for various stats. It will be passed down the event chain of GetStatCoefficients; add to the contained values to modify stats.
        /// </summary>
        public class StatHookEventArgs : EventArgs {

            /// <summary>Added to the direct multiplier to base health. MAX_HEALTH ~ (BASE_HEALTH + baseHealthAdd) * (HEALTH_MULT + healthMultAdd).</summary>
            public float healthMultAdd = 0f;

            /// <summary>Added to base health. MAX_HEALTH ~ (BASE_HEALTH + baseHealthAdd) * (HEALTH_MULT + healthMultAdd).</summary>
            public float baseHealthAdd = 0f;

            /// <summary>Added to base shield. MAX_SHIELD ~ BASE_SHIELD + baseShieldAdd.</summary>
            public float baseShieldAdd = 0f;

            /// <summary>Added to the direct multiplier to base health regen. HEALTH_REGEN ~ (BASE_REGEN + baseRegenAdd) * (REGEN_MULT + regenMultAdd).</summary>
            public float regenMultAdd = 0f;

            /// <summary>Added to base health regen. HEALTH_REGEN ~ (BASE_REGEN + baseRegenAdd) * (REGEN_MULT + regenMultAdd).</summary>
            public float baseRegenAdd = 0f;

            /// <summary>Added to base move speed. MOVE_SPEED ~ (BASE_MOVE_SPEED + baseMoveSpeedAdd) * (MOVE_SPEED_MULT + moveSpeedMultAdd / MOVE_SPEED_REDUCTION_MULT + moveSpeedReductionMultAdd)</summary>
            public float baseMoveSpeedAdd = 0f;

            /// <summary>Added to the direct multiplier to move speed. MOVE_SPEED ~ (BASE_MOVE_SPEED + baseMoveSpeedAdd) * (MOVE_SPEED_MULT + moveSpeedMultAdd / MOVE_SPEED_REDUCTION_MULT + moveSpeedReductionMultAdd)</summary>
            public float moveSpeedMultAdd = 0f;

            /// <summary>Added reduction multiplier to move speed. MOVE_SPEED ~ (BASE_MOVE_SPEED + baseMoveSpeedAdd) * (MOVE_SPEED_MULT + moveSpeedMultAdd / MOVE_SPEED_REDUCTION_MULT + moveSpeedReductionMultAdd)</summary>
            public float moveSpeedReductionMultAdd = 0f;

            /// <summary>Added to the direct multiplier to jump power. JUMP_POWER ~ BASE_JUMP_POWER * (JUMP_POWER_MULT + jumpPowerMultAdd)</summary>
            public float jumpPowerMultAdd = 0f;

            /// <summary>Added to the direct multiplier to base damage. DAMAGE ~ (BASE_DAMAGE + baseDamageAdd) * (DAMAGE_MULT + damageMultAdd).</summary>
            public float damageMultAdd = 0f;

            /// <summary>Added to base damage. DAMAGE ~ (BASE_DAMAGE + baseDamageAdd) * (DAMAGE_MULT + damageMultAdd).</summary>
            public float baseDamageAdd = 0f;

            /// <summary>Added to attack speed. ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd) * (ATTACK_SPEED_MULT + attackSpeedMultAdd).</summary>
            public float baseAttackSpeedAdd = 0f;

            /// <summary>Added to the direct multiplier to attack speed. ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd) * (ATTACK_SPEED_MULT + attackSpeedMultAdd).</summary>
            public float attackSpeedMultAdd = 0f;

            /// <summary>Added to crit chance. CRIT_CHANCE ~ BASE_CRIT_CHANCE + critAdd.</summary>
            public float critAdd = 0f;

            /// <summary>Added to armor. ARMOR ~ BASE_ARMOR + armorAdd.</summary>
            public float armorAdd = 0f;
        }

        /// <summary>
        /// Used as the delegate type for the GetStatCoefficients event.
        /// </summary>
        /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
        /// <param name="args">An instance of StatHookEventArgs, passed to each subscriber to this event in turn for modification.</param>
        public delegate void StatHookEventHandler(CharacterBody sender, StatHookEventArgs args);

        /// <summary>
        /// Subscribe to this event to modify one of the stat hooks which StatHookEventArgs covers. Fired during CharacterBody.RecalculateStats.
        /// </summary>
        public static event StatHookEventHandler GetStatCoefficients;

        private static StatHookEventArgs StatMods;

        private static void HookRecalculateStats(ILContext il) {
            ILCursor c = new ILCursor(il);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterBody>>((cb) => {
                StatMods = new StatHookEventArgs();
                GetStatCoefficients?.Invoke(cb, StatMods);
            });

            ModifyHealthStat(c);
            ModifyShieldStat(c);
            ModifyHealthRegenStat(c);
            ModifyMovementSpeedStat(c);
            ModifyJumpStat(c);
            ModifyDamageStat(c);
            ModifyAttackSpeedStat(c);
            ModifyCritStat(c);
            ModifyArmorStat(c);
        }

        private static void ModifyArmorStat(ILCursor c) {
            c.Index = 0;

            bool ILFound = c.TryGotoNext(
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseArmor))
            ) && c.TryGotoNext(
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.armor)))
            ) && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.armor)))
            );

            if (ILFound) {
                c.EmitDelegate<Func<float, float>>((oldArmor) => {
                    return oldArmor + StatMods.armorAdd;
                });
            }
        }

        private static void ModifyAttackSpeedStat(ILCursor c) {
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

            if (ILFound) {
                c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseAttackSpeed)));
                c.GotoNext(x => x.MatchStloc(locBaseAttackSpeedIndex));
                c.EmitDelegate<Func<float, float>>((origSpeed) => {
                    return origSpeed + StatMods.baseAttackSpeedAdd;
                });
                c.GotoNext(x => x.MatchStloc(locAttackSpeedMultIndex));
                c.EmitDelegate<Func<float, float>>((origSpeedMult) => {
                    return origSpeedMult + StatMods.attackSpeedMultAdd;
                });
            }
        }

        private static void ModifyCritStat(ILCursor c) {
            c.Index = 0;

            int locOrigCrit = -1;
            bool ILFound = c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(out locOrigCrit),
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.crit))));

            if (ILFound) {
                c.Emit(OpCodes.Ldloc, locOrigCrit);
                c.EmitDelegate<Func<float, float>>((origCrit) => {
                    return origCrit + StatMods.critAdd;
                });
                c.Emit(OpCodes.Stloc, locOrigCrit);
            }
        }

        private static void ModifyDamageStat(ILCursor c) {
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

            if (ILFound) {
                c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseDamage)));
                c.GotoNext(x => x.MatchStloc(locBaseDamageIndex));
                c.EmitDelegate<Func<float, float>>((origDamage) => {
                    return origDamage + StatMods.baseDamageAdd;
                });
                c.GotoNext(x => x.MatchStloc(locDamageMultIndex));
                c.EmitDelegate<Func<float, float>>((origDamageMult) => {
                    return origDamageMult + StatMods.damageMultAdd;
                });
            }
        }

        private static void ModifyJumpStat(ILCursor c) {
            c.Index = 0;

            bool ILFound = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseJumpPower)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelJumpPower)),
                x => x.MatchLdloc(out _),
                x => x.MatchMul(),
                x => x.MatchAdd());

            if (ILFound) {
                c.EmitDelegate<Func<float, float>>((origJumpPower) => {
                    return origJumpPower * (1 + StatMods.jumpPowerMultAdd);
                });
            }
        }

        private static void ModifyHealthStat(ILCursor c) {
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

            if (ILFound) {
                c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseMaxHealth)));
                c.GotoNext(x => x.MatchStloc(locBaseHealthIndex));
                c.EmitDelegate<Func<float, float>>((origMaxHealth) => {
                    return origMaxHealth + StatMods.baseHealthAdd;
                });
                c.GotoNext(x => x.MatchStloc(locHealthMultIndex));
                c.EmitDelegate<Func<float, float>>((origHealthMult) => {
                    return origHealthMult + StatMods.healthMultAdd;
                });
            }
        }

        private static void ModifyShieldStat(ILCursor c) {
            c.Index = 0;

            int locBaseShieldIndex = -1;
            bool ILFound = c.TryGotoNext(
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseMaxShield)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMaxShield))
            ) && c.TryGotoNext(
                x => x.MatchStloc(out locBaseShieldIndex)
            );

            if (ILFound) {
                c.EmitDelegate<Func<float, float>>((origBaseShield) => {
                    return origBaseShield + StatMods.baseShieldAdd;
                });
            }
        }

        private static void ModifyHealthRegenStat(ILCursor c) {
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

            if (ILFound) {
                c.GotoNext(x => x.MatchLdloc(out locRegenMultIndex));
                c.EmitDelegate<Func<float>>(() => {
                    return StatMods.baseRegenAdd;
                });
                c.Emit(OpCodes.Add);
                c.GotoNext(x => x.MatchMul());
                c.EmitDelegate<Func<float, float>>((origRegenMult) => {
                    return origRegenMult + StatMods.regenMultAdd;
                });
            }
        }

        private static void ModifyMovementSpeedStat(ILCursor c) {
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
            );

            if (ILFound) {
                c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMoveSpeed)));
                c.GotoNext(x => x.MatchStloc(locBaseSpeedIndex));
                c.EmitDelegate<Func<float, float>>((origBaseMoveSpeed) => {
                    return origBaseMoveSpeed + StatMods.baseMoveSpeedAdd;
                });
                c.GotoNext(x => x.MatchStloc(locSpeedMultIndex));
                c.EmitDelegate<Func<float, float>>((origMoveSpeedMult) => {
                    return origMoveSpeedMult + StatMods.moveSpeedMultAdd;
                });
                c.GotoNext(x => x.MatchStloc(locSpeedDivIndex));
                c.EmitDelegate<Func<float, float>>((origMoveSpeedReductionMult) => {
                    return origMoveSpeedReductionMult + StatMods.moveSpeedReductionMultAdd;
                });
            }
        }
    }
}
