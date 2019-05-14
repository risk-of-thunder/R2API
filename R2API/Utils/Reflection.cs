using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace R2API.Utils {
    public static class Reflection {

        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private delegate object GetDelegate(object instance);

        private delegate void SetDelegate(object instance, object value);


        #region Caches

        // Field
        private static readonly ConcurrentDictionary<(Type T, string name), FieldInfo> FieldCache =
            new ConcurrentDictionary<(Type T, string name), FieldInfo>();

        private static readonly ConcurrentDictionary<FieldInfo, GetDelegate> FieldGetDelegateCache =
            new ConcurrentDictionary<FieldInfo, GetDelegate>();

        private static readonly ConcurrentDictionary<FieldInfo, SetDelegate> FieldSetDelegateCache =
            new ConcurrentDictionary<FieldInfo, SetDelegate>();


        // Property
        private static readonly ConcurrentDictionary<(Type T, string name), PropertyInfo> PropertyCache =
            new ConcurrentDictionary<(Type T, string name), PropertyInfo>();

        private static readonly ConcurrentDictionary<PropertyInfo, GetDelegate> PropertyGetDelegateCache =
            new ConcurrentDictionary<PropertyInfo, GetDelegate>();

        private static readonly ConcurrentDictionary<PropertyInfo, SetDelegate> PropertySetDelegateCache =
            new ConcurrentDictionary<PropertyInfo, SetDelegate>();


        // Method
        private static readonly ConcurrentDictionary<(Type T, string name), MethodInfo> MethodCache =
            new ConcurrentDictionary<(Type T, string name), MethodInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo> OverloadedMethodCache =
            new ConcurrentDictionary<(Type T, string name, Type[] arguments), MethodInfo>();

        private static readonly ConcurrentDictionary<MethodInfo, FastReflectionDelegate> DelegateCache =
            new ConcurrentDictionary<MethodInfo, FastReflectionDelegate>();


        // Class
        private static readonly ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo> ConstructorCache =
            new ConcurrentDictionary<(Type T, Type[] arguments), ConstructorInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name), Type> NestedTypeCache =
            new ConcurrentDictionary<(Type T, string name), Type>();


        // Helper methods
        private static TValue GetOrAddOnNull<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key,
            Func<TKey, TValue> factory) {
            if (dict.TryGetValue(key, out var val) && val != null)
                return val;

            return dict[key] = factory(key);
        }

        #endregion


        #region Field

        public static FieldInfo GetFieldCached<T>(string name) =>
            typeof(T).GetFieldCached(name);

        public static FieldInfo GetFieldCached(this Type T, string name) =>
            FieldCache.GetOrAddOnNull((T, name), x => x.T.GetFieldFull(x.name));

        public static TReturn GetFieldValue<TReturn>(this object instance, string fieldName) =>
            (TReturn) instance.GetType()
                .GetFieldCached(fieldName)?
                .GetFieldGetDelegate<TReturn>(true)
                (instance);

        public static TReturn GetFieldValue<TReturn>(this Type staticType, string fieldName) =>
            (TReturn) staticType
                .GetFieldCached(fieldName)?
                .GetFieldGetDelegate<TReturn>(false)
                (null);

        public static void SetFieldValue<TValue>(this object instance, string fieldName, TValue value) =>
            instance.GetType()
                .GetFieldCached(fieldName)?
                .GetFieldSetDelegate(true)
                (instance, value);

        public static void SetFieldValue<TValue>(this Type staticType, string fieldName, TValue value) =>
            staticType
                .GetFieldCached(fieldName)?
                .GetFieldSetDelegate(false)
                (null, value);


        /// <summary>
        /// Gets the <see cref="FieldInfo"/> on the specified <see cref="Type"/> and searches base types if not found.
        /// </summary>
        /// <param name="T">The <see cref="Type"/> to search and get base types from</param>
        /// <param name="name">The name of the property to search for.</param>
        /// <returns></returns>
        private static FieldInfo GetFieldFull(this Type T, string name) {
            while (T != null) {
                var fieldInfo = T.GetField(name, AllFlags);
                if (fieldInfo != null) {
                    return fieldInfo;
                }
                T = T.BaseType;
            }

            return null;
        }

        private static GetDelegate GetFieldGetDelegate<TValue>(this FieldInfo field, bool instance) =>
            FieldGetDelegateCache.GetOrAdd(field, x => x.CreateGetDelegate<TValue>(instance));

        private static SetDelegate GetFieldSetDelegate(this FieldInfo field, bool instance) =>
            FieldSetDelegateCache.GetOrAdd(field, x => x.CreateSetDelegate(instance));

        private static GetDelegate CreateGetDelegate<T>(this FieldInfo field, bool instance) {
            var method = new DynamicMethodDefinition($"{field} Getter", typeof(T), new [] { typeof(object) });
            var il = method.GetILProcessor();

            if (instance) {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
            }
            else {
                il.Emit(OpCodes.Ldsfld, field);
            }

            il.Emit(OpCodes.Ret);

            return (GetDelegate)method.Generate().CreateDelegate(typeof(GetDelegate));
        }

        private static SetDelegate CreateSetDelegate(this FieldInfo field, bool instance) {
            var method = new DynamicMethodDefinition($"{field} Setter", typeof(void), new[] { typeof(object), typeof(object) });
            var il = method.GetILProcessor();

            if (instance) {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
            }
            else {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stsfld, field);
            }

            il.Emit(OpCodes.Ret);

            return (SetDelegate)method.Generate().CreateDelegate(typeof(SetDelegate));
        }

        #endregion

        #region Property

        public static PropertyInfo GetPropertyCached<T>(string name) =>
            typeof(T).GetPropertyCached(name);

        public static PropertyInfo GetPropertyCached(this Type T, string name) =>
            PropertyCache.GetOrAddOnNull((T, name), x => x.T.GetProperty(x.name, AllFlags));

        public static TReturn GetPropertyValue<TReturn>(this object instance, string propName) =>
            (TReturn) instance.GetType()
                .GetPropertyCached(propName)?
                .GetPropertyGetDelegate<TReturn>(true)
                (instance);

        public static TReturn GetPropertyValue<TReturn>(this Type staticType, string propName) =>
            (TReturn) staticType
                .GetPropertyCached(propName)?
                .GetPropertyGetDelegate<TReturn>(false)
                (null);

        public static void SetPropertyValue(this object instance, string propName, object value) =>
            instance.GetType()
                .GetPropertyCached(propName)?
                .GetPropertySetDelegate(true)
                (instance, value);

        public static void SetPropertyValue(this Type staticType, string propName, object value) =>
            staticType.GetPropertyCached(propName)?
                .GetPropertySetDelegate(false)
                (null, value);


        private static GetDelegate GetPropertyGetDelegate<TValue>(this PropertyInfo property, bool instance) =>
            PropertyGetDelegateCache.GetOrAdd(property, prop => prop.CreateGetDelegate<TValue>(instance));

        private static SetDelegate GetPropertySetDelegate(this PropertyInfo property, bool instance) =>
            PropertySetDelegateCache.GetOrAdd(property, prop => prop.CreateSetDelegate(instance));

        private static GetDelegate CreateGetDelegate<TValue>(this PropertyInfo property, bool instance) {
            var method = new DynamicMethodDefinition($"{property} Getter", typeof(TValue), new[] { typeof(object) });
            var il = method.GetILProcessor();

            if (instance) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Call, property.GetGetMethod(true));
            il.Emit(OpCodes.Ret);

            return (GetDelegate)method.Generate().CreateDelegate(typeof(GetDelegate));
        }

        private static SetDelegate CreateSetDelegate(this PropertyInfo property, bool instance) {
            var method = new DynamicMethodDefinition($"{property} Setter", typeof(void), new[] { typeof(object), typeof(object) });
            var il = method.GetILProcessor();

            if (instance) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, property.GetSetMethod(true));
            il.Emit(OpCodes.Ret);

            return (SetDelegate)method.Generate().CreateDelegate(typeof(SetDelegate));
        }

        #endregion

        #region Method

        public static MethodInfo GetMethodCached<T>(string name) =>
            typeof(T).GetMethodCached(name);

        public static MethodInfo GetMethodCached(this Type T, string name) =>
            MethodCache.GetOrAddOnNull((T, name), x => x.T.GetMethod(x.name, AllFlags));

        public static MethodInfo GetMethodCached<T>(string name, Type[] argumentTypes) =>
            typeof(T).GetMethodCached(name, argumentTypes);

        public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes) =>
            OverloadedMethodCache.GetOrAddOnNull((T, name, argumentTypes),
                // TODO: access tuple element 3 by name
                x => x.T.GetMethod(x.name, AllFlags, null, x.Item3, null));

        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName) =>
            instance.InvokeMethod<TReturn>(methodName, null);

        public static TReturn InvokeMethod<TReturn>(this Type staticType, string methodName) =>
            staticType.InvokeMethod<TReturn>(methodName, null);

        public static void InvokeMethod(this object instance, string methodName) =>
            instance.InvokeMethod<object>(methodName);

        public static void InvokeMethod(this Type staticType, string methodName) =>
            staticType.InvokeMethod<object>(methodName);

        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName, params object[] methodParams) =>
            (TReturn) (methodParams == null
                ? instance.GetType()
                    .GetMethodCached(methodName)
                : instance.GetType()
                    .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray())
            )?
            .GetMethodDelegateCached()
            .Invoke(instance, methodParams);

        public static TReturn InvokeMethod<TReturn>(this Type staticType, string methodName, params object[] methodParams) =>
            (TReturn) (methodParams == null
                ? staticType
                    .GetMethodCached(methodName)
                : staticType
                    .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray())
            )?
            .GetMethodDelegateCached()
            .Invoke(null, methodParams);

        public static void InvokeMethod(this object instance, string methodName, params object[] methodParams) =>
            instance.InvokeMethod<object>(methodName, methodParams);

        public static void InvokeMethod(this Type staticType, string methodName, params object[] methodParams) =>
            staticType.InvokeMethod<object>(methodName, methodParams);


        private static FastReflectionDelegate GetMethodDelegateCached(this MethodInfo methodInfo) =>
            // Thanks 0x0 :)
            DelegateCache.GetOrAdd(methodInfo, method => method.CreateFastDelegate());

        #endregion

        #region Class

        public static ConstructorInfo GetConstructorCached<T>(Type[] argumentTypes) =>
            GetConstructorCached(typeof(T), argumentTypes);

        public static ConstructorInfo GetConstructorCached(this Type T, Type[] argumentTypes) =>
            // TODO: access tuple element 2 by name
            ConstructorCache.GetOrAddOnNull((T, argumentTypes), x => x.T.GetConstructor(x.Item2));


        public static Type GetNestedType<T>(string name) =>
            typeof(T).GetNestedTypeCached(name);

        public static Type GetNestedTypeCached<T>(string name) =>
            typeof(T).GetNestedTypeCached(name);

        public static Type GetNestedTypeCached(this Type T, string name) =>
            NestedTypeCache.GetOrAddOnNull((T, name), x => x.T.GetNestedType(x.name, AllFlags));


        public static object Instantiate(this Type type) =>
            Activator.CreateInstance(type, true);

        public static object Instantiate(this Type type, params object[] constructorArguments) =>
            type.GetConstructorCached(constructorArguments.Select(x => x.GetType()).ToArray())
                .Invoke(constructorArguments);

        public static object InstantiateGeneric<TClass>(this Type typeArgument) =>
            typeof(TClass).MakeGenericType(typeArgument).Instantiate();

        public static object InstantiateGeneric<TClass>(this Type[] typeArgument) =>
            typeof(TClass).MakeGenericType(typeArgument).Instantiate();

        public static IList InstantiateList(this Type type) =>
            (IList) typeof(List<>).MakeGenericType(type).Instantiate();

        #endregion
    }
}
