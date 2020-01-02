using R2API.Utils;
using EntityStates;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;

namespace R2API {
    [R2APISubmodule]
    public static class LoadoutAPI {
        #region Loaded check
        //Maybe best to set up a base class or interface that does this automatically?
        public static bool Loaded {
            get {
                return IsLoaded;
            }
        }
        private static bool IsLoaded = false;
        #endregion

        #region Submodule Hooks
        private static HashSet<SkillDef> addedSkills = new HashSet<SkillDef>();
        private static HashSet<SkillFamily> addedSkillFamilies = new HashSet<SkillFamily>();
        private static HashSet<SkinDef> addedSkins = new HashSet<SkinDef>();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkillVariantLocked += CheckSkillVariantValid;
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkinLocked += CheckSkinValid;
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.ToXml += BodyLoadout_ToXml;
        }

        [R2APISubmoduleInit( Stage = InitStage.UnsetHooks )]
        internal static void UnsetHooks() {
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkillVariantLocked -= CheckSkillVariantValid;
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.IsSkinLocked -= CheckSkinValid;
            On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.ToXml -= BodyLoadout_ToXml;
        }
        #endregion

        #region EntityState fixes
        // ReSharper disable InconsistentNaming
        private static readonly Hook _detourSet_stateType = new Hook(
            typeof(SerializableEntityStateType).GetMethodCached("set_stateType"),
            typeof(EntityAPI).GetMethodCached(nameof(set_stateType_Hook))
        );

        private static readonly Hook _detourSet_typeName = new Hook(
            typeof(SerializableEntityStateType).GetMethodCached("set_typeName"),
            typeof(EntityAPI).GetMethodCached(nameof(set_typeName_Hook))
        );
        // ReSharper restore InconsistentNaming

        internal static void set_stateType_Hook( ref SerializableEntityStateType self, Type value ) =>
            self.SetStructFieldValue( "_typeName",
            value != null && value.IsSubclassOf( typeof( EntityState ) )
            ? value.AssemblyQualifiedName
            : "" );

        internal static void set_typeName_Hook( ref SerializableEntityStateType self, string value ) =>
            set_stateType_Hook( ref self, Type.GetType( value ) ?? GetTypeAllAssemblies( value ) );

        private static Type GetTypeAllAssemblies( string name ) {
            Type type = null;

            var assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                // Assembly-CSharp will be checked earlier if we order by name
                .OrderBy(asm => asm.FullName);

            foreach( var asm in assemblies ) {
                type = asm.GetTypes().FirstOrDefault( t => t.Name == name || t.FullName == name );

                if( type != null )
                    break;
            }

            return type;
        }
        #endregion

        #region User Profile Fixes
        private static bool CheckSkinValid(On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.orig_IsSkinLocked orig, object self, RoR2.UserProfile userProfile) {
            return !self.InvokeMethod<bool>("IsSkinValid") || orig(self, userProfile);
        }

        private static bool CheckSkillVariantValid(On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.orig_IsSkillVariantLocked orig, object self, int skillSlotIndex, RoR2.UserProfile userProfile) {
            return !self.InvokeMethod<bool>("IsSkillVariantValid", skillSlotIndex) || orig(self, skillSlotIndex, userProfile);
        }

        private static System.Xml.Linq.XElement BodyLoadout_ToXml( On.RoR2.Loadout.BodyLoadoutManager.BodyLoadout.orig_ToXml orig, System.Object self, String elementName ) {
            var bodyIndex = self.GetFieldValue<int>("bodyIndex");
            var bodySkinController = BodyCatalog.GetBodyPrefab( bodyIndex ).GetComponent<ModelLocator>().modelTransform.GetComponent<ModelSkinController>();
            var skinPreference = self.GetFieldValue<uint>("skinPreference" );
            if( addedSkins.Contains( bodySkinController.skins[skinPreference] ) ) {
                self.SetFieldValue<uint>( "skinPreference", 0u );
            }
            var skillPreferences = self.GetFieldValue<uint[]>("skillPreferences" );
            var allBodyInfosObj = typeof( Loadout.BodyLoadoutManager ).GetFieldValue<object>( "allBodyInfos" );
            var allBodyInfos = ((Array)allBodyInfosObj).Cast<object>().ToArray();
            var currentInfo = allBodyInfos[bodyIndex];
            var prefabSkillSlotsObj = currentInfo.GetFieldValue<object>( "prefabSkillSlots" );
            var prefabSkillSlots = ((Array)prefabSkillSlotsObj).Cast<object>().ToArray();
            var skillFamilyIndices = currentInfo.GetFieldValue<int[]>( "skillFamilyIndices" );
            for( int i = 0; i < prefabSkillSlots.Length; i++ ) {
                var skillFamilyIndex = skillFamilyIndices[i];
                SkillFamily family = SkillCatalog.GetSkillFamily( skillFamilyIndex );
                SkillDef def = family.variants[skillPreferences[i]].skillDef;
                if( addedSkills.Contains( def ) ) {
                    skillPreferences[i] = 0u;
                }
            }
            return orig( self, elementName );
        }
        #endregion

