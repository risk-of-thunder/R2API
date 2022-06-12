using BepInEx.Logging;
using JetBrains.Annotations;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API.Utils {

    /// <summary>
    /// Attribute to have at the top of your BaseUnityPlugin class if you want to load a specific R2API Submodule.
    /// Parameter(s) are the nameof the submodules.
    /// e.g: [R2APISubmoduleDependency("SurvivorAPI", "ItemAPI")]
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [Obsolete("All submodules are automatically loaded and this attribute is now unused.", false)]
    public class R2APISubmoduleDependency : Attribute {
        public string?[]? SubmoduleNames { get; }

        public R2APISubmoduleDependency(params string[] submoduleName) {
            SubmoduleNames = submoduleName;
        }
    }
}
