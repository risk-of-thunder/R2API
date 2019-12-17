using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class EffectAPI {
     
        /// <summary>
        /// Adds an effect to the EffectCatalog
        /// Can be called at any time.
        /// </summary>
        /// <param name="effect">The prefab of the effect to be added</param>
        /// <returns>True if the effect was added</returns>
        public static bool AddEffect(GameObject effect) {
            /*List<GameObject> effects = EffectManager.instance.GetFieldValue<List<GameObject>>("effectPrefabsList");
            Dictionary<GameObject, uint> effectLookup = EffectManager.instance.GetFieldValue<Dictionary<GameObject, uint>>("effectPrefabToIndexMap");

            if(!effect) {
                return false;
            }

            int index = effects.Count;

            effects.Add( effect );
            effectLookup.Add( effect, (uint)index );

            return true;*/

            R2API.Logger.LogError("EffectAPI is currently broken for now");

            return false;
        }
    }
}
