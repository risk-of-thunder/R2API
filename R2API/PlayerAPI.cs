using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;

namespace R2API {

    public enum Priority : short {
        Last = short.MaxValue,
        Multiplicative = 16000,
        Additive = 8000,
        High = 1000,
        VeryHigh = 400,
        Critical = 200,
        Maximum = 1,
    }
    [Flags]
    public enum FunctionTag : int {
        None = 0x0,
        Health = 0x1,
        Shield = 0x2,
        Regen = 0x4,
        MoveSpeed = 0x8,
        JumpPower = 0x10,
        JumpCount = 0x20,
        Damage = 0x40,
        AttackSpeed = 0x80,
        Crit = 0x100,
        Armor = 0x200,
        GeneralCoolDown = 0x400,
        PrimaryCoolDown = 0x800,
        SecondaryCoolDown = 0x1000,
        UtilityCoolDown = 0x2000,
        SpecialCoolDown = 0x4000,
        PrimaryCount = 0x8000,
        SecondaryCount = 0x10000,
        UtilityCount = 0x20000,
        SpecialCount = 0x40000,
        All = 0x7ffff,
    }


    public class ModRecalculateCustom {
        public short RecalculatePriority;

        public FunctionTag FlagOverWrite = FunctionTag.None;

        public virtual float RecalculateHealth(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateShield(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateRegen(float baseValue, CharacterBody character) => baseValue;

        public virtual float RecalculateMoveSpeed(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateJumpPower(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateJumpCount(float baseValue, CharacterBody character) => baseValue;

        public virtual float RecalculateDamage(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateAttackSpeed(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateCrit(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateArmor(float baseValue, CharacterBody character) => baseValue;

        public virtual float RecalculateGeneralCooldown(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculatePrimaryCoolDown(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateSecondaryCooldown(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateUtilityCooldown(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateSpecialCooldown(float baseValue, CharacterBody character) => baseValue;

        public virtual float RecalculatePrimaryCount(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateSecondaryCount(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateUtilityCount(float baseValue, CharacterBody character) => baseValue;
        public virtual float RecalculateSpecialCount(float baseValue, CharacterBody character) => baseValue;

        public virtual void UpdateItem(CharacterBody character) {

        }
    }

    class DefaultRecalculate : ModRecalculateCustom {

        public DefaultRecalculate() {
            RecalculatePriority = 0;
        }

        public override float RecalculateHealth(float baseValue, CharacterBody character) {
            float MaxHealth = character.baseMaxHealth + (character.level - 1) * character.levelMaxHealth;
            float HealthBonusItem = 0;
            float hpbooster = 1;
            float healthDivider = 1;
            if ((bool)character.inventory) {
                HealthBonusItem += CustomItemAPI.GetBonusForStat(character, StatIndex.MaxHealth);

                if (character.inventory.GetItemCount(ItemIndex.Infusion) > 0)
                    HealthBonusItem += character.inventory.infusionBonus;
                hpbooster += CustomItemAPI.GetMultiplierForStat(character, StatIndex.MaxHealth);
                healthDivider = character.CalcLunarDaggerPower();
            }
            MaxHealth += HealthBonusItem;
            MaxHealth *= hpbooster / healthDivider;
            return MaxHealth;
        }

        public override float RecalculateShield(float baseValue, CharacterBody character) {
            float MaxShield = character.baseMaxShield + character.levelMaxShield * (character.level - 1);

            if (character.inventory) {
                if (character.inventory.GetItemCount(ItemIndex.ShieldOnly) > 0) {

                    MaxShield += character.maxHealth * (1.25f + (character.inventory.GetItemCount(ItemIndex.ShieldOnly) - 1) * 0.5f);
                    character.SetPropertyValue("maxHealth", 1);
                }
            }
            //Buff
            if (character.HasBuff(BuffIndex.EngiShield))
                MaxShield += character.maxHealth * 1f;
            if (character.HasBuff(BuffIndex.EngiTeamShield))
                MaxShield += character.maxHealth * 0.5f;


            //NPC Overload Buff
            if (character.GetFieldValue<BuffMask>("buffMask").HasBuff(BuffIndex.AffixBlue)) {
                character.SetPropertyValue("maxHealth", character.maxHealth * 0.5f);
                MaxShield += character.maxHealth;
            }
            if (character.inventory) {
                MaxShield += CustomItemAPI.GetBonusForStat(character, StatIndex.MaxShield);

                MaxShield *= (1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.MaxShield));
            }
            return MaxShield;
        }

        public override float RecalculateRegen(float baseValue, CharacterBody character) {
            float BaseRegen = (character.baseRegen + character.levelRegen * (character.level - 1)) * 2.5f;

            float RegenBonus = 0;
            float regenmult = 1;
            //Item Related
            if ((bool)character.inventory) {
                RegenBonus += CustomItemAPI.GetBonusForStat(character, StatIndex.Regen);
                if (character.outOfDanger)
                    RegenBonus += CustomItemAPI.GetBonusForStat(character, StatIndex.SafeRegen);
                if (character.inventory.GetItemCount(ItemIndex.HealthDecay) > 0)
                    RegenBonus -= character.maxHealth / character.inventory.GetItemCount(ItemIndex.HealthDecay);
                regenmult += CustomItemAPI.GetMultiplierForStat(character, StatIndex.Regen);
                if (character.outOfDanger)
                    regenmult += CustomItemAPI.GetMultiplierForStat(character, StatIndex.SafeRegen);
            }

            float totalRegen = (BaseRegen * regenmult + RegenBonus);

            return totalRegen;

        }

        public override float RecalculateMoveSpeed(float baseValue, CharacterBody character) {
            float BaseMoveSpeed = character.baseMoveSpeed + character.levelMoveSpeed * (character.level - 1);

            float SpeedBonus = 1;


            //More weird stuff
            if ((bool)character.inventory)
                if (character.inventory.currentEquipmentIndex == EquipmentIndex.AffixYellow)
                    BaseMoveSpeed += 2;

            if (character.isSprinting)
                BaseMoveSpeed *= character.GetFieldValue<float>("sprintingSpeedMultiplier");


            //SpeedBonus
            if (character.HasBuff(BuffIndex.BugWings))
                SpeedBonus += 0.2f;
            if (character.HasBuff(BuffIndex.Warbanner))
                SpeedBonus += 0.3f;
            if (character.HasBuff(BuffIndex.EnrageAncientWisp))
                SpeedBonus += 0.4f;
            if (character.HasBuff(BuffIndex.CloakSpeed))
                SpeedBonus += 0.4f;
            if (character.HasBuff(BuffIndex.TempestSpeed))
                SpeedBonus += 1;
            if (character.HasBuff(BuffIndex.WarCryBuff))
                SpeedBonus += .5f;
            if (character.HasBuff(BuffIndex.EngiTeamShield))
                SpeedBonus += 0.3f;

            SpeedBonus += CustomItemAPI.GetMultiplierForStat(character, StatIndex.MoveSpeed);
            if (character.isSprinting)
                SpeedBonus += CustomItemAPI.GetMultiplierForStat(character, StatIndex.RunningMoveSpeed);
            if (character.outOfCombat && character.outOfDanger) {
                SpeedBonus += CustomItemAPI.GetMultiplierForStat(character, StatIndex.SafeMoveSpeed);
                if (character.isSprinting)
                    SpeedBonus += CustomItemAPI.GetMultiplierForStat(character, StatIndex.SafeRunningMoveSpeed);
            }

            //Debuff Speed
            float SpeedMalus = 1f;
            if (character.HasBuff(BuffIndex.Slow50))
                SpeedMalus += 0.5f;
            if (character.HasBuff(BuffIndex.Slow60))
                SpeedMalus += 0.6f;
            if (character.HasBuff(BuffIndex.Slow80))
                SpeedMalus += 0.8f;
            if (character.HasBuff(BuffIndex.ClayGoo))
                SpeedMalus += 0.5f;
            if (character.HasBuff(BuffIndex.Slow30))
                SpeedMalus += 0.3f;
            if (character.HasBuff(BuffIndex.Cripple))
                ++SpeedMalus;

            BaseMoveSpeed += CustomItemAPI.GetBonusForStat(character, StatIndex.MoveSpeed);
            if (character.isSprinting)
                BaseMoveSpeed += CustomItemAPI.GetBonusForStat(character, StatIndex.RunningMoveSpeed);
            if (character.outOfCombat && character.outOfDanger) {
                BaseMoveSpeed += CustomItemAPI.GetBonusForStat(character, StatIndex.SafeMoveSpeed);
                if (character.isSprinting)
                    BaseMoveSpeed += CustomItemAPI.GetBonusForStat(character, StatIndex.SafeRunningMoveSpeed);
            }

            float MoveSpeed = BaseMoveSpeed * (SpeedBonus / SpeedMalus);
            if ((bool)character.inventory) {
                MoveSpeed *= 1.0f - 0.05f * character.GetBuffCount(BuffIndex.BeetleJuice);
            }

            return MoveSpeed;
        }


        public override float RecalculateJumpPower(float baseValue, CharacterBody character) {
            float JumpPower = character.baseJumpPower + character.levelJumpPower * (character.level - 1) + CustomItemAPI.GetBonusForStat(character, StatIndex.JumpPower);
            JumpPower *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.JumpPower);
            return JumpPower;
        }

        public override float RecalculateJumpCount(float baseValue, CharacterBody character) {
            float JumpCount = character.baseJumpCount + CustomItemAPI.GetBonusForStat(character, StatIndex.JumpCount);
            JumpCount *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.JumpCount);
            return JumpCount;
        }

        public override float RecalculateDamage(float baseValue, CharacterBody character) {
            float BaseDamage = character.baseDamage + character.levelDamage * (character.level - 1);
            BaseDamage += CustomItemAPI.GetBonusForStat(character, StatIndex.Damage);

            float DamageBoost = 0;
            int DamageBoostCount = character.inventory ? character.inventory.GetItemCount(ItemIndex.BoostDamage) : 0;
            if (DamageBoostCount > 0)
                DamageBoost += DamageBoostCount * DamageBoost;
            DamageBoost -= 0.05f * character.GetBuffCount(BuffIndex.BeetleJuice);

            if (character.HasBuff(BuffIndex.GoldEmpowered))
                DamageBoost += 1;

            float DamageMult = DamageBoost + (character.CalcLunarDaggerPower());
            DamageMult += CustomItemAPI.GetMultiplierForStat(character, StatIndex.Damage);
            return BaseDamage * DamageMult;
        }

        public override float RecalculateAttackSpeed(float baseValue, CharacterBody character) {
            float BaseAttackSpeed = character.baseAttackSpeed + character.levelAttackSpeed * (character.level - 1);

            //Item efect
            float AttackSpeedBonus = 1f;
            if (character.inventory) {
                if (character.inventory.currentEquipmentIndex == EquipmentIndex.AffixYellow)
                    AttackSpeedBonus += 0.5f;
            }

            //Buffs
            float AttackSpeedMult = AttackSpeedBonus + character.GetFieldValue<int[]>("buffs")[2] * 0.12f;
            if (character.HasBuff(BuffIndex.Warbanner))
                AttackSpeedMult += 0.3f;
            if (character.HasBuff(BuffIndex.EnrageAncientWisp))
                AttackSpeedMult += 2f;
            if (character.HasBuff(BuffIndex.WarCryBuff))
                AttackSpeedMult += 1f;


            BaseAttackSpeed += CustomItemAPI.GetBonusForStat(character, StatIndex.AttackSpeed);
            AttackSpeedMult += CustomItemAPI.GetMultiplierForStat(character, StatIndex.AttackSpeed);
            float AttackSpeed = BaseAttackSpeed * AttackSpeedMult;
            //Debuff
            AttackSpeed *= 1 - (0.05f * character.GetBuffCount(BuffIndex.BeetleJuice));

            return AttackSpeed;
        }

        public override float RecalculateCrit(float baseValue, CharacterBody character) {
            float CriticalChance = character.baseCrit + character.levelCrit * (character.level - 1);


            CriticalChance += CustomItemAPI.GetBonusForStat(character, StatIndex.Crit);
            CriticalChance *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.AttackSpeed);

            if (character.HasBuff(BuffIndex.FullCrit))
                CriticalChance += 100;


            return CriticalChance;
        }

        public override float RecalculateArmor(float baseValue, CharacterBody character) {
            float BaseArmor = character.baseArmor + character.levelArmor * (character.level - 1);
            float BonusArmor = 0;

            if (character.HasBuff(BuffIndex.ArmorBoost))
                BonusArmor += 200;
            if (character.HasBuff(BuffIndex.Cripple))
                BonusArmor -= 20;
            float TotalArmor = BaseArmor + BonusArmor;
            TotalArmor += CustomItemAPI.GetBonusForStat(character, StatIndex.Armor);
            if (character.isSprinting)
                TotalArmor += CustomItemAPI.GetBonusForStat(character, StatIndex.RunningArmor);
            TotalArmor *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.Armor);
            if (character.isSprinting)
                TotalArmor *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.RunningArmor);
            return TotalArmor;
        }

        public override float RecalculateGeneralCooldown(float baseValue, CharacterBody character) {
            float CoolDownMultiplier = 1f;
            CoolDownMultiplier += CustomItemAPI.GetBonusForStat(character, StatIndex.GlobalCoolDown);

            CoolDownMultiplier *= CustomItemAPI.GetMultiplierForStatCD(character, StatIndex.GlobalCoolDown);

            if (character.HasBuff(BuffIndex.GoldEmpowered))
                CoolDownMultiplier *= 0.25f;
            if (character.HasBuff(BuffIndex.NoCooldowns))
                CoolDownMultiplier = 0.0f;


            return CoolDownMultiplier;
        }

        public override float RecalculatePrimaryCoolDown(float baseValue, CharacterBody character) {
            float CoolDownMultiplier = 1f;
            CoolDownMultiplier += CustomItemAPI.GetBonusForStat(character, StatIndex.CoolDownPrimary);

            CoolDownMultiplier *= CustomItemAPI.GetMultiplierForStatCD(character, StatIndex.CoolDownPrimary);
            return CoolDownMultiplier;
        }
        public override float RecalculatePrimaryCount(float baseValue, CharacterBody character) {
            float count = 0;
            count += CustomItemAPI.GetBonusForStat(character, StatIndex.CountPrimary);

            count *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.CountPrimary);
            return count;
        }
        public override float RecalculateSecondaryCooldown(float baseValue, CharacterBody character) {
            float CoolDownMultiplier = 1f;
            CoolDownMultiplier += CustomItemAPI.GetBonusForStat(character, StatIndex.CoolDownSecondary);

            CoolDownMultiplier *= CustomItemAPI.GetMultiplierForStatCD(character, StatIndex.CoolDownSecondary);
            return CoolDownMultiplier;
        }
        public override float RecalculateSecondaryCount(float baseValue, CharacterBody character) {
            float count = 0;
            count += CustomItemAPI.GetBonusForStat(character, StatIndex.CountSecondary);
            count *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.CountSecondary);
            return count;
        }
        public override float RecalculateSpecialCooldown(float baseValue, CharacterBody character) {
            float CoolDownMultiplier = 1f;
            CoolDownMultiplier += CustomItemAPI.GetBonusForStat(character, StatIndex.CoolDownUtility);

            CoolDownMultiplier *= CustomItemAPI.GetMultiplierForStatCD(character, StatIndex.CoolDownUtility);
            return CoolDownMultiplier;
        }
        public override float RecalculateSpecialCount(float baseValue, CharacterBody character) {
            float count = 0;
            count += CustomItemAPI.GetBonusForStat(character, StatIndex.CountUtility);
            count *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.CountUtility);
            return count;
        }
        public override float RecalculateUtilityCooldown(float baseValue, CharacterBody character) {
            float CoolDownMultiplier = 1f;
            CoolDownMultiplier += CustomItemAPI.GetBonusForStat(character, StatIndex.CoolDownSpecial);

            CoolDownMultiplier *= CustomItemAPI.GetMultiplierForStatCD(character, StatIndex.CoolDownSpecial);
            return CoolDownMultiplier;
        }
        public override float RecalculateUtilityCount(float baseValue, CharacterBody character) {
            float count = 0;
            count += CustomItemAPI.GetBonusForStat(character, StatIndex.CountSpecial);

            count *= 1 + CustomItemAPI.GetMultiplierForStat(character, StatIndex.CountSpecial);
            return count;
        }

    }


    public static class PlayerAPI {
        public static List<Action<PlayerStats>> CustomEffects { get; private set; }

        static List<ModRecalculateCustom> m_RecalulateList;
        static MethodInfo GetMethodInfo(Func<float, CharacterBody, float> f) {
            return f.Method;
        }

        static float StatHandler(MethodInfo method, CharacterBody character) {
            float value = 0;
            foreach (ModRecalculateCustom recal in m_RecalulateList) {
                value = (float)method.Invoke(recal, new object[2] { value, character });
            }
            return value;
        }

        static void AddOrder(this Dictionary<int, ModRecalculateCustom> dic, int pos, ModRecalculateCustom obj, bool warn = false) {
            try { 
                if (dic.ContainsKey(pos)) {
                        AddOrder(dic, pos + 1, obj, true);
                }
                else { 
                    dic.Add(pos, obj);
                    if (warn)
                        Debug.Log("Character Stat API warning : The loading priority for " + obj.ToString() + " priority : " + obj.RecalculatePriority + " is allready used by : "+ dic[obj.RecalculatePriority].ToString() +", priotity : " + pos + " given");
                }
            }
            catch (OverflowException)
            {
                throw new Exception("Error, the Minimum priority is allready used by : "+ dic[short.MaxValue].ToString() +", only one recalculate can be at the Minimum priority");
            }
        }

        static public void ReorderRecalculateList() {
            Dictionary<int, ModRecalculateCustom> m__temp_RecalulateDic = new Dictionary<int, ModRecalculateCustom>();
            foreach (ModRecalculateCustom obj in m_RecalulateList) {
                m__temp_RecalulateDic.AddOrder(obj.RecalculatePriority, obj);
            }
            m_RecalulateList = new List<ModRecalculateCustom>();
            foreach (KeyValuePair<int, ModRecalculateCustom> kv in m__temp_RecalulateDic) {
                m_RecalulateList.Add(kv.Value);
            }
        }

        static public void AddCustomRecalculate(ModRecalculateCustom customRecalculate) {
            m_RecalulateList.Add(customRecalculate);
            ReorderRecalculateList();
        }

        public static void InitHooks() {
            m_RecalulateList = new List<ModRecalculateCustom>();
            m_RecalulateList.Add(new DefaultRecalculate());
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) => RecalcStats(self);
        }

        public static void RecalcStats(CharacterBody characterBody) {
            if (characterBody == null)
                return;

            CustomItemAPI.Update();
            foreach (ModRecalculateCustom recal in m_RecalulateList) {
                recal.InvokeMethod("UpdateItem", new object[1] { characterBody });
            }


            characterBody.SetPropertyValue("experience",
                TeamManager.instance.GetTeamExperience(characterBody.teamComponent.teamIndex));
            float Level = TeamManager.instance.GetTeamLevel(characterBody.teamComponent.teamIndex);
            if (characterBody.inventory) {
                Level += characterBody.inventory.GetItemCount(ItemIndex.LevelBonus);

            }
            characterBody.SetPropertyValue("level", Level);

            characterBody.SetPropertyValue("isElite", characterBody.GetFieldValue<BuffMask>("buffMask").containsEliteBuff);

            float preHealth = characterBody.maxHealth;
            float preShield = characterBody.maxShield;

            characterBody.SetPropertyValue("maxHealth", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateHealth), characterBody));

            characterBody.SetPropertyValue("maxShield", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateShield), characterBody));

