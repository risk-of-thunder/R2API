using MonoMod;
using Mono.Cecil;
using System;

namespace RoR2 {
    [MonoModCustomMethodAttribute("NoInlining")]
    public class NoInlining : Attribute {
    }

    [MonoModPublic]
    public class MakePublic : Attribute { }
}

namespace MonoMod {
    internal static class MonoModRules {
        // ReSharper disable once UnusedParameter.Global
        public static void NoInlining(MethodDefinition method, CustomAttribute attrib) => method.NoInlining = true;
    }
}
