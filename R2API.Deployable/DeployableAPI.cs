using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace R2API {
    /// <summary>
    /// API for handling deployables added by mods
    /// </summary>
    public static class DeployableAPI {
        public const string PluginGUID = R2API.PluginGUID + ".deployable";
        public const string PluginName = R2API.PluginName + ".Deployable";
        public const string PluginVersion = "0.0.1";

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        [Obsolete(R2APISubmoduleDependency.propertyObsolete)]
        public static bool Loaded => true;

        private static bool _loaded;

        private static readonly Dictionary<DeployableSlot, GetDeployableSameSlotLimit> moddedDeployables = new Dictionary<DeployableSlot, GetDeployableSameSlotLimit>();

        public static int VanillaDeployableSlotCount { get; }
        public static int ModdedDeployableSlotCount { get; private set; }

        static DeployableAPI() {
            VanillaDeployableSlotCount = Enum.GetValues(typeof(DeployableSlot)).Length;
        }

        internal static void SetHooks() {
            IL.RoR2.CharacterMaster.GetDeployableSameSlotLimit += GetDeployableSameSlotLimitIL;
        }

        internal static void UnsetHooks() {
            IL.RoR2.CharacterMaster.GetDeployableSameSlotLimit -= GetDeployableSameSlotLimitIL;
        }

        private static void GetDeployableSameSlotLimitIL(MonoMod.Cil.ILContext il) {
            var c = new ILCursor(il);

            ILLabel switchEndLabel = null;
            c.GotoNext(MoveType.After,
                x => x.MatchSwitch(out _),
                x => x.MatchBr(out switchEndLabel));

            var switchDefaultLabel = il.DefineLabel();
            c.Previous.Operand = switchDefaultLabel;

            c.GotoLabel(switchEndLabel, MoveType.Before);
            c.Emit(OpCodes.Ldarg_0);
            switchDefaultLabel.Target = c.Previous;
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldloc, 1);
            c.Emit(OpCodes.Ldloca, 0);
            c.Emit(OpCodes.Call, typeof(DeployableAPI).GetMethod(nameof(GetModdedDeployableSameSlotLimit), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static void GetModdedDeployableSameSlotLimit(CharacterMaster self, DeployableSlot slot, int deployableCountMultiplier, ref int slotLimit) {
            if (moddedDeployables.TryGetValue(slot, out var getDeployableSameSlotLimit)) {
                slotLimit = getDeployableSameSlotLimit.Invoke(self, deployableCountMultiplier);
            }
        }

        /// <summary>
        /// Register new DeployableSlot with callback function to get deployable limit.
        /// </summary>
        /// <param name="getDeployableSameSlotLimit">Will be executed when new deployable added with returned DeployableSlot.</param>
        /// <returns>DeployableSlot that you should use when call `CharacterMaster.AddDeployable`</returns>
        public static DeployableSlot RegisterDeployableSlot(GetDeployableSameSlotLimit getDeployableSameSlotLimit) {

            if (getDeployableSameSlotLimit == null) {
                throw new ArgumentNullException($"{nameof(getDeployableSameSlotLimit)} can't be null");
            }

            var deployableSlot = (DeployableSlot)VanillaDeployableSlotCount + ModdedDeployableSlotCount++;
            moddedDeployables[deployableSlot] = getDeployableSameSlotLimit;

            return deployableSlot;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="self">Instance of a `CharacterMaster` for which this method is executed</param>
        /// <param name="deployableCountMultiplier">Multiplier for minion count (if Swarms artifact is enabled value will be 2).
        /// You don't have to use it, but you can for stuff like Beetle Guards</param>
        /// <returns></returns>
        public delegate int GetDeployableSameSlotLimit(CharacterMaster self, int deployableCountMultiplier);
    }
}
