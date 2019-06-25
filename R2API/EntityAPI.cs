using EntityStates;
using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using R2API.Utils;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class EntityAPI {
        #region Detours

        // ReSharper disable InconsistentNaming
        private static readonly Hook _detourSet_stateType = new Hook(
            typeof(SerializableEntityStateType).GetMethodCached("set_stateType"),
            typeof(EntityAPI).GetMethodCached(nameof(set_stateType_Hook))
        );

        private static readonly Hook _detourSet_typeName = new Hook(
            typeof(SerializableEntityStateType).GetMethodCached("set_typeName"),
            typeof(EntityAPI).GetMethodCached(nameof(set_typeName_Hook))
        );
        // ReSharper restore InconsistentNaming

        #endregion


        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            _detourSet_stateType.Apply();
            _detourSet_typeName.Apply();
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            _detourSet_stateType.Undo();
            _detourSet_typeName.Undo();
        }


        internal static void set_stateType_Hook(ref SerializableEntityStateType self, Type value) =>
            self.SetStructFieldValue("_typeName",
                value != null && value.IsSubclassOf(typeof(EntityState))
                    ? value.AssemblyQualifiedName
                    : "");

        internal static void set_typeName_Hook(ref SerializableEntityStateType self, string value) =>
            set_stateType_Hook(ref self, Type.GetType(value) ?? GetTypeAllAssemblies(value));


        private static Type GetTypeAllAssemblies(string name) {
            Type type = null;

            var assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                // Assembly-CSharp will be checked earlier if we order by name
                .OrderBy(asm => asm.FullName);

            foreach (var asm in assemblies) {
                type = asm.GetTypes().FirstOrDefault(t => t.Name == name || t.FullName == name);

                if (type != null)
                    break;
            }

            return type;
        }
    }
}
