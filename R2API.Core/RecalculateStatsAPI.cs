using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;

namespace R2API {
    /// <summary>
    /// API for computing bonuses granted by factors inside RecalculateStats.
    /// </summary>
    public static class RecalculateStatsAPI {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        [R2APIInitialize(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.CharacterBody.RecalculateStats += HookRecalculateStats;
        }

        [R2APIInitialize(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.CharacterBody.RecalculateStats -= HookRecalculateStats;
        }

        /// <summary>
        /// A collection of modifiers for various stats. It will be passed down the event chain of GetStatCoefficients; add to the contained values to modify stats.
        /// </summary>
        public class StatHookEventArgs : EventArgs {
            /// <summary>Added to the direct multiplier to base health. MAX_HEALTH ~ (BASE_HEALTH + baseHealthAdd) * (HEALTH_MULT + healthMultAdd) / (BASE_CURSE_PENALTY + baseCurseAdd).</summary>
            public float healthMultAdd = 0f;

            /// <summary>Added to base health. MAX_HEALTH ~ (BASE_HEALTH + baseHealthAdd) * (HEALTH_MULT + healthMultAdd).</summary>
            public float baseHealthAdd = 0f;

            /// <summary>Added to base shield. MAX_SHIELD ~ (BASE_SHIELD + baseShieldAdd) * (SHIELD_MULT + shieldMultAdd).</summary>
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

            /// <summary>Added to the direct multiplier to jump power. JUMP_POWER ~ (BASE_JUMP_POWER + baseJumpPowerAdd) * (JUMP_POWER_MULT + jumpPowerMultAdd)</summary>
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

            /// <summary> Added to Curse Penalty.MAX_HEALTH ~ (BASE_HEALTH + baseHealthAdd) * (HEALTH_MULT + healthMultAdd) / (BASE_CURSE_PENALTY + baseCurseAdd)</summary>
            public float baseCurseAdd = 0f;

            /// <summary>Added to flat cooldown reduction. COOLDOWN ~ BASE_COOLDOWN * (BASE_COOLDOWN_MULT + cooldownMultAdd) - (BASE_FLAT_REDUCTION + cooldownReductionAdd) </summary>
            public float cooldownReductionAdd = 0f;

            /// <summary>Added to the direct multiplier to cooldown timers. COOLDOWN ~ BASE_COOLDOWN * (BASE_COOLDOWN_MULT + cooldownMultAdd) - (BASE_FLAT_REDUCTION + cooldownReductionAdd)</summary>
            public float cooldownMultAdd = 0f;

            /// <summary> (Primary) Added to the direct multiplier to cooldown timers. COOLDOWN ~ BASE_COOLDOWN * (BASE_COOLDOWN_MULT + cooldownMultAdd + primaryCooldownMultAdd) - (BASE_FLAT_REDUCTION + cooldownReductionAdd)</summary>
            public float primaryCooldownMultAdd = 0f;

            /// <summary> (Secondary) Added to the direct multiplier to cooldown timers. COOLDOWN ~ BASE_COOLDOWN * (BASE_COOLDOWN_MULT + cooldownMultAdd+ secondaryCooldownMultAdd) - (BASE_FLAT_REDUCTION + cooldownReductionAdd)</summary>
            public float secondaryCooldownMultAdd = 0f;

            /// <summary> (Utility) Added to the direct multiplier to cooldown timers. COOLDOWN ~ BASE_COOLDOWN * (BASE_COOLDOWN_MULT + cooldownMultAdd + utilityCooldownMultAdd) - (BASE_FLAT_REDUCTION + cooldownReductionAdd)</summary>
            public float utilityCooldownMultAdd = 0f;

            /// <summary> (Special) Added to the direct multiplier to cooldown timers. COOLDOWN ~ BASE_COOLDOWN * (BASE_COOLDOWN_MULT + cooldownMultAdd + specialCooldownMultAdd) - (BASE_FLAT_REDUCTION + cooldownReductionAdd)</summary>
            public float specialCooldownMultAdd = 0f;

            /// <summary>Added to the direct multiplier to shields MAX_SHIELD ~ (BASE_SHIELD + baseShieldAdd) * (SHIELD_MULT + shieldMultAdd).</summary>
            public float shieldMultAdd = 0f;

            /// <summary>Added to base jump power. JUMP_POWER ~ (BASE_JUMP_POWER + baseJumpPowerAdd)* (JUMP_POWER_MULT + jumpPowerMultAdd)</summary>
            public float baseJumpPowerAdd = 0f;

            /// <summary>Added to the direct multiplier to level scaling. EFFECTIVE LEVEL ~ (BASE LEVEL * (BASE_LEVEL_SCALING + levelMultAdd)</summary>
            public float levelMultAdd = 0f;

            /// <summary>Amount of Root effects currently applied. MOVE_SPEED ~ (moveSpeedRootCount > 0) ? 0 : MOVE_SPEED </summary>
            public int moveSpeedRootCount = 0;

            /// <summary>Added to the direct multiplier to crit damage. CRIT_DAMAGE ~ DAMAGE * (BASE_CRIT_MULT + critDamageMultAdd) </summary>
            public float critDamageMultAdd = 0;
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
        public static event StatHookEventHandler GetStatCoefficients {
            add {
                if (!Loaded) {
                    throw new InvalidOperationException(
                        $"{nameof(RecalculateStatsAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(RecalculateStatsAPI)})]");
                }

                _getStatCoefficients += value;
            }

            remove {
                if (!Loaded) {
                    throw new InvalidOperationException(
                        $"{nameof(RecalculateStatsAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(RecalculateStatsAPI)})]");
                }

                _getStatCoefficients -= value;
            }
        }

        private static StatHookEventArgs StatMods;

        private static void HookRecalculateStats(ILContext il) {
            ILCursor c = new ILCursor(il);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterBody>>(GetStatMods);

            ModifyHealthStat(c);
            ModifyShieldStat(c);
            ModifyHealthRegenStat(c);
            ModifyMovementSpeedStat(c);
            ModifyJumpStat(c);
            ModifyDamageStat(c);
            ModifyAttackSpeedStat(c);
            ModifyCritStat(c);
            ModifyArmorStat(c);
            ModifyCurseStat(c);
            ModifyCooldownStat(c);
            ModifyLevelingStat(c);
        }

        private static void GetStatMods(CharacterBody characterBody) {
            StatMods = new StatHookEventArgs();

            if (_getStatCoefficients != null) {
                foreach (StatHookEventHandler @event in _getStatCoefficients.GetInvocationList()) {
                    try {
                        @event(characterBody, StatMods);
                    }
                    catch (Exception e) {
                        R2API.Logger.LogError(
                            $"Exception thrown by : {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                    }
                }
            }
        }


        private static void ModifyCurseStat(ILCursor c) {
            c.Index = 0;

            bool ILFound = c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcR4(1),
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.cursePenalty))
                ));

            if (ILFound) {
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<CharacterBody>>((body) => { body.cursePenalty += StatMods.baseCurseAdd; });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyCurseStat)} failed.");
            }
        }


