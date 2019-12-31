using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class EffectAPI {
        #region Loaded check
        //Maybe best to set up a base class or interface that does this automatically?
        public static bool Loaded {
            get {
                return IsLoaded;
            }
        }
        private static bool IsLoaded = false;
        #endregion

        /// <summary>
        /// Mimics events found in CatalogModHelpers, can be used to add or sort effects.
        /// </summary>
        public static event Action<List<EffectDef>> getAdditionalEntries;

        [R2APISubmoduleInit( Stage = InitStage.SetHooks )]
        internal static void SetHooks() {
            On.RoR2.EffectCatalog.GetDefaultEffectDefs += EffectCatalog_GetDefaultEffectDefs;
        }

        [R2APISubmoduleInit( Stage = InitStage.UnsetHooks )]
        internal static void UnsetHooks() {
            On.RoR2.EffectCatalog.GetDefaultEffectDefs -= EffectCatalog_GetDefaultEffectDefs;
        }

        private static EffectDef[] EffectCatalog_GetDefaultEffectDefs( On.RoR2.EffectCatalog.orig_GetDefaultEffectDefs orig ) {
            EffectDef[] effects = orig();

            var effectList = effects.ToList();
            if( getAdditionalEntries != null ) {
                getAdditionalEntries( effectList );
            }
            return effectList.ToArray();
        }


        /// <summary>
        /// Creates an EffectDef from a prefab and adds it to the EffectCatalog.
        /// The prefab must have an the following components: EffectComponent, VFXAttributes
        /// For more control over the EffectDef, use AddEffect( EffectDef )
        /// </summary>
        /// <param name="effect">The prefab of the effect to be added</param>
        /// <returns>True if the effect was added</returns>
        public static bool AddEffect(GameObject effect) {
            if( !IsLoaded ) {
                R2API.Logger.LogError( "EffectAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]" );
                return false;
            }
            if( effect == null ) {
                Debug.LogError( "Effect prefab was null" );
                return false;
            }

            EffectComponent effectComp = effect.GetComponent<EffectComponent>();
            if( effectComp == null ) {
                Debug.LogErrorFormat( "Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name );
                return false;
            }

            VFXAttributes vfxAtrib = effect.GetComponent<VFXAttributes>();
            if( vfxAtrib == null ) {
                Debug.LogErrorFormat( "Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name );
                return false;
            }

            EffectDef def = new EffectDef {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAtrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };

            return AddEffect( def );
        }

        /// <summary>
        /// Adds an EffectDef to the EffectCatalog when the catalog inits.
        /// </summary>
        /// <param name="effect">The EffectDef to addZ</param>
        /// <returns>False if the EffectDef was null</returns>
        public static bool AddEffect( EffectDef effect ) {
            if( !IsLoaded ) {
                R2API.Logger.LogError( "EffectAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]" );
                return false;
            }
            if( effect == null ) {
                R2API.Logger.LogError( "EffectDef was null." );
                return false;
            }

            getAdditionalEntries += ( list ) => list.Add( effect );
            return true;
        }
    }
}
