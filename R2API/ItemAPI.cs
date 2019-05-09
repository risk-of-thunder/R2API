using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2;
using R2API.Utils;
using UnityEngine;

namespace R2API {

    public enum HitEffectType {
        OnHitEnemy = 0,
        OnHitAll = 1
    }
    public enum StatIndex {
        MaxHealth,
        MaxShield,

        Regen,
        SafeRegen,

        MoveSpeed,
        RunningMoveSpeed,
        SafeMoveSpeed,
        SafeRunningMoveSpeed,

        JumpPower,
        JumpCount,

        Damage,
        AttackSpeed,

        Crit,
        Armor,
        RunningArmor,

        GlobalCoolDown,
        CoolDownPrimary,
        CoolDownSecondary,
        CoolDownUtility,
        CoolDownSpecial,
        CountPrimary,
        CountSecondary,
        CountUtility,
        CountSpecial,
    }

    public class CustomItemStat {
        public StatIndex Stat;
        private float m_BaseBonus;
        private float m_StackBonus;
        private float m_BaseMultBonus;
        private float m_StackMultBonus;

        #region Properties
        public float FlatBonus { get { return m_BaseBonus; } }
        public float StackBonus { get { return m_StackBonus; } }
        public float MultBonus { get { return m_BaseMultBonus; } }
        public float MultStackBonus { get { return m_StackMultBonus; } }

        public float GetFlatBonusFromCount(int count) {
            if (count > 0)
                return (count - 1) * m_StackBonus + m_BaseBonus;
            return 0;
        }
        public float GetPercentBonusFromCount(int count) {
            if (count > 0)
                return (count - 1) * m_StackMultBonus + m_BaseMultBonus;
            return 0;
        }
        #endregion



        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FlatBonus">Flat bonus when player Have the item</param>
        /// <param name="FlatStackBonus">Flat bonus for each additional item the player own</param>
        /// <param name="MultBonus">Multiplicative bonus when player Have the item</param>
        /// <param name="MultStackBonus">Multiplicative bonus for each additional item the player own, for Cooldowns values of 0 are ignored</param>
        /// <param name="Stat"></param>
        public CustomItemStat(float FlatBonus, float FlatStackBonus, float MultBonus, float MultStackBonus, StatIndex Stat) {
            m_BaseBonus = FlatBonus;
            m_StackBonus = FlatStackBonus;
            m_BaseMultBonus = MultBonus;
            m_StackMultBonus = MultStackBonus;
            this.Stat = Stat;
        }
        /// <summary>
        /// Set Flat and Stack Bonusat the same time, if you want to set the separatly use ModItemStats(float FlatBonus, float StackBonus, StatIndex Stat)
        /// </summary>
        /// <param name="FlatBonus">Flat bonus for each item the player own</param>
        /// <param name="Stat"></param>
        public CustomItemStat(float FlatBonus, StatIndex Stat) {
            m_BaseBonus = FlatBonus;
            m_StackBonus = FlatBonus;
            m_BaseMultBonus = 0;
            m_StackMultBonus = 0;
            this.Stat = Stat;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FlatBonus">Flat bonus when player Have the item</param>
        /// <param name="FlatStackBonus">Flat bonus for each additional item the player own</param>
        /// <param name="MultBonus">Multiplicative bonus for each item the player own</param>
        public CustomItemStat(float FlatBonus, float StackBonus, StatIndex Stat) {
            m_BaseBonus = FlatBonus;
            m_StackBonus = StackBonus;
            m_BaseMultBonus = 0;
            m_StackMultBonus = 0;
            this.Stat = Stat;
        }
        public CustomItemStat(float FlatBonus, float StackBonus, float MultBonus, StatIndex Stat) {
            m_BaseBonus = FlatBonus;
            m_StackBonus = StackBonus;
            m_BaseMultBonus = MultBonus;
            m_StackMultBonus = MultBonus;
            this.Stat = Stat;
        }
        #endregion

        #region Operator
        public static CustomItemStat operator +(CustomItemStat a, CustomItemStat b) {
            a.m_BaseBonus += b.m_BaseBonus;
            a.m_StackBonus += b.m_StackBonus;
            a.m_BaseMultBonus += b.m_BaseMultBonus;
            a.m_StackMultBonus += b.m_StackMultBonus;
            return a;
        }
        public static CustomItemStat operator -(CustomItemStat a, CustomItemStat b) {
            a.m_BaseBonus -= b.m_BaseBonus;
            a.m_StackBonus -= b.m_StackBonus;
            a.m_BaseMultBonus -= b.m_BaseMultBonus;
            a.m_StackMultBonus -= b.m_StackMultBonus;
            return a;
        }
        public static CustomItemStat operator *(CustomItemStat a, float b) {
            a.m_BaseBonus *= b;
            a.m_StackBonus *= b;
            a.m_BaseMultBonus *= b;
            a.m_StackMultBonus *= b;
            return a;
        }
        public static CustomItemStat operator /(CustomItemStat a, float b) {
            a.m_BaseBonus /= b;
            a.m_StackBonus /= b;
            a.m_BaseMultBonus /= b;
            a.m_StackMultBonus /= b;
            return a;
        }
        #endregion

    }

