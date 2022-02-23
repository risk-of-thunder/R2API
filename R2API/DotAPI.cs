using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {

    /// <summary>
    /// API for adding damage over time effects to the game.
    /// </summary>
    [R2APISubmodule]
    public static class DotAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static DotController.DotDef[] DotDefs {
            get => DotController.dotDefs;
            set => DotController.dotDefs = value;
        }

        private static void ResizeDotDefs(int newSize) {
            var dotDefs = DotDefs;
            Array.Resize(ref dotDefs, newSize);
            DotDefs = dotDefs;
        }

        private static readonly List<DotController.DotDef> CustomDots = new List<DotController.DotDef>();

        public static int VanillaDotCount {get; private set;}
        public static int CustomDotCount => CustomDots.Count;


        private static readonly Dictionary<DotController, bool[]> ActiveCustomDots = new Dictionary<DotController, bool[]>();

        /// <summary>
        /// Allows for custom behaviours when applying the dot. EG, percentburn. <see cref="DotController.AddDot(GameObject, float, DotController.DotIndex, float)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="dotStack"></param>
        public delegate void CustomDotBehaviour(DotController self, DotController.DotStack dotStack);

        private static CustomDotBehaviour[] _customDotBehaviours = new CustomDotBehaviour[0];

        /// <summary>
        /// Allows custom visuals for your buff. think bleeding etc. <see cref="DotController.FixedUpdate"/>
        /// </summary>
        /// <param name="self"></param>
        public delegate void CustomDotVisual(DotController self);

        private static CustomDotVisual[] _customDotVisuals = new CustomDotVisual[0];

        /// <summary>
        /// customDotBehaviour code will be executed when the dot is added to the target.
        /// Please refer to the game AddDot() method for example use case.
        /// customDotVisual code will be executed in the FixedUpdate of the DotController.
        /// Please refer to the game FixedUpdate() method for example use case.
        /// </summary>
        /// <param name="dotDef"></param>
        /// <param name="customDotBehaviour"></param>
        /// <param name="customDotVisual"></param>
        /// <returns></returns>
        public static DotController.DotIndex RegisterDotDef(DotController.DotDef? dotDef,
            CustomDotBehaviour? customDotBehaviour = null, CustomDotVisual? customDotVisual = null) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DotAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DotAPI)})]");
            }

            var dotDefIndex = VanillaDotCount + CustomDotCount;

            if (DotDefs != null) {
                ResizeDotDefs(dotDefIndex + 1);
                DotDefs[dotDefIndex] = dotDef;
            }

            CustomDots.Add(dotDef);

            var customArrayIndex = _customDotBehaviours.Length;
            Array.Resize(ref _customDotBehaviours, _customDotBehaviours.Length + 1);
            _customDotBehaviours[customArrayIndex] = customDotBehaviour;

            Array.Resize(ref _customDotVisuals, _customDotVisuals.Length + 1);
            _customDotVisuals[customArrayIndex] = customDotVisual;

            if (dotDef.associatedBuff != null) {
                R2API.Logger.LogInfo($"Custom Dot (Index: {dotDefIndex}) that uses Buff : {dotDef.associatedBuff.name} added");
            }
            else {
                R2API.Logger.LogInfo($"Custom Dot (Index: {dotDefIndex}) with no associated Buff added");
            }


            return (DotController.DotIndex)dotDefIndex;
        }

        /// <summary>
        /// Unrolled version of RegisterDotDef(DotController.DotDef, CustomDotBehaviour, CustomDotVisual)
        /// <see cref="RegisterDotDef(DotController.DotDef, CustomDotBehaviour, CustomDotVisual)"/>
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="damageCoefficient"></param>
        /// <param name="colorIndex"></param>
        /// <param name="associatedBuff">The buff associated with the DOT, can be null</param>
        /// <param name="customDotBehaviour"></param>
        /// <param name="customDotVisual"></param>
        /// <returns></returns>
        public static DotController.DotIndex RegisterDotDef(float interval, float damageCoefficient,
            DamageColorIndex colorIndex, BuffDef associatedBuff = null, CustomDotBehaviour customDotBehaviour = null,
            CustomDotVisual customDotVisual = null) {
            var dotDef = new DotController.DotDef {
                associatedBuff = associatedBuff,
                damageCoefficient = damageCoefficient,
                interval = interval,
                damageColorIndex = colorIndex
            };
            return RegisterDotDef(dotDef, customDotBehaviour, customDotVisual);
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.DotController.InitDotCatalog += RetrieveVanillaCount;
            IL.RoR2.DotController.Awake += ResizeTimerArray;
            On.RoR2.DotController.InitDotCatalog += AddCustomDots;
            On.RoR2.DotController.Awake += TrackActiveCustomDots;
            On.RoR2.DotController.OnDestroy += TrackActiveCustomDots2;
            On.RoR2.DotController.GetDotDef += GetDotDef;
            On.RoR2.DotController.FixedUpdate += FixedUpdate;
            IL.RoR2.DotController.InflictDot_refInflictDotInfo += FixInflictDotReturnCheck;
            IL.RoR2.DotController.AddDot += CallCustomDotBehaviours;
            On.RoR2.DotController.HasDotActive += OnHasDotActive;
            IL.RoR2.DotController.EvaluateDotStacksForType += EvaluateDotStacksForType;

            IL.RoR2.GlobalEventManager.OnHitEnemy += FixDeathMark;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.DotController.InitDotCatalog -= RetrieveVanillaCount;
            IL.RoR2.DotController.Awake -= ResizeTimerArray;
            On.RoR2.DotController.Awake -= TrackActiveCustomDots;
            On.RoR2.DotController.OnDestroy -= TrackActiveCustomDots2;
            On.RoR2.DotController.GetDotDef -= GetDotDef;
            On.RoR2.DotController.FixedUpdate -= FixedUpdate;
            IL.RoR2.DotController.InflictDot_refInflictDotInfo -= FixInflictDotReturnCheck;
            IL.RoR2.DotController.AddDot -= CallCustomDotBehaviours;
            On.RoR2.DotController.HasDotActive -= OnHasDotActive;
            IL.RoR2.DotController.EvaluateDotStacksForType -= EvaluateDotStacksForType;

            IL.RoR2.GlobalEventManager.OnHitEnemy -= FixDeathMark;
        }

        private static void EvaluateDotStacksForType(ILContext il) {
            //Empty IL hook that doesn't add nor delete anything.
            //The purpose of it is to fix weird issue with DamageAPI.
            //For some reason if an IL hook for `DotController.EvaluateDotStacksForType` is added in `DamageAPI`
            //calling `self.EvaluateDotStacksForType` in `DotAPI.FixedUpdate` throws NRE in it because
            //instead of calling `DotController.GetDotDef` hook for some reason original method is called which returns null for modded DOT.
            //Seems like it's important that this hook is applied after `DotController.GetDotDef` otherwise the issue persits.
            //idk what is the reason for that behaviour, probably something to do with optimizations because
            //if you replace `mono-2.0-bdwgc.dll` in the game files with the one that allows you to debug code with dnSpy
            //the issue dissapear even without this hook.
        }

        private static void RetrieveVanillaCount(ILContext il) {
            var c = new ILCursor(il);
            if (c.TryGotoNext(
                i => i.MatchLdcI4(out VanillaDotCount),
                i => i.MatchNewarr<DotController.DotDef>())) {
            }
            else {
                R2API.Logger.LogError("Failed finding IL Instructions. Aborting RetrieveVanillaCount IL Hook");
            }
        }

        private static void ResizeTimerArray(ILContext il) {
            var c = new ILCursor(il);
            if (c.TryGotoNext(
                i => i.MatchLdcI4(VanillaDotCount),
                i => i.MatchNewarr<float>())) {
                c.Index++;
                c.EmitDelegate<Func<int, int>>(i => DotDefs.Length);
            }
            else {
                R2API.Logger.LogError("Failed finding IL Instructions. Aborting ResizeTimerArray IL Hook");
            }
        }

        private static void AddCustomDots(On.RoR2.DotController.orig_InitDotCatalog orig) {
            orig();

            DotController.dotDefs = DotController.dotDefs.Concat(CustomDots).ToArray();
        }

        private static void TrackActiveCustomDots(On.RoR2.DotController.orig_Awake orig, DotController self) {
            orig(self);

            ActiveCustomDots.Add(self, new bool[CustomDotCount]);
        }

        private static void TrackActiveCustomDots2(On.RoR2.DotController.orig_OnDestroy orig, DotController self) {
            orig(self);

            ActiveCustomDots.Remove(self);
        }

        private static object GetDotDef(On.RoR2.DotController.orig_GetDotDef orig, DotController self,
            DotController.DotIndex dotIndex) {
            return DotDefs[(int)dotIndex];
        }

        private static void FixedUpdate(On.RoR2.DotController.orig_FixedUpdate orig, DotController self) {
            orig(self);

            if (NetworkServer.active) {
                for (var i = VanillaDotCount; i < DotDefs.Length; i++) {
                    var dotDef = DotDefs[i];
                    var dotTimers = self.dotTimers;

                    float dotProcTimer = dotTimers[i] - Time.fixedDeltaTime;
                    if (dotProcTimer <= 0f) {
                        dotProcTimer += dotDef.interval;

                        self.EvaluateDotStacksForType((DotController.DotIndex)i, dotDef.interval, out var remainingActive);

                        ActiveCustomDots[self][i - VanillaDotCount] = remainingActive != 0;
                    }

                    dotTimers[i] = dotProcTimer;
                }
            }

            for (var i = 0; i < CustomDotCount; i++) {
                if (ActiveCustomDots[self][i]) {
                    _customDotVisuals[i]?.Invoke(self);
                }
            }
        }

        private static void FixInflictDotReturnCheck(ILContext il) {
            var c = new ILCursor(il);

            // ReSharper disable once InconsistentNaming
            static void ILFailMessage(int index) {
                R2API.Logger.LogError(
                    $"Failed finding IL Instructions. Aborting FixInflictDotReturnCheck IL Hook {index}");
            }

            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld(out _),
                i => i.MatchLdcI4(out _),
                i => i.MatchBlt(out _),

                i => i.MatchLdarg(0),
                i => i.MatchLdfld(out _),
                i => i.MatchLdcI4(out _),
                i => i.MatchBlt(out _)
                )) {
                c.Next.OpCode = OpCodes.Nop;
            }
            else {
                ILFailMessage(1);
            }
        }

        private static void CallCustomDotBehaviours(ILContext il) {
            var c = new ILCursor(il);
            int dotStackLoc = 0;

            // ReSharper disable once InconsistentNaming
            static void ILFailMessage(int index) {
                R2API.Logger.LogError(
                    $"Failed finding IL Instructions. Aborting OnAddDot IL Hook {index}");
            }

            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<DotController>("dotStackPool"),
                i => i.MatchCallOrCallvirt(out _),
                i => i.MatchStloc(out dotStackLoc))) {
                if (c.TryGotoNext(
                    i => i.MatchLdarg(out _),
                    i => i.MatchSwitch(out _))) {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloc, dotStackLoc);
                    c.EmitDelegate<Action<DotController, DotController.DotStack>>((self, dotStack) => {
                        if ((int)dotStack.dotIndex >= VanillaDotCount) {
                            var customDotIndex = (int)dotStack.dotIndex - VanillaDotCount;
                            _customDotBehaviours[customDotIndex]?.Invoke(self, dotStack);
                        }
                    });
                }
            }
            else {
                ILFailMessage(1);
            }
        }

        private static bool OnHasDotActive(On.RoR2.DotController.orig_HasDotActive orig, DotController self,
            DotController.DotIndex dotIndex) {
            if ((int)dotIndex >= VanillaDotCount) {
                if (ActiveCustomDots.TryGetValue(self, out var activeDots)) {
                    return activeDots[(int)dotIndex - VanillaDotCount];
                }

                return false;
            }

            return orig(self, dotIndex);
        }

        private static void FixDeathMark(ILContext il) {
            var c = new ILCursor(il);
            int dotControllerLoc = 0;
            int numberOfDebuffAndDotLoc = 0;

            // ReSharper disable once InconsistentNaming
            static void ILFailMessage(int index) {
                R2API.Logger.LogError(
                    $"Failed finding IL Instructions. Aborting FixDeathMark IL Hook {index}");
            }

            if (c.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(DotController), nameof(DotController.HasDotActive)))) {
                if (c.TryGotoNext(i => i.MatchLdloc(out numberOfDebuffAndDotLoc))) {

                }
                else {
                    ILFailMessage(2);
                }
            }
            else {
                ILFailMessage(1);
            }

            if (c.TryGotoPrev(MoveType.After,
                i => i.MatchCallOrCallvirt(typeof(DotController), nameof(DotController.FindDotController)),
                i => i.MatchStloc(out dotControllerLoc))) {

                static int CountCustomDots(DotController dotController, int numberOfDebuffAndDotLoc) {

                    if (dotController) {
                        for (var i = VanillaDotCount; i < VanillaDotCount + CustomDotCount; i++) {
                            var dotIndex = (DotController.DotIndex)i;
                            if (dotController.HasDotActive(dotIndex)) {
                                numberOfDebuffAndDotLoc++;
                            }
                        }
                    }

                    return numberOfDebuffAndDotLoc;
                }

                c.Emit(OpCodes.Ldloc, dotControllerLoc);
                c.Emit(OpCodes.Ldloc, numberOfDebuffAndDotLoc);
                c.EmitDelegate<Func<DotController, int, int>>(CountCustomDots);
                c.Emit(OpCodes.Stloc, numberOfDebuffAndDotLoc);
            }
            else {
                ILFailMessage(3);
            }
        }
    }
}
