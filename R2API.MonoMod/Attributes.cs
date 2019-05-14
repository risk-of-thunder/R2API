using MonoMod;
using Mono.Cecil;
using System;

namespace RoR2 {
    [MonoModCustomMethodAttribute("NoInlining")]
    public class NoInlining : Attribute {
    }
}

namespace MonoMod {
    internal static class MonoModRules {
        public static void NoInlining(MethodDefinition method, CustomAttribute attrib) => method.NoInlining = true;
    }
}