    public class ModHitEffect {

        public HitEffectType EffectType = HitEffectType.OnHitEnemy;
        /// <summary>
        /// Check if the effect is Proc or not
        /// </summary>
        /// <param name="globalEventManager"></param>
        /// <param name="damageInfo"></param>
        /// <param name="victim"></param>
        /// <param name="itemCount"></param>
        /// <returns></returns>
        virtual public bool Condition(GlobalEventManager globalEventManager, DamageInfo damageInfo, GameObject victim, int itemCount) {
            return true;
        }
        virtual public void Effect(GlobalEventManager globalEventManager, DamageInfo damageInfo, GameObject victim, int itemCount) {

        }
    }


    public class CustomItem {
        public ItemDef Item { get; private set; }

        private int m_Index;
        private List<ModHitEffect> m_EffectList;

        private List<CustomItemStat> m_StatList;


        #region properties
        public List<CustomItemStat> GetStatsList { get { return m_StatList; } }
        public List<ModHitEffect> GetHitEffectList { get { return m_EffectList; } }
        public int Index { get { return m_Index; } private set { m_Index = value; } }

        /// <summary>
        /// Flat bonus of the First Item
        /// </summary>
        /// <param name="Stat"></param>
        /// <returns></returns>
        public float FlatBonus(StatIndex Stat) {
            CustomItemStat s = m_StatList.Find(x => x.Stat == Stat);
            if (s != null)
                return s.FlatBonus;
            return 0;
        }
        /// <summary>
        /// Flat bonus Per item after the first one
        /// </summary>
        /// <param name="Stat"></param>
        /// <returns></returns>
        public float StackBonus(StatIndex Stat) {
            CustomItemStat s = m_StatList.Find(x => x.Stat == Stat);
            if (s != null)
                return s.StackBonus;
            return 0;
        }
        /// <summary>
        /// Multiplicative bonus of the First item
        /// </summary>
        /// <param name="Stat"></param>
        /// <returns></returns>
        public float MultBonus(StatIndex Stat) {
            CustomItemStat s = m_StatList.Find(x => x.Stat == Stat);
            if (s != null)
                return s.MultBonus;
            return 0;
        }
        /// <summary>
        /// Multiplicative bonus Per item after the first one
        /// </summary>
        /// <param name="Stat"></param>
        /// <returns></returns>
        public float MultStackBonus(StatIndex Stat) {
            CustomItemStat s = m_StatList.Find(x => x.Stat == Stat);
            if (s != null)
                return s.MultStackBonus;
            return 0;
        }
        /// <summary>
        /// Get FlatBonus From Count
        /// </summary>
        /// <param name="Stat"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public float GetFlatBonusFromCount(StatIndex Stat, int Count) {
            CustomItemStat s = m_StatList.Find(x => x.Stat == Stat);
            if (s != null)
                return s.GetFlatBonusFromCount(Count);
            return 0;
        }
        /// <summary>
        /// Get Multiplicative Bonus from Count
        /// </summary>
        /// <param name="Stat"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public float GetMultStackBonusFromCount(StatIndex Stat, int Count) {
            CustomItemStat s = m_StatList.Find(x => x.Stat == Stat);
            if (s != null)
                return s.GetPercentBonusFromCount(Count);
            return 0;
        }
        #endregion