        private static void ModifyCooldownStat(ILCursor c) {
            c.Index = 0;
            int ILFound = 0;
            while (c.TryGotoNext(
                       x => x.MatchCallOrCallvirt(
                           typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale)))
                   ) && c.TryGotoNext(
                       x => x.MatchCallOrCallvirt(
                           typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction)))
                   )) {
                ILFound++;
            }

            if (ILFound >= 4) {
                c.Index = 0;
                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.primaryCooldownMultAdd);
                });
                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(
                        typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown + StatMods.cooldownReductionAdd;
                });

                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.secondaryCooldownMultAdd);
                });
                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(
                        typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown + StatMods.cooldownReductionAdd;
                });

                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.utilityCooldownMultAdd);
                });
                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(
                        typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown + StatMods.cooldownReductionAdd;
                });


                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.cooldownScale))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown * (1 + StatMods.cooldownMultAdd + StatMods.specialCooldownMultAdd);
                });
                c.GotoNext(x =>
                    x.MatchCallOrCallvirt(
                        typeof(GenericSkill).GetPropertySetter(nameof(GenericSkill.flatCooldownReduction))));
                c.EmitDelegate<Func<float, float>>((oldCooldown) => {
                    return oldCooldown + StatMods.cooldownReductionAdd;
                });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyCooldownStat)} failed.");
            }
        }


        private static void ModifyLevelingStat(ILCursor c) {
            c.Index = 0;
            bool ILFound = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertyGetter(nameof(CharacterBody.level))),
                x => x.MatchLdcR4(1),
                x => x.MatchSub()
            );

            if (ILFound) {
                c.EmitDelegate<Func<float, float>>((oldScaling) => {
                    return oldScaling * (1 + StatMods.levelMultAdd);
                });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyLevelingStat)} failed.");
            }
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
                c.EmitDelegate<Func<float, float>>((oldArmor) => { return oldArmor + StatMods.armorAdd; });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyArmorStat)} failed.");
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
                c.EmitDelegate<Func<float, float>>((origSpeed) => { return origSpeed + StatMods.baseAttackSpeedAdd; });
                c.GotoNext(x => x.MatchStloc(locAttackSpeedMultIndex));
                c.EmitDelegate<Func<float, float>>((origSpeedMult) => {
                    return origSpeedMult + StatMods.attackSpeedMultAdd;
                });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyAttackSpeedStat)} failed.");
            }
        }

        private static void ModifyCritStat(ILCursor c) {
            c.Index = 0;

            int locOrigCrit = -1;
            bool ILFound = c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(out locOrigCrit),
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.crit)))
            ) && c.TryGotoPrev( MoveType.After,
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.critMultiplier)))
                );

            if (ILFound) {
                c.Index--;
                c.EmitDelegate<Func<float,float>>((origCritMult) => {return origCritMult + StatMods.critDamageMultAdd;});
                c.GotoNext(MoveType.After,x => x.MatchStloc(locOrigCrit));
                c.Emit(OpCodes.Ldloc, locOrigCrit);
                c.EmitDelegate<Func<float, float>>((origCrit) => { return origCrit + StatMods.critAdd; });
                c.Emit(OpCodes.Stloc, locOrigCrit);
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyCritStat)} failed.");
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
                c.EmitDelegate<Func<float, float>>((origDamage) => { return origDamage + StatMods.baseDamageAdd; });
                c.GotoNext(x => x.MatchStloc(locDamageMultIndex));
                c.EmitDelegate<Func<float, float>>((origDamageMult) => {
                    return origDamageMult + StatMods.damageMultAdd;
                });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyDamageStat)} failed.");
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
                    return (origJumpPower + StatMods.baseJumpPowerAdd) * (1 + StatMods.jumpPowerMultAdd);
                });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyJumpStat)} failed.");
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
            else {
                R2API.Logger.LogError($"{nameof(ModifyHealthStat)} failed.");
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
            ) && c.TryGotoNext(
                x => x.MatchLdloc(locBaseShieldIndex),
                x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetPropertySetter(nameof(CharacterBody.maxShield)))
            );

            if (ILFound) {
                c.Index++;
                c.EmitDelegate<Func<float, float>>((origMaxShield) => {
                    return origMaxShield * (1 + StatMods.shieldMultAdd);
                });
                c.GotoPrev(x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMaxShield)));
                c.GotoNext(x => x.MatchStloc(out locBaseShieldIndex));
                c.EmitDelegate<Func<float, float>>((origBaseShield) => {
                    return origBaseShield + StatMods.baseShieldAdd;
                });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyShieldStat)} failed.");
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
                c.EmitDelegate<Func<float>>(() => { return StatMods.baseRegenAdd; });
                c.Emit(OpCodes.Add);
                c.GotoNext(x => x.MatchMul());
                c.EmitDelegate<Func<float, float>>((origRegenMult) => {
                    return origRegenMult + StatMods.regenMultAdd;
                });
            }
            else {
                R2API.Logger.LogError($"{nameof(ModifyHealthRegenStat)} failed.");
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
            ) && c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out _),
                x => x.MatchOr(),
                x => x.MatchLdloc(out _),
                x => x.MatchOr()
            );

            if (ILFound) {
                c.EmitDelegate<Func<bool>>(() => { return (StatMods.moveSpeedRootCount > 0); });
                c.Emit(OpCodes.Or);
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
            else {
                R2API.Logger.LogError($"{nameof(ModifyMovementSpeedStat)} failed.");
            }
        }
    }
}
