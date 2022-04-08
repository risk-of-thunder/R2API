using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace R2API {
    /// <summary>
    /// API for reserving unique ProcTypes.
    /// </summary>
    [R2APISubmodule]
    public static class ProcTypeAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        internal static uint currentProcTypeIndex = (uint)ProcType.Count + 1;

        /// <summary>
        /// Reserve a unique ProcType for use with ProcChainMask.
        /// </summary>
        public static ProcType ReserveProcType() {
            if (!Loaded)
            {
                throw new InvalidOperationException($"{nameof(ProcTypeAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ProcTypeAPI)})]");
            }

            return (ProcType)currentProcTypeIndex++;
        }
    }
}
