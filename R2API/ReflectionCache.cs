using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace R2API {
    public static class ReflectionCache {
        private static readonly ConcurrentDictionary<(Type T, string name), FieldInfo> fieldCache =
            new ConcurrentDictionary<(Type T, string name), FieldInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), PropertyInfo> propertyCache =
            new ConcurrentDictionary<(Type T, string name), PropertyInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), MethodInfo> methodCache =
            new ConcurrentDictionary<(Type T, string name), MethodInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo>
            overloadedMethodCache =
                new ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo>();

        private static readonly ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo> constructorCache =
            new ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), Type> nestedTypeCache =
            new ConcurrentDictionary<(Type T, string name), Type>();


        private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;


        #region Field

        public static FieldInfo GetFieldCached<T>(string name, BindingFlags bindingFlags = _bindingFlags) =>
            GetFieldCached(typeof(T), name, bindingFlags);

        public static FieldInfo GetFieldCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags) =>
            fieldCache.GetOrAdd((T, name), x => x.T.GetField(x.name, bindingFlags));

        #endregion

        #region Property

        public static PropertyInfo GetPropertyCached<T>(string name, BindingFlags bindingFlags = _bindingFlags) =>
            GetPropertyCached(typeof(T), name, bindingFlags);

        public static PropertyInfo GetPropertyCached(this Type T, string name,
            BindingFlags bindingFlags = _bindingFlags) =>
            propertyCache.GetOrAdd((T, name), x => x.T.GetProperty(x.name, bindingFlags));

        #endregion

        #region Method

        public static MethodInfo GetMethodCached<T>(string name, BindingFlags bindingFlags = _bindingFlags) {
            return GetMethodCached(typeof(T), name, bindingFlags);
        }

        public static MethodInfo GetMethodCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags) {
            return methodCache.GetOrAdd((T, name), x => x.T.GetMethod(x.name, bindingFlags));
        }

        public static MethodInfo GetMethodCached<T>(string name, Type[] argumentTypes,
            BindingFlags bindingFlags = _bindingFlags) =>
            GetMethodCached(typeof(T), name, argumentTypes, bindingFlags);

        public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes,
            BindingFlags bindingFlags = _bindingFlags) =>
            overloadedMethodCache.GetOrAdd((T, name, argumentTypes),
                x => x.T.GetMethod(x.name, bindingFlags, null, argumentTypes, null));

        #endregion

        #region Constructor

        public static ConstructorInfo GetConstructorCached<T>(Type[] argumentTypes) =>
            GetConstructorCached(typeof(T), argumentTypes);

        public static ConstructorInfo GetConstructorCached(this Type T, Type[] argumentTypes) =>
            constructorCache.GetOrAdd((T, argumentTypes),
                x => x.T.GetConstructor(x.arguments));

        #endregion

        #region Nested Types

        public static Type GetNestedTypeCached<T>(string name, BindingFlags bindingFlags = _bindingFlags) =>
            GetNestedTypeCached(typeof(T), name, bindingFlags);

        public static Type GetNestedTypeCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags) =>
            nestedTypeCache.GetOrAdd((T, name),
                x => x.T.GetNestedType(x.name, bindingFlags));

        #endregion
    }
}
