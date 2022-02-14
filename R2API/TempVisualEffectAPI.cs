using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace R2API {
    /// <summary>
    /// API for adding custom TemporaryVisualEffects to CharacterBody components.
    /// </summary>
    [R2APISubmodule]
    public static class TempVisualEffectAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;
        private static bool _TVEsAdded;

        /// <summary>
        /// Delegate used for checking if TVE should be active (bool active). <see cref="CharacterBody.UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect, string, float, bool, string)"/>
        /// </summary>
        /// <param name="body"></param>
        public delegate bool EffectCondition(CharacterBody body);
        public struct TemporaryVisualEffectStruct {
            public TemporaryVisualEffect effect;
            public GameObject effectPrefab;
            public bool useBestFitRadius;
            public EffectCondition condition;
            public string childLocatorOverride;
            public string effectName;
        }

        internal static List<TemporaryVisualEffectStruct> tves = new List<TemporaryVisualEffectStruct>();
        internal static Dictionary<string, TemporaryVisualEffectStruct> tveDict = new Dictionary<string, TemporaryVisualEffectStruct>();
        internal const string moddedString = "R2APIModded:";

        /// <summary>
        /// Adds a custom TemporaryVisualEffect (TVEs) to the static tves List and Dict.
        /// Custom TVEs are used and updated in the CharacterBody just after vanilla TVEs.
        /// Must be called before R2APIContentPackProvider.WhenContentPackReady. Will fail if called after.
        /// Returns true if successful.
        /// </summary>
        /// <param name="effectPrefab">MUST contain a TemporaryVisualEffect component.</param>
        /// <param name="useBestFitRadius"></param>
        /// /// <param name="condition"></param>
        /// <param name="childLocatorOverride"></param>
        public static bool AddTemporaryVisualEffect(GameObject effectPrefab, EffectCondition condition, bool useBestFitRadius = false, string childLocatorOverride = "") {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(TempVisualEffectAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(TempVisualEffectAPI)})]");
            }
            if (effectPrefab == null) {
                R2API.Logger.LogError($"Failed to add TVE: GameObject is null"); throw new ArgumentNullException($"{nameof(effectPrefab)} can't be null");
            }
            if (_TVEsAdded) {
                R2API.Logger.LogError($"Failed to add TVE: {effectPrefab.name} after TVE list was created"); return false;
            }
            if (condition == null) {
                R2API.Logger.LogError($"Failed to add TVE: {effectPrefab.name} no condition attached"); return false;
            }
            if (tves.Any(name => name.effectName == effectPrefab.name)) {
                R2API.Logger.LogError($"Failed to add TVE: {effectPrefab.name} name already exists in list"); return false;
            }
            if (!effectPrefab.GetComponent<TemporaryVisualEffect>()) {
                R2API.Logger.LogError($"Failed to add TVE: {effectPrefab.name} GameObject has no TemporaryVisualEffect component"); return false;
            }

            var newTVE = new TemporaryVisualEffectStruct();

            newTVE.effectPrefab = effectPrefab;
            newTVE.useBestFitRadius = useBestFitRadius;
            newTVE.condition = condition;
            newTVE.childLocatorOverride = childLocatorOverride;
            newTVE.effectName = effectPrefab.name;

            tves.Add(newTVE);
            tveDict.Add(moddedString + newTVE.effectName, newTVE);
            R2API.Logger.LogMessage($"Added new TVE: {newTVE}");
            return true;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += UpdateAllHook;
            IL.RoR2.CharacterBody.UpdateSingleTemporaryVisualEffect += UpdateSingleHook;
            R2APIContentPackProvider.WhenAddingContentPacks += DontAllowNewEntries;
            CharacterBody.onBodyStartGlobal += BodyStart;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects -= UpdateAllHook;
            IL.RoR2.CharacterBody.UpdateSingleTemporaryVisualEffect += UpdateSingleHook;
            R2APIContentPackProvider.WhenAddingContentPacks -= DontAllowNewEntries;
            CharacterBody.onBodyStartGlobal -= BodyStart;
        }

        private static void BodyStart(CharacterBody body) {
            if (!body.gameObject.GetComponent<R2APITVEController>()) {
                body.gameObject.AddComponent<R2APITVEController>();
            }
        }

        private static void DontAllowNewEntries() {
            _TVEsAdded = true;
        }

        private static void UpdateAllHook(On.RoR2.CharacterBody.orig_UpdateAllTemporaryVisualEffects orig, CharacterBody self) {
            orig(self);
            var controller = self.gameObject.GetComponent<R2APITVEController>();
            if (controller) {
                for (int i = 0; i < controller.localTVEs.Count; i++) {
                    TempVisualEffectAPI.TemporaryVisualEffectStruct temp = controller.localTVEs[i];
                    self.UpdateSingleTemporaryVisualEffect(ref temp.effect, moddedString + temp.effectName, temp.useBestFitRadius ? self.radius : self.bestFitRadius, temp.condition.Invoke(self), temp.childLocatorOverride);
                    controller.localTVEs[i] = temp;
                }
            }
        }

        private static void UpdateSingleHook(ILContext il) {
            var cursor = new ILCursor(il);

            GameObject GetCustomTVE(GameObject vanillaLoaded, string resourceString)
                {
                if (!vanillaLoaded) {
                    if (tveDict.TryGetValue(resourceString, out var customTVEPrefab)) {
                        return customTVEPrefab.effectPrefab;
                    }
                }
                return vanillaLoaded;

            }

            var resourceStringIndex = -1;
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(out resourceStringIndex),
                x => x.MatchCallOrCallvirt<Resources>(nameof(Resources.Load))
            )) {
                cursor.Emit(OpCodes.Ldarg, resourceStringIndex);
                cursor.EmitDelegate<Func<GameObject, string, GameObject>>(GetCustomTVE);
            }
            else {
                R2API.Logger.LogError($"{nameof(UpdateSingleHook)} failed.");
            }
        }
    }

    /// <summary>
    /// Contains a local list of custom TemporaryVisualEffects for each CharacterBody.
    /// </summary>
    public class R2APITVEController : MonoBehaviour {
        public List<TempVisualEffectAPI.TemporaryVisualEffectStruct> localTVEs = new List<TempVisualEffectAPI.TemporaryVisualEffectStruct>(TempVisualEffectAPI.tves);
    }
}
