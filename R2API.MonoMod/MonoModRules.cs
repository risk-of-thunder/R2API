using MonoMod;
using Mono.Cecil;
using System;
using MonoMod.InlineRT;

namespace RoR2 {
    [MonoModCustomMethodAttribute("NoInlining")]
    public class NoInlining : Attribute { }
}

namespace MonoMod {
    internal static class MonoModRules {
        // ReSharper disable once UnusedParameter.Global
        public static void NoInlining(MethodDefinition method, CustomAttribute _) => method.NoInlining = true;

        static MonoModRules() {
            // Inject this type into the game assembly so that we know that the monomod patch was correctly applied.
            var module = MonoModRule.Modder.Module;
            var wasHereType = new TypeDefinition("R2API", "R2APIMonoModPatchWasHere",
                TypeAttributes.Public | TypeAttributes.Class, module.ImportReference(typeof(Attribute)));
            module.Types.Add(wasHereType);
        }
    }
}
