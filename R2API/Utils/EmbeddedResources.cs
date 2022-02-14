using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace R2API.Utils {
    public static unsafe class EmbeddedResources {
        // https://github.com/Unity-Technologies/mono/blob/unity-main/mcs/class/corlib/System.Reflection/RuntimeAssembly.cs#L457
        private static readonly delegate*<Assembly, string, out int, out Module, nint> GetManifestResourceInternal =
            (delegate*<Assembly, string, out int, out Module, nint>)
            typeof(R2API).Assembly.GetType().GetMethod("GetManifestResourceInternal", (BindingFlags)(-1)).MethodHandle.GetFunctionPointer();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static (nint ptr, int size) GetEmbeddedResource(string resourceName, Assembly? owningAssembly = null) {
            owningAssembly ??= Assembly.GetCallingAssembly();

            return (GetManifestResourceInternal(owningAssembly, resourceName, out int size, out _), size);
        }
    }
}
