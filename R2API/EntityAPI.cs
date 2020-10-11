// Nullable context not needed for deprecated APIs

#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
using EntityStates;
using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using R2API.Utils;

namespace R2API {
    [Obsolete("Please use LoadoutAPI instead")]
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class EntityAPI {
        #region Detours

        // ReSharper disable InconsistentNaming
        private static readonly Hook _detourSet_stateType = new Hook(
            typeof(SerializableEntityStateType).GetMethodCached("set_stateType"),
            typeof(EntityAPI).GetMethodCached(nameof(Set_stateType_Hook))
        );

        private static readonly Hook _detourSet_typeName = new Hook(
            typeof(SerializableEntityStateType).GetMethodCached("set_typeName"),
            typeof(EntityAPI).GetMethodCached(nameof(Set_typeName_Hook))
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

        internal static void Set_stateType_Hook(ref SerializableEntityStateType self, Type value) =>
            self.SetStructFieldValue("_typeName",
                value != null && value.IsSubclassOf(typeof(EntityState))
                    ? value.AssemblyQualifiedName
                    : "");


        internal static void Set_typeName_Hook(ref SerializableEntityStateType self, string value) =>
            Set_stateType_Hook(ref self, Type.GetType(value) ?? GetTypeAllAssemblies(value));


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
#pragma warning restore CS8605 // Unboxing a possibly null value.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
