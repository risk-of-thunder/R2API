using R2API.Utils;
using RoR2;
using System;
using System.Reflection;
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

            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), effect);
            return true;
        }

        /// <summary>
        /// Adds an EffectDef to the EffectCatalog when the catalog inits.
        /// </summary>
        /// <param name="effect">The EffectDef to addZ</param>
        /// <returns>False if the EffectDef was null</returns>
        [Obsolete($"This method is obsolete, please Add the EffectDef using the method AddEffect(GameObject? effect)")]
        public static bool AddEffect(EffectDef? effect) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(EffectAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(EffectAPI)})]");
            }
            if (effect == null) {
                R2API.Logger.LogError("EffectDef was null.");
                return false;
            }

            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), effect.prefab);
            return true;
        }
    }
}