        public CustomItem(int Index) {
            this.m_Index = Index;
            m_StatList = new List<CustomItemStat>();
            m_EffectList = new List<ModHitEffect>();
        }

        public CustomItem(int Index, List<CustomItemStat> Stats) {
            this.m_Index = Index;
            m_StatList = Stats;
            m_EffectList = new List<ModHitEffect>();
        }

        public CustomItem(int Index, CustomItemStat Stat) {
            this.m_Index = Index;
            m_StatList = new List<CustomItemStat>
            {
                Stat
            };
            m_EffectList = new List<ModHitEffect>();
        }
        public CustomItem(int Index, CustomItemStat Stat1, CustomItemStat Stat2) {
            this.m_Index = Index;
            m_StatList = new List<CustomItemStat>
            {
                Stat1,
                Stat2
            };
            m_EffectList = new List<ModHitEffect>();
        }
        public CustomItem(int Index, CustomItemStat Stat1, CustomItemStat Stat2, CustomItemStat Stat3) {
            this.m_Index = Index;
            m_StatList = new List<CustomItemStat>
            {
                Stat1,
                Stat2,
                Stat3
            };
            m_EffectList = new List<ModHitEffect>();
        }
        public CustomItem(int Index, CustomItemStat Stat1, CustomItemStat Stat2, CustomItemStat Stat3, CustomItemStat Stat4) {
            this.m_Index = Index;
            m_StatList = new List<CustomItemStat>
            {
                Stat1,
                Stat2,
                Stat3,
                Stat4
            };
            m_EffectList = new List<ModHitEffect>();
        }

        #region Operator

        public static CustomItem operator +(CustomItem Item, ModHitEffect Effect) {
            if (!Item.m_EffectList.Exists(x => x.GetType() == Effect.GetType())) {
                Item.m_EffectList.Add(Effect);
            }
            return Item;
        }
        public static CustomItem operator +(CustomItem Item, List<ModHitEffect> Effects) {
            foreach (ModHitEffect Effect in Effects)
                if (!Item.m_EffectList.Exists(x => x.GetType() == Effect.GetType())) {
                    Item.m_EffectList.Add(Effect);
                }
            return Item;
        }