            characterBody.SetPropertyValue("regen", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateRegen), characterBody));

            characterBody.SetPropertyValue("moveSpeed", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateMoveSpeed), characterBody));
            characterBody.SetPropertyValue("acceleration", characterBody.moveSpeed / characterBody.baseMoveSpeed * characterBody.baseAcceleration);

            characterBody.SetPropertyValue("jumpPower", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateJumpPower), characterBody));
            characterBody.SetPropertyValue("maxJumpHeight", Trajectory.CalculateApex(characterBody.jumpPower));
            characterBody.SetPropertyValue("maxJumpCount", (int)StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateJumpCount), characterBody));

            characterBody.SetPropertyValue("damage", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateDamage), characterBody));

            characterBody.SetPropertyValue("attackSpeed", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateAttackSpeed), characterBody));

            characterBody.SetPropertyValue("crit", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateCrit), characterBody));
            characterBody.SetPropertyValue("armor", StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateArmor), characterBody));

            //CoolDown 
            float CoolDownMultiplier = StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateGeneralCooldown), characterBody);
            if ((bool)characterBody.GetFieldValue<SkillLocator>("skillLocator").primary) {
                characterBody.GetFieldValue<SkillLocator>("skillLocator").primary.cooldownScale = StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculatePrimaryCoolDown), characterBody) * CoolDownMultiplier;
                if (characterBody.GetFieldValue<SkillLocator>("skillLocator").primary.baseMaxStock > 1)
                    characterBody.GetFieldValue<SkillLocator>("skillLocator").primary.SetBonusStockFromBody((int)StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculatePrimaryCount), characterBody));
            }
            if ((bool)characterBody.GetFieldValue<SkillLocator>("skillLocator").secondary) {
                characterBody.GetFieldValue<SkillLocator>("skillLocator").secondary.cooldownScale = StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateSecondaryCooldown), characterBody) * CoolDownMultiplier;
                characterBody.GetFieldValue<SkillLocator>("skillLocator").secondary.SetBonusStockFromBody((int)StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateSecondaryCount), characterBody));
            }
            if ((bool)characterBody.GetFieldValue<SkillLocator>("skillLocator").utility) {
                characterBody.GetFieldValue<SkillLocator>("skillLocator").utility.cooldownScale = StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateUtilityCooldown), characterBody) * CoolDownMultiplier;
                characterBody.GetFieldValue<SkillLocator>("skillLocator").utility.SetBonusStockFromBody((int)StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateUtilityCount), characterBody));
            }
            if ((bool)characterBody.GetFieldValue<SkillLocator>("skillLocator").special) {
                characterBody.GetFieldValue<SkillLocator>("skillLocator").special.cooldownScale = StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateSpecialCooldown), characterBody) * CoolDownMultiplier;
                if (characterBody.GetFieldValue<SkillLocator>("skillLocator").special.baseMaxStock > 1)
                    characterBody.GetFieldValue<SkillLocator>("skillLocator").special.SetBonusStockFromBody((int)StatHandler(GetMethodInfo(new ModRecalculateCustom().RecalculateSpecialCount), characterBody));
            }
            //Since it's not yet used in game, I leave that here for now
            characterBody.SetPropertyValue("critHeal", 0.0f);
            if (characterBody.inventory) {
                if (characterBody.inventory.GetItemCount(ItemIndex.CritHeal) > 0) {
                    float crit = characterBody.crit;
                    characterBody.SetPropertyValue("crit", characterBody.crit / (characterBody.inventory.GetItemCount(ItemIndex.CritHeal) + 1));
                    characterBody.SetPropertyValue("critHeal", crit - characterBody.crit);
                }
            }

            if (NetworkServer.active) {
                float HealthOffset = characterBody.maxHealth - preHealth;
                float ShieldOffset = characterBody.maxShield - preShield;
                if (HealthOffset > 0) {
                    double num47 = characterBody.healthComponent.Heal(HealthOffset, new ProcChainMask(), false);
                }
                else if (characterBody.healthComponent.health > characterBody.maxHealth)
                    characterBody.healthComponent.Networkhealth = characterBody.maxHealth;
                if (ShieldOffset > 0) {
                    characterBody.healthComponent.RechargeShieldFull();
                    //characterBody.healthComponent.RechargeShield(ShieldOffset); //Depend on the version of the Assembly-Csharp
                }
            }

            characterBody.statsDirty = false;
        }
    }

    public class PlayerStats {
        //Character Stats
        public int maxHealth = 0;
        public int healthRegen = 0;
        public bool isElite = false;
        public int maxShield = 0;
        public float movementSpeed = 0;
        public float acceleration = 0;
        public float jumpPower = 0;
        public float maxJumpHeight = 0;
        public float maxJumpCount = 0;
        public float attackSpeed = 0;
        public float damage = 0;
        public float Crit = 0;
        public float Armor = 0;
        public float critHeal = 0;

        //Primary Skill
        public float PrimaryCooldownScale = 0;
        public float PrimaryStock = 0;

        //Secondary Skill
        public float SecondaryCooldownScale = 0;
        public float SecondaryStock = 0;

        //Utility Skill
        public float UtilityCooldownScale = 0;
        public float UtilityStock = 0;

        //Special Skill
        public float SpecialCooldownScale = 0;
        public float SpecialStock = 0;
    }
}