        #region Adding Skills
        /// <summary>
        /// Adds a type for a skill EntityState to the SkillsCatalog.
        /// State must derive from EntityStates.EntityState.
        /// Note that SkillDefs and SkillFamiles must also be added seperately.
        /// </summary>
        /// <param name="t">The type to add</param>
        /// <returns>True if succesfully added</returns>
        public static bool AddSkill( Type t ) {
            if( !IsLoaded ) {
                R2API.Logger.LogError( "LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]" );
                return false;
            }
            if( t == null || !t.IsSubclassOf( typeof( EntityStates.EntityState ) ) || t.IsAbstract ) {
                R2API.Logger.LogError( "Invalid skill type." );
                return false;
            }
            Type stateTab = typeof(EntityStates.EntityState).Assembly.GetType("EntityStates.StateIndexTable");
            Type[] id2State = stateTab.GetFieldValue<Type[]>("stateIndexToType");
            string[] name2Id = stateTab.GetFieldValue<string[]>("stateIndexToTypeName");
            Dictionary<Type, short> state2Id = stateTab.GetFieldValue<Dictionary<Type, short>>("stateTypeToIndex");
            int ogNum = id2State.Length;
            Array.Resize<Type>( ref id2State, ogNum + 1 );
            Array.Resize<String>( ref name2Id, ogNum + 1 );
            id2State[ogNum] = t;
            name2Id[ogNum] = t.FullName;
            state2Id[t] = (short)ogNum;
            stateTab.SetFieldValue<Type[]>( "stateIndexToType", id2State );
            stateTab.SetFieldValue<String[]>( "stateIndexToTypeName", name2Id );
            stateTab.SetFieldValue<Dictionary<Type, Int16>>( "stateTypeToIndex", state2Id );
            return true;
        }

        /// <summary>
        /// Registers an event to add a SkillDef to the SkillDefCatalog.
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="s">The SkillDef to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillDef( SkillDef s ) {
            if( !IsLoaded ) {
                R2API.Logger.LogError( "LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]" );
                return false;
            }
            if( !s ) {
                R2API.Logger.LogError( "Invalid SkillDef" );
                return false;
            }
            addedSkills.Add( s );
            SkillCatalog.getAdditionalSkillDefs += ( list ) => {
                list.Add( s );
            };
            return true;
        }

        /// <summary>
        /// Registers an event to add a SkillFamily to the SkillFamiliesCatalog
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="sf">The skillfamily to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillFamily( SkillFamily sf ) {
            if( !IsLoaded ) {
                R2API.Logger.LogError( "LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]" );
                return false;
            }
            if( !sf ) {
                R2API.Logger.LogError( "Invalid SkillFamily" );
                return false;
            }
            addedSkillFamilies.Add( sf );
            SkillCatalog.getAdditionalSkillFamilies += ( list ) => {
                list.Add( sf );
            };
            return true;
        }

        #endregion

        #region Adding Skins
        /// <summary>
        /// A container struct for all SkinDef parameters.
        /// Use this to set skinDef values, then call CreateNewSkinDef().
        /// </summary>
        public struct SkinDefInfo {
            public SkinDef[] baseSkins;
            public Sprite icon;
            public string nameToken;
            public string unlockableName;
            public GameObject rootObject;
            public CharacterModel.RendererInfo[] rendererInfos;
            public string name;
        }

        /// <summary>
        /// Creates a new SkinDef from a SkinDefInfo.
        /// Note that this prevents null-refs by disabling SkinDef awake while the SkinDef is being created.
        /// The things that occur during awake are performed when first applied to a character instead.
        /// </summary>
        /// <param name="skin"></param>
        /// <returns></returns>
        public static SkinDef CreateNewSkinDef( SkinDefInfo skin ) {
            if( !IsLoaded ) {
                R2API.Logger.LogError( "LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]" );
                return null;
            }
            On.RoR2.SkinDef.Awake += DoNothing;

            SkinDef newSkin = ScriptableObject.CreateInstance<SkinDef>();

            newSkin.baseSkins = skin.baseSkins;
            newSkin.icon = skin.icon;
            newSkin.unlockableName = skin.unlockableName;
            newSkin.rootObject = skin.rootObject;
            newSkin.rendererInfos = skin.rendererInfos;
            newSkin.nameToken = skin.nameToken;
            newSkin.name = skin.name;

            On.RoR2.SkinDef.Awake -= DoNothing;

            addedSkins.Add( newSkin );
            return newSkin;
        }

        private static void DoNothing( On.RoR2.SkinDef.orig_Awake orig, SkinDef self ) {
            //Intentionally do nothing
        }
        #endregion
    }
}
