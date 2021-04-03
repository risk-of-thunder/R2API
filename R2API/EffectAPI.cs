using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace R2API {

    [R2APISubmodule]
    public static class EffectAPI {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        // ReSharper disable once ConvertToAutoProperty
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static readonly List<EffectDef> AddedEffects = new List<EffectDef>();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            R2APIContentPackProvider.WhenContentPackReady += AddAdditionalEntries;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            R2APIContentPackProvider.WhenContentPackReady -= AddAdditionalEntries;
        }

        private static void AddAdditionalEntries(ContentPack r2apiContentPack) {
            foreach (var customEffect in AddedEffects) {
                R2API.Logger.LogInfo($"Custom Effect: {customEffect.prefabName} added");
            }

            r2apiContentPack.effectDefs = AddedEffects.ToArray();
        }

        /// <summary>
        /// Creates an EffectDef from a prefab and adds it to the EffectCatalog.
        /// The prefab must have an the following components: EffectComponent, VFXAttributes
        /// For more control over the EffectDef, use AddEffect(EffectDef)
        /// </summary>
        /// <param name="effect">The prefab of the effect to be added</param>
        /// <returns>True if the effect was added</returns>
        public static bool AddEffect(GameObject? effect) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(EffectAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(EffectAPI)})]");
            }

            if (effect == null) {
                Debug.LogError("Effect prefab was null");
                return false;
            }

            var effectComp = effect.GetComponent<EffectComponent>();
            if (effectComp == null) {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
                return false;
            }

            var vfxAttrib = effect.GetComponent<VFXAttributes>();
            if (vfxAttrib == null) {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
                return false;
            }

            var def = new EffectDef {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };

            return AddEffect(def);
        }

        /// <summary>
        /// Adds an EffectDef to the EffectCatalog when the catalog inits.
        /// </summary>
        /// <param name="effect">The EffectDef to addZ</param>
        /// <returns>False if the EffectDef was null</returns>
        public static bool AddEffect(EffectDef? effect) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(EffectAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(EffectAPI)})]");
            }
            if (effect == null) {
                R2API.Logger.LogError("EffectDef was null.");
                return false;
            }

            AddedEffects.Add(effect);
            return true;
        }
    }
}
