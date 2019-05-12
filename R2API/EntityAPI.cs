using EntityStates;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using R2API.Utils;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    public static class EntityAPI {
        public static void InitHooks() {
            //var detour = new Hook(
            //    typeof(SerializableEntityStateType).GetMethodCached("set_stateType",
            //        BindingFlags.Public | BindingFlags.Instance),
            //    typeof(EntityAPI).GetMethodCached(nameof(set_stateType_Hook),
            //        BindingFlags.Public | BindingFlags.Static)
            //);

            //detour.Apply();
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void set_stateType_Hook(ref SerializableEntityStateType self, Type value) {
            var typeName = typeof(SerializableEntityStateType).GetFieldCached("_typeName");
            typeName.SetValue(self,
                value != null && value.IsSubclassOf(typeof(EntityState)) ? value.AssemblyQualifiedName : "");
        }
    }
}
