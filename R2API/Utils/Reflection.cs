using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace R2API.Utils {
    public static class Reflection {
        private const BindingFlags DefaultFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        #region Field

        private static readonly ConcurrentDictionary<(Type T, string name), FieldInfo> FieldCache =
            new ConcurrentDictionary<(Type T, string name), FieldInfo>();


        public static FieldInfo GetFieldCached<T>(string name, BindingFlags bindingFlags) =>
            GetFieldCached(typeof(T), name, bindingFlags);

        public static FieldInfo GetFieldCached(this Type T, string name, BindingFlags bindingFlags) {
            if (FieldCache.TryGetValue((T, name), out var val) && val != null)
                return val;

            return FieldCache[(T, name)] = T.GetField(name, bindingFlags);
        }


        public static TReturn GetFieldValue<TReturn>(this object instance, string fieldName) {
            return (TReturn) instance.GetType()
                .GetFieldCached(fieldName, DefaultFlags | BindingFlags.Instance)
                .GetValue(instance);
        }

        public static TReturn GetFieldValue<TReturn>(this Type staticType, string fieldName) {
            return (TReturn) staticType
                .GetFieldCached(fieldName, DefaultFlags | BindingFlags.Static)
                .GetValue(null);
        }

        public static void SetFieldValue(this object instance, string fieldName, object value) {
            instance.GetType()
                .GetFieldCached(fieldName, DefaultFlags | BindingFlags.Instance)
                .SetValue(instance, value);
        }

        public static void SetFieldValue(this Type staticType, string fieldName, object value) {
            staticType
                .GetFieldCached(fieldName, DefaultFlags | BindingFlags.Static)
                .SetValue(null, value);
        }

        #endregion

        #region Property

        private static readonly ConcurrentDictionary<(Type T, string name), PropertyInfo> PropertyCache =
            new ConcurrentDictionary<(Type T, string name), PropertyInfo>();


        public static PropertyInfo GetPropertyCached<T>(string name, BindingFlags bindingFlags) =>
            GetPropertyCached(typeof(T), name, bindingFlags);

        public static PropertyInfo GetPropertyCached(this Type T, string name, BindingFlags bindingFlags) {
            if (PropertyCache.TryGetValue((T, name), out var val) && val != null)
                return val;

            return PropertyCache[(T, name)] = T.GetProperty(name, bindingFlags);
        }


        public static TReturn GetPropertyValue<TReturn>(this object instance, string propName) {
            return (TReturn) instance.GetType()
                .GetPropertyCached(propName, DefaultFlags | BindingFlags.Instance)
                .GetValue(instance);
        }

        public static TReturn GetPropertyValue<TReturn>(this Type staticType, string propName) {
            return (TReturn) staticType
                .GetPropertyCached(propName, DefaultFlags | BindingFlags.Static)
                .GetValue(null);
        }

        public static void SetPropertyValue(this object instance, string propName, object value) {
            instance.GetType()
                .GetPropertyCached(propName, DefaultFlags | BindingFlags.Instance)
                .SetValue(instance, value);
        }

        public static void SetPropertyValue(this Type staticType, string propName, object value) {
            staticType.GetPropertyCached(propName, DefaultFlags | BindingFlags.Static)
                .SetValue(null, value);
        }

        #endregion

        #region Method

        private static readonly ConcurrentDictionary<(Type T, string name), MethodInfo> MethodCache =
            new ConcurrentDictionary<(Type T, string name), MethodInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo> OverloadedMethodCache =
            new ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo>();


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


        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName) =>
            instance.InvokeMethod<TReturn>(methodName, null);

        public static TReturn InvokeMethod<TReturn>(this Type staticType, string methodName) =>
            staticType.InvokeMethod<TReturn>(methodName, null);

        public static void InvokeMethod(this object instance, string methodName) =>
            instance.InvokeMethod<object>(methodName);

        public static void InvokeMethod(this Type staticType, string methodName) =>
            staticType.InvokeMethod<object>(methodName);


        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName, params object[] methodParams) {
            return (TReturn) (methodParams == null
                    ? instance.GetType()
                        .GetMethodCached(methodName, DefaultFlags | BindingFlags.Instance)
                    : instance.GetType()
                        .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray(),
                            DefaultFlags | BindingFlags.Instance)
                )
                .Invoke(instance, methodParams);
        }

        public static TReturn InvokeMethod<TReturn>(this Type staticType, string methodName, params object[] methodParams) {
            return (TReturn) (methodParams == null
                    ? staticType
                        .GetMethodCached(methodName, DefaultFlags | BindingFlags.Instance)
                    : staticType
                        .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray(),
                            DefaultFlags | BindingFlags.Instance)
                )
                .Invoke(null, methodParams);
        }

        public static void InvokeMethod(this object instance, string methodName, params object[] methodParams) =>
            instance.InvokeMethod<object>(methodName, methodParams);

        public static void InvokeMethod(this Type staticType, string methodName, params object[] methodParams) =>
            staticType.InvokeMethod<object>(methodName, methodParams);

        #endregion

        #region Class

        private static readonly ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo> ConstructorCache =
            new ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), Type> NestedTypeCache =
            new ConcurrentDictionary<(Type T, string name), Type>();


        public static ConstructorInfo GetConstructorCached<T>(Type[] argumentTypes) =>
            GetConstructorCached(typeof(T), argumentTypes);

        public static ConstructorInfo GetConstructorCached(this Type T, Type[] argumentTypes) {
            if (ConstructorCache.TryGetValue((T, argumentTypes), out var val) && val != null)
                return val;

            return ConstructorCache[(T, argumentTypes)] = T.GetConstructor(argumentTypes);
        }

        public static Type GetNestedTypeCached<T>(string name, BindingFlags bindingFlags) =>
            GetNestedTypeCached(typeof(T), name, bindingFlags);

        public static Type GetNestedTypeCached(this Type T, string name, BindingFlags bindingFlags) {
            if (NestedTypeCache.TryGetValue((T, name), out var val) && val != null)
                return val;

            return NestedTypeCache[(T, name)] = T.GetNestedType(name, bindingFlags);
        }


        public static Type GetNestedType<TParent>(string name) {
            return typeof(TParent).GetNestedTypeCached(name, BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static object Instantiate(this Type type) {
            return Activator.CreateInstance(type, true);
        }

        public static object Instantiate(this Type type, params object[] constructorArguments) {
            return type.GetConstructorCached(constructorArguments.Select(x => x.GetType()).ToArray())
                .Invoke(constructorArguments);
        }

        public static object InstantiateGeneric<TClass>(this Type typeArgument) {
            return typeof(TClass).MakeGenericType(typeArgument).Instantiate();
        }

        public static object InstantiateGeneric<TClass>(this Type[] typeArgument) {
            return typeof(TClass).MakeGenericType(typeArgument).Instantiate();
        }

        public static IList InstantiateList(this Type type) {
            return (IList) typeof(List<>).MakeGenericType(type).Instantiate();
        }

        #endregion
    }
}
