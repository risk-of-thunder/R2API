using MonoMod;
using Mono.Cecil;
using System;

namespace RoR2 {
    [MonoModCustomMethodAttribute("NoInlining")]
    public class NoInlining : Attribute { }
}

namespace MonoMod {
    internal static class MonoModRules {
        // ReSharper disable once UnusedParameter.Global
        public static void NoInlining(MethodDefinition method, CustomAttribute _) => method.NoInlining = true;
    }
}