        public static CustomItem operator +(CustomItem Item, CustomItemStat Stat) {
            if (Item.m_StatList.Exists(x => x.Stat == Stat.Stat)) {
                Item.m_StatList[Item.m_StatList.FindIndex(x => x.Stat == Stat.Stat)] += Stat;
            }
            else {
                Item.m_StatList.Add(Stat);
            }
            return Item;
        }
        public static CustomItem operator +(CustomItem Item, List<CustomItemStat> Stats) {
            foreach (CustomItemStat Stat in Stats)
                if (Item.m_StatList.Exists(x => x.Stat == Stat.Stat)) {
                    Item.m_StatList[Item.m_StatList.FindIndex(x => x.Stat == Stat.Stat)] += Stat;
                }
                else {
                    Item.m_StatList.Add(Stat);
                }
            return Item;
        }
        public static CustomItem operator -(CustomItem Item, CustomItemStat Stat) {
            if (Item.m_StatList.Exists(x => x.Stat == Stat.Stat)) {
                Item.m_StatList[Item.m_StatList.FindIndex(x => x.Stat == Stat.Stat)] -= Stat;
            }
            return Item;
        }
        #endregion
    }

    static public class CustomItemAPI {

        internal static void InitHooks() {

            Init();
        }

        static public Dictionary<int, CustomItem> ModItemDictionary;
        static private Dictionary<int, CustomItem> m_DefaultModItemDictionary;
        static public Dictionary<int, CustomItem> DefaultModItemDictionary { get { return m_DefaultModItemDictionary; } }

        static private void DefaultOnHitEffect(int index, ModHitEffect HitEffect) {
            if (m_DefaultModItemDictionary.ContainsKey(index)) {
                m_DefaultModItemDictionary[index] += HitEffect;
            }
            else {
                throw new Exception("ModItemManager ERROR : int does not exist in m_DefaultModItemDictionary\nBtw you shouldn't mess with this boi !  Index :  " + index);
            }
        }

        static private void DefaultStatItem(int index, CustomItemStat stat) {
            if (m_DefaultModItemDictionary.ContainsKey(index)) {
                m_DefaultModItemDictionary[index] += stat;
            }
            else {
                throw new Exception("ModItemManager ERROR : int does not exist in m_DefaultModItemDictionary\nBtw you shouldn't mess with this boi !  Index :  " + index);
            }
        }

        public static void AddOnHitEffect(int index, ModHitEffect HitEffect) {
            if (ModItemDictionary.ContainsKey(index)) {
                ModItemDictionary[index] += HitEffect;
            }
            else {
                throw new Exception("ModItemManager ERROR : int does not exist in ModItemDictionary");
            }
        }
        public static void AddOnHitEffect(int index, List<ModHitEffect> HitEffects) {
            if (ModItemDictionary.ContainsKey(index)) {
                ModItemDictionary[index] += HitEffects;
            }
            else {
                throw new Exception("ModItemManager ERROR : int does not exist in ModItemDictionary");
            }
        }

        static public void AddModItem(int index, CustomItem ModItem) {
            if (!ModItemDictionary.ContainsKey(index)) {
                ModItemDictionary.Add(index, ModItem);
            }
            else {
                ModItemDictionary[index] += ModItem.GetStatsList;
            }

        }
        static public void AddStatToItem(int index, CustomItemStat stat) {
            if (ModItemDictionary.ContainsKey(index)) {
                ModItemDictionary[index] += stat;
            }
            else {
                throw new Exception("ModItemManager ERROR : int does not exist in ModItemDictionary");
            }
        }
        static public void AddStatToItem(int index, List<CustomItemStat> stats) {
            if (ModItemDictionary.ContainsKey(index)) {
                ModItemDictionary[index] += stats;
            }
            else {
                throw new Exception("ModItemManager ERROR : int does not exist in ModItemDictionary");
            }
        }

        static public CustomItem GetModItem(int index) {
            if (ModItemDictionary.ContainsKey(index)) {
                return ModItemDictionary[index];
            }
            else {
                throw new Exception("ModItemManager ERROR : int does not exist in ModItemDictionary");
            }
        }

        static public void Init() {
            m_DefaultModItemDictionary = new Dictionary<int, CustomItem>();

            foreach (ItemIndex itemIndex in (ItemIndex[])Enum.GetValues(typeof(ItemIndex))) {
                if (itemIndex != ItemIndex.Count && itemIndex != ItemIndex.None) {
                    m_DefaultModItemDictionary.Add((int)itemIndex, new CustomItem((int)itemIndex));
                }
            }

            //Default On Hit Effect
            DefaultOnHitEffect((int)ItemIndex.HealOnCrit, new HealOnCritHitReplace());
            DefaultOnHitEffect((int)ItemIndex.CooldownOnCrit, new CoolDownOnCritHitReplace());
            DefaultOnHitEffect((int)ItemIndex.AttackSpeedOnCrit, new AttackSpeedOnCritHitReplace());
            DefaultOnHitEffect((int)ItemIndex.Seed, new HealOnHitReplace());
            DefaultOnHitEffect((int)ItemIndex.BleedOnHit, new BleedOnHitReplace());
            DefaultOnHitEffect((int)ItemIndex.SlowOnHit, new SlowOnHitReplace());
            DefaultOnHitEffect((int)ItemIndex.GoldOnHit, new GoldOnHitReplace());
            DefaultOnHitEffect((int)ItemIndex.Missile, new MissileOnHitReplace());
            DefaultOnHitEffect((int)ItemIndex.ChainLightning, new UkeleleOnHitReplace());
            DefaultOnHitEffect((int)ItemIndex.BounceNearby, new HookEffectReplace());
            DefaultOnHitEffect((int)ItemIndex.StickyBomb, new StickyBombOnHitReplace());
            DefaultOnHitEffect((int)ItemIndex.IceRing, new IceRingEffectReplace());
            DefaultOnHitEffect((int)ItemIndex.FireRing, new FireRingEffectReplace());
            DefaultOnHitEffect((int)ItemIndex.Behemoth, new BehemotEffectReplace());

            //Default Stats
            DefaultStatItem((int)ItemIndex.Knurl, new CustomItemStat(40, StatIndex.MaxHealth));
            DefaultStatItem((int)ItemIndex.BoostHp, new CustomItemStat(0, 0, 0.1f, StatIndex.MaxHealth));
            DefaultStatItem((int)ItemIndex.PersonalShield, new CustomItemStat(25, StatIndex.MaxShield));
            DefaultStatItem((int)ItemIndex.HealWhileSafe, new CustomItemStat(0, 0, 2.5f, 1.5f, StatIndex.SafeRegen));
            DefaultStatItem((int)ItemIndex.Knurl, new CustomItemStat(1.6f, StatIndex.Regen));
            DefaultStatItem((int)ItemIndex.HealthDecay, new CustomItemStat(0, 0, -0.1f, StatIndex.Regen));
            DefaultStatItem((int)ItemIndex.SprintOutOfCombat, new CustomItemStat(0, 0, 0.3f, StatIndex.SafeRunningMoveSpeed));
            DefaultStatItem((int)ItemIndex.Hoof, new CustomItemStat(0, 0, 0.14f, StatIndex.MoveSpeed));
            DefaultStatItem((int)ItemIndex.SprintBonus, new CustomItemStat(0, 0, 0.3f, 0.2f, StatIndex.RunningMoveSpeed));
            DefaultStatItem((int)ItemIndex.Feather, new CustomItemStat(1, StatIndex.JumpCount));
            DefaultStatItem((int)ItemIndex.BoostDamage, new CustomItemStat(0, 0, 0.1f, StatIndex.Damage));
            DefaultStatItem((int)ItemIndex.Syringe, new CustomItemStat(0, 0, 0.15f, StatIndex.AttackSpeed));
            DefaultStatItem((int)ItemIndex.CritGlasses, new CustomItemStat(10, StatIndex.Crit));
            DefaultStatItem((int)ItemIndex.AttackSpeedOnCrit, new CustomItemStat(5, 0, StatIndex.Crit));
            DefaultStatItem((int)ItemIndex.CritHeal, new CustomItemStat(5, 0, StatIndex.Crit));
            DefaultStatItem((int)ItemIndex.HealOnCrit, new CustomItemStat(5, 0, StatIndex.Crit));
            DefaultStatItem((int)ItemIndex.CooldownOnCrit, new CustomItemStat(5, 0, StatIndex.Crit));
            DefaultStatItem((int)ItemIndex.SprintArmor, new CustomItemStat(30, StatIndex.RunningArmor));
            DefaultStatItem((int)ItemIndex.DrizzlePlayerHelper, new CustomItemStat(70, StatIndex.Armor));
            DefaultStatItem((int)ItemIndex.AlienHead, new CustomItemStat(0, 0, 0.75f, StatIndex.GlobalCoolDown));
            DefaultStatItem((int)ItemIndex.UtilitySkillMagazine, new CustomItemStat(0, 0, 2f / 3f, 1, StatIndex.CoolDownUtility));
            DefaultStatItem((int)ItemIndex.SecondarySkillMagazine, new CustomItemStat(1, StatIndex.CountSecondary));
            DefaultStatItem((int)ItemIndex.UtilitySkillMagazine, new CustomItemStat(2, StatIndex.CountUtility));

            Update();
        }

        static public void Update() {

            ModItemDictionary = new Dictionary<int, CustomItem>();

            foreach (KeyValuePair<int, CustomItem> kv in m_DefaultModItemDictionary) {
                ModItemDictionary.Add(kv.Key, kv.Value);
            }
        }

        static public float GetBonusForStat(CharacterBody c, StatIndex stat) {
            float value = 0;
            if (c.inventory) {
                foreach (KeyValuePair<int, CustomItem> kv in ModItemDictionary) {
                    if (ModItemDictionary.ContainsKey(kv.Key) && c.inventory.GetItemCount(kv.Key) > 0)
                        value += kv.Value.GetFlatBonusFromCount(stat, c.inventory.GetItemCount(kv.Key));
                }
            }
            return value;
        }
        static public float GetMultiplierForStat(CharacterBody c, StatIndex stat) {
            float value = 0;
            if (c.inventory) {
                foreach (KeyValuePair<int, CustomItem> kv in ModItemDictionary) {
                    if (ModItemDictionary.ContainsKey(kv.Key) && c.inventory.GetItemCount(kv.Key) > 0)
                        value += kv.Value.GetMultStackBonusFromCount(stat, c.inventory.GetItemCount(kv.Key));
                }
            }
            return value;
        }

        static public float GetMultiplierForStatCD(CharacterBody c, StatIndex stat) {
            float value = 1;
            if (c.inventory) {
                foreach (KeyValuePair<int, CustomItem> kv in ModItemDictionary) {
                    if (ModItemDictionary.ContainsKey(kv.Key) && c.inventory.GetItemCount(kv.Key) > 0)
                        if (kv.Value.GetMultStackBonusFromCount(stat, c.inventory.GetItemCount(kv.Key)) != 0)
                            value *= kv.Value.GetMultStackBonusFromCount(stat, c.inventory.GetItemCount(kv.Key));
                }
            }
            return value;
        }

        static public void OnHitEnemyEffects(GlobalEventManager globalEventManager, DamageInfo damageInfo, GameObject victim) {
            float procCoefficient = damageInfo.procCoefficient;
            CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
            CharacterMaster master = body.master;
            if (!(bool)body || procCoefficient <= 0.0 || !(bool)body || !(bool)master || !(bool)master.inventory)
                return;

            Inventory inventory = master.inventory;

            foreach (KeyValuePair<int, CustomItem> Kv in ModItemDictionary) {
                int count = inventory.GetItemCount(Kv.Key);
                if (count > 0) {
                    foreach (ModHitEffect HitEffects in Kv.Value.GetHitEffectList) {

                        if (HitEffects.EffectType == HitEffectType.OnHitEnemy && HitEffects.Condition(globalEventManager, damageInfo, victim, count)) {
                            HitEffects.Effect(globalEventManager, damageInfo, victim, count);
                        }
                    }
                }
            }
        }

        static public void OnHitAllEffects(GlobalEventManager globalEventManager, DamageInfo damageInfo, GameObject victim) {
            float procCoefficient = damageInfo.procCoefficient;
            CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
            CharacterMaster master = body.master;
            if (!(bool)body || procCoefficient <= 0.0 || !(bool)body || !(bool)master || !(bool)master.inventory)
                return;

            Inventory inventory = master.inventory;

            foreach (KeyValuePair<int, CustomItem> Kv in ModItemDictionary) {
                int count = inventory.GetItemCount(Kv.Key);
                if (count > 0) {
                    foreach (ModHitEffect HitEffects in Kv.Value.GetHitEffectList) {

                        if (HitEffects.EffectType == HitEffectType.OnHitAll && HitEffects.Condition(globalEventManager, damageInfo, victim, count)) {
                            HitEffects.Effect(globalEventManager, damageInfo, victim, count);
                        }
                    }
                }
            }
        }

    }

    static class InventoryExtender {
        public static int GetItemCount(this Inventory inv, int ItemIndex) {
            return inv.GetFieldValue<int[]>("itemStacks")[ItemIndex];
        }
    }

}
