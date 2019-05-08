using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace R2API {
    public static class ReflectionCache {
        private static readonly ConcurrentDictionary<(Type T, string name), FieldInfo> FieldCache =
            new ConcurrentDictionary<(Type T, string name), FieldInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), PropertyInfo> PropertyCache =
            new ConcurrentDictionary<(Type T, string name), PropertyInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), MethodInfo> MethodCache =
            new ConcurrentDictionary<(Type T, string name), MethodInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo> OverloadedMethodCache =
            new ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo>();

        private static readonly ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo> ConstructorCache =
            new ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), Type> NestedTypeCache =
            new ConcurrentDictionary<(Type T, string name), Type>();


        #region Field

        public static FieldInfo GetFieldCached<T>(string name, BindingFlags bindingFlags) =>
            GetFieldCached(typeof(T), name, bindingFlags);

        public static FieldInfo GetFieldCached(this Type T, string name, BindingFlags bindingFlags) {
            if (FieldCache.TryGetValue((T, name), out var val) && val != null)
                return val;

            return FieldCache[(T, name)] = T.GetField(name, bindingFlags);
        }

        #endregion

        #region Property

        public static PropertyInfo GetPropertyCached<T>(string name, BindingFlags bindingFlags) =>
            GetPropertyCached(typeof(T), name, bindingFlags);

        public static PropertyInfo GetPropertyCached(this Type T, string name, BindingFlags bindingFlags) {
            if (PropertyCache.TryGetValue((T, name), out var val) && val != null)
                return val;

            return PropertyCache[(T, name)] = T.GetProperty(name, bindingFlags);
        }

        #endregion

        #region Method

        public static MethodInfo GetMethodCached<T>(string name, BindingFlags bindingFlags) =>
            GetMethodCached(typeof(T), name, bindingFlags);

        public static MethodInfo GetMethodCached(this Type T, string name, BindingFlags bindingFlags) {
            if (MethodCache.TryGetValue((T, name), out var val) && val != null)
                return val;

            return MethodCache[(T, name)] = T.GetMethod(name, bindingFlags);
        }

        public static MethodInfo GetMethodCached<T>(string name, Type[] argumentTypes, BindingFlags bindingFlags) =>
            GetMethodCached(typeof(T), name, argumentTypes, bindingFlags);

        public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes, BindingFlags bindingFlags) {
            if (OverloadedMethodCache.TryGetValue((T, name, argumentTypes), out var val) && val != null)
                return val;

            return OverloadedMethodCache[(T, name, argumentTypes)] =
                T.GetMethod(name, bindingFlags, null, argumentTypes, null);
        }

        #endregion

        #region Constructor

        public static ConstructorInfo GetConstructorCached<T>(Type[] argumentTypes) =>
            GetConstructorCached(typeof(T), argumentTypes);

        public static ConstructorInfo GetConstructorCached(this Type T, Type[] argumentTypes) {
            if (ConstructorCache.TryGetValue((T, argumentTypes), out var val) && val != null)
                return val;

            return ConstructorCache[(T, argumentTypes)] = T.GetConstructor(argumentTypes);
        }

        #endregion

        #region Nested Types

        public static Type GetNestedTypeCached<T>(string name, BindingFlags bindingFlags) =>
            GetNestedTypeCached(typeof(T), name, bindingFlags);

        public static Type GetNestedTypeCached(this Type T, string name, BindingFlags bindingFlags) {
            if (NestedTypeCache.TryGetValue((T, name), out var val) && val != null)
                return val;

            return NestedTypeCache[(T, name)] = T.GetNestedType(name, bindingFlags);
        }

        #endregion
    }
}
