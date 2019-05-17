using EntityStates;
using System;
using MonoMod.RuntimeDetour;
using R2API.Utils;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    public static class EntityAPI {
        internal static void InitHooks() {
            var detour = new Hook(
                typeof(SerializableEntityStateType).GetMethodCached("set_stateType"),
                typeof(EntityAPI).GetMethodCached(nameof(set_stateType_Hook))
            );

            detour.Apply();
        }

        internal static void set_stateType_Hook(ref SerializableEntityStateType self, Type value) {
            self.SetStructFieldValue("_typeName",
                value != null && value.IsSubclassOf(typeof(EntityState))
                    ? value.AssemblyQualifiedName
                    : "");
        }
    }
}
