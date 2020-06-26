using System;
using System.Collections.Generic;
using R2API.Utils;
using RoR2.Skills;

namespace R2API {
    [Obsolete("Please use LoadoutAPI instead")]
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class SkillAPI {

        /// <summary>
        /// Adds a type for a skill EntityState to the SkillsCatalog.
        /// State must derive from EntityStates.EntityState.
        /// Note that SkillDefs and SkillFamiles must also be added seperately.
        /// </summary>
        /// <param name="t">The type to add</param>
        /// <returns>True if succesfully added</returns>
        public static bool AddSkill(Type t) {
            if (t == null || !t.IsSubclassOf(typeof(EntityStates.EntityState)) || t.IsAbstract) {
                return false;
            }
            Type stateTab = typeof(EntityStates.EntityState).Assembly.GetType("EntityStates.StateIndexTable");
            Type[] id2State = stateTab.GetFieldValue<Type[]>("stateIndexToType");
            string[] name2Id = stateTab.GetFieldValue<string[]>("stateIndexToTypeName");
            Dictionary<Type, short> state2Id = stateTab.GetFieldValue<Dictionary<Type, short>>("stateTypeToIndex");
            int ogNum = id2State.Length;
            Array.Resize<Type>(ref id2State, ogNum + 1);
            Array.Resize<String>(ref name2Id, ogNum + 1);
            id2State[ogNum] = t;
            name2Id[ogNum] = t.FullName;
            state2Id[t] = (short)ogNum;
            stateTab.SetFieldValue<Type[]>("stateIndexToType", id2State);
            stateTab.SetFieldValue<String[]>("stateIndexToTypeName", name2Id);
            stateTab.SetFieldValue<Dictionary<Type, Int16>>("stateTypeToIndex", state2Id);
            return true;
        }

        /// <summary>
        /// Registers an event to add a SkillDef to the SkillDefCatalog.
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="s">The SkillDef to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillDef(SkillDef s) {
            if (!s) return false;
            SkillCatalog.getAdditionalSkillDefs += (list) => {
                list.Add(s);
            };
            return true;
        }

        /// <summary>
        /// Registers an event to add a SkillFamily to the SkillFamiliesCatalog
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="sf">The skillfamily to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillFamily(SkillFamily sf) {
            if (!sf) return false;
            SkillCatalog.getAdditionalSkillFamilies += (list) => {
                list.Add(sf);
            };
            return true;
        }
    }
}
