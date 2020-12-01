using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using UnityEngine;

namespace R2API.Utils {
    public static class Reflection {

        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                              BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private delegate T GetDelegate<out T>(object instance);

        private delegate void SetDelegate<in T>(object instance, T value);

        //private delegate object CallDelegate(object instance, object[] arguments);

        private delegate void SetDelegateRef<TInstance, in TValue>(ref TInstance instance, TValue value) where TInstance : struct;

        private delegate T GetDelegateRef<TInstance, out T>(ref TInstance instance) where TInstance : struct;

        #region Caches

        // Field
        private static readonly ConcurrentDictionary<(Type T, string name), FieldInfo> FieldCache =
            new ConcurrentDictionary<(Type T, string name), FieldInfo>();

        private static readonly ConcurrentDictionary<FieldInfo, Delegate> FieldGetDelegateCache =
            new ConcurrentDictionary<FieldInfo, Delegate>();

        private static readonly ConcurrentDictionary<FieldInfo, Delegate> FieldSetDelegateCache =
            new ConcurrentDictionary<FieldInfo, Delegate>();


        // Property
        private static readonly ConcurrentDictionary<(Type T, string name), PropertyInfo> PropertyCache =
            new ConcurrentDictionary<(Type T, string name), PropertyInfo>();

        private static readonly ConcurrentDictionary<PropertyInfo, Delegate> PropertyGetDelegateCache =
            new ConcurrentDictionary<PropertyInfo, Delegate>();

        private static readonly ConcurrentDictionary<PropertyInfo, Delegate> PropertySetDelegateCache =
            new ConcurrentDictionary<PropertyInfo, Delegate>();


        // Method
        private static readonly ConcurrentDictionary<(Type T, string name), MethodInfo> MethodCache =
            new ConcurrentDictionary<(Type T, string name), MethodInfo>();

        private static readonly ConcurrentDictionary<(Type T, string name, Type[] argumentTypes), MethodInfo>
            OverloadedMethodCache =
                new ConcurrentDictionary<(Type T, string name, Type[] argumentTypes), MethodInfo>();

        private static readonly ConcurrentDictionary<MethodInfo, FastReflectionDelegate> DelegateCache =
            new ConcurrentDictionary<MethodInfo, FastReflectionDelegate>();


        // Class
        private static readonly ConcurrentDictionary<(Type T, Type[] argumentTypes), ConstructorInfo> ConstructorCache =
            new ConcurrentDictionary<(Type T, Type[] argumentTypes), ConstructorInfo>();

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

        /// <summary>
        /// Gets the <see cref="FieldInfo"/> of the type by name and caches it
        /// </summary>
        /// <typeparam name="T">The type to search</typeparam>
        /// <param name="name">The name of the field to find</param>
        /// <returns></returns>
        public static FieldInfo GetFieldCached<T>(string? name) =>
            typeof(T).GetFieldCached(name);

        /// <summary>
        /// Gets the <see cref="FieldInfo" /> of the type by name and caches it
        /// </summary>
        /// <param name="T">The type to search</param>
        /// <param name="name">The name of the field to find</param>
        /// <returns></returns>
        public static FieldInfo GetFieldCached(this Type? T, string? name) =>
            FieldCache.GetOrAddOnNull((T, name), x => x.T.GetFieldFull(x.name)
                ?? throw new Exception($"Could not find {nameof(FieldInfo)} on {T.FullName} with the name {name}"));


        /// <summary>
        /// Gets the value of the field on the object
        /// </summary>
        /// <typeparam name="TReturn">The type of the return value</typeparam>
        /// <param name="fieldName">The name of the field to get the value of</param>
        /// <param name="instance">The object to get the field's value from</param>
        /// <returns></returns>
        public static TReturn GetFieldValue<TReturn>(this object? instance, string? fieldName) =>
            instance.GetType()
                .GetFieldCached(fieldName)
                .GetFieldGetDelegate<TReturn>()
                (instance);

        /// <summary>
        /// Gets the value of the specified static field on the specified static type
        /// </summary>
        /// <typeparam name="TReturn">The return type</typeparam>
        /// <param name="staticType">The name of the static field to get the value of</param>
        /// <param name="fieldName">The type to get the specified static field's value on</param>
        /// <returns></returns>
        public static TReturn GetFieldValue<TReturn>(this Type? staticType, string? fieldName) =>
            staticType
                .GetFieldCached(fieldName)
                .GetFieldGetDelegate<TReturn>()
                (null);

        /// <summary>
        /// Sets the value of the specified field on the specified object; if the object is a struct use
        /// <see cref="SetStructFieldValue{TInstance, TValue}(ref TInstance, string, TValue)"/> instead
        /// </summary>
        /// <typeparam name="TValue">The type of the value to set</typeparam>
        /// <param name="instance">The name of the field to set the value of</param>
        /// <param name="fieldName">The type to set the specified field's value on</param>
        /// <param name="value">The value to set</param>
        /// <returns></returns>
        public static void SetFieldValue<TValue>(this object? instance, string? fieldName, TValue value) =>
            instance.GetType()
                .GetFieldCached(fieldName)
                .GetFieldSetDelegate<TValue>()
                (instance, value);

        /// <summary>
        /// Sets the value of the specified static field on the specified static type
        /// </summary>
        /// <typeparam name="TValue">The type of the value to set</typeparam>
        /// <param name="staticType">The name of the static field to set the value of</param>
        /// <param name="fieldName">The type to set the specified static field's value on</param>
        /// <param name="value">The value to set</param>
        /// <returns></returns>
        public static void SetFieldValue<TValue>(this Type? staticType, string? fieldName, TValue value) =>
            staticType
                .GetFieldCached(fieldName)
                .GetFieldSetDelegate<TValue>()
                (null, value);

        /// <summary>
        /// Sets the value of the specified field on the specified struct
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance of the struct</typeparam>
        /// <typeparam name="TValue">The type of the value to set</typeparam>
        /// <param name="instance">The name of the field to set the value of</param>
        /// <param name="fieldName">The type to set the specified field's value on</param>
        /// <param name="value">The value to set the field to</param>
        /// <returns></returns>
        public static void SetStructFieldValue<TInstance, TValue>(this ref TInstance instance, string? fieldName, TValue value)
            where TInstance : struct =>
            typeof(TInstance)
                .GetFieldCached(fieldName)
                .GetFieldSetDelegateRef<TInstance, TValue>()
                (ref instance, value);


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

        private static T GetMemberFull<T>(this Type type, string name) where T : MemberInfo {
            while (type != null) {
                var fieldInfo = type.GetMember(name, AllFlags);
                if (fieldInfo != null) {
                    return (T)fieldInfo.First();
                }

                type = type.BaseType;
            }

            return null;
        }

        private static GetDelegate<TReturn> GetFieldGetDelegate<TReturn>(this FieldInfo field) =>
            FieldGetDelegateCache.GetOrAdd(field, x => x.CreateGetDelegate<TReturn>())
                .CastDelegate<GetDelegate<TReturn>>();

        private static SetDelegate<TValue> GetFieldSetDelegate<TValue>(this FieldInfo field) =>
            FieldSetDelegateCache.GetOrAdd(field, x => x.CreateSetDelegate<TValue>())
                .CastDelegate<SetDelegate<TValue>>();

        private static SetDelegateRef<TInstance, TValue> GetFieldSetDelegateRef<TInstance, TValue>(this FieldInfo field) where TInstance : struct =>
            FieldSetDelegateCache.GetOrAdd(field, x => x.CreateSetDelegateRef<TInstance, TValue>())
                .CastDelegate<SetDelegateRef<TInstance, TValue>>();

        #endregion

        #region Property

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/> of the type by name
        /// </summary>
        /// <typeparam name="T">The type on which to find the property</typeparam>
        /// <param name="name">The name of the property to get</param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyCached<T>(string? name) =>
            typeof(T).GetPropertyCached(name);

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/> of the type by name
        /// </summary>
        /// <param name="T">The type to get the <see cref="PropertyInfo"/> from</param>
        /// <param name="name">The name of the property to get</param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyCached(this Type? T, string? name) =>
            PropertyCache.GetOrAddOnNull((T, name), x => x.T.GetProperty(x.name, AllFlags));

        /// <summary>
        /// Gets the value of the property on the specified object; if the object is a struct use
        /// <see cref="GetStructPropertyValue{TInstance, TValue}(ref TInstance, string)"></see> instead
        /// </summary>
        /// <typeparam name="TReturn">The type of the return value</typeparam>
        /// <param name="instance">The object to get the property's value from</param>
        /// <param name="propName">The name of the field to get the value of</param>
        /// <returns></returns>
        public static TReturn GetPropertyValue<TReturn>(this object? instance, string? propName) =>
            instance.GetType()
                .GetPropertyCached(propName)
                .GetPropertyGetDelegate<TReturn>()
                (instance);

        /// <summary>
        /// Gets the value of the static property on the specified static type
        /// </summary>
        /// <typeparam name="TReturn">The return type</typeparam>
        /// <param name="staticType">The name of the static field to get the value of</param>
        /// <param name="propName">The type to get the specified static property's value on</param>
        /// <returns></returns>
        public static TReturn GetPropertyValue<TReturn>(this Type? staticType, string? propName) =>
            staticType
                .GetPropertyCached(propName)
                .GetPropertyGetDelegate<TReturn>()
                (null);

        /// <summary>
        /// Sets the value of the property on the specified class; if you're setting the property on a
        /// struct use <see cref="SetStructPropertyValue{TInstance, TValue}(ref TInstance, string?, TValue)"/> instead
        /// </summary>
        /// <typeparam name="TValue">The type of the value to set</typeparam>
        /// <param name="instance">The name of the field to set the value of</param>
        /// <param name="propName">The type to set the specified property's value on</param>
        /// <param name="value">The value to set</param>
        /// <returns></returns>
        public static void SetPropertyValue<TValue>(this object? instance, string? propName, TValue value) =>
            instance.GetType()
                .GetPropertyCached(propName)?
                .GetPropertySetDelegate<TValue>()
                (instance, value);

        /// <summary>
        /// Sets the value of the static property on the specified static class
        /// </summary>
        /// <typeparam name="TValue">The type of the value to set</typeparam>
        /// <param name="staticType">The name of the static field to set the value of</param>
        /// <param name="propName">The type to set the specified static property's value on</param>
        /// <param name="value">The value to set the property to</param>
        /// <returns></returns>
        public static void SetPropertyValue<TValue>(this Type? staticType, string? propName, TValue value) =>
            staticType.GetPropertyCached(propName)?
                .GetPropertySetDelegate<TValue>()
                (null, value);

        /// <summary>
        /// Sets the value of the specified property on the specified struct
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance of the struct</typeparam>
        /// <typeparam name="TValue">The type of the value to set</typeparam>
        /// <param name="instance">The name of the field to set the value of</param>
        /// <param name="propName">The type to set the specified property's value on</param>
        /// <param name="value">The value to set the property to</param>
        /// <returns></returns>
        public static void SetStructPropertyValue<TInstance, TValue>(this ref TInstance instance, string? propName,
            TValue value)
            where TInstance : struct =>
            typeof(TInstance)
                .GetPropertyCached(propName)
                .GetPropertySetDelegateRef<TInstance, TValue>()
                (ref instance, value);

        /// <summary>
        /// Gets the value of the specified property on the specified struct
        /// </summary>
        /// <typeparam name="TInstance">The type of the struct</typeparam>
        /// <typeparam name="TValue">The type of the value to set</typeparam>
        /// <param name="instance">The name of the field to set the value of</param>
        /// <param name="propName">The type to set the specified property's value on</param>
        /// <returns></returns>
        public static TValue GetStructPropertyValue<TInstance, TValue>(this ref TInstance instance, string? propName)
            where TInstance : struct =>
            typeof(TInstance)
                .GetPropertyCached(propName)
                .GetPropertyGetDelegateRef<TInstance, TValue>()
                (ref instance);

        private static GetDelegate<TReturn> GetPropertyGetDelegate<TReturn>(this PropertyInfo property) =>
            PropertyGetDelegateCache.GetOrAdd(property, prop => prop.CreateGetDelegate<TReturn>())
                .CastDelegate<GetDelegate<TReturn>>();

        private static GetDelegateRef<TInstance, TReturn> GetPropertyGetDelegateRef<TInstance, TReturn>(this PropertyInfo property)
            where TInstance : struct =>
            PropertyGetDelegateCache.GetOrAdd(property, prop => prop.CreateGetDelegate<TInstance, TReturn>())
                .CastDelegate<GetDelegateRef<TInstance, TReturn>>();

        private static SetDelegate<TValue> GetPropertySetDelegate<TValue>(this PropertyInfo property) =>
            PropertySetDelegateCache.GetOrAdd(property, prop => prop.CreateSetDelegate<TValue>())
                .CastDelegate<SetDelegate<TValue>>();

        private static SetDelegateRef<TInstance, TValue> GetPropertySetDelegateRef<TInstance, TValue>(
            this PropertyInfo property)
            where TInstance : struct =>
            PropertySetDelegateCache.GetOrAdd(property, prop => prop.CreateSetDelegateRef<TInstance, TValue>())
                .CastDelegate<SetDelegateRef<TInstance, TValue>>();

        #endregion

        #region Method

        /// <summary>
        /// Gets the method on the specified type and caches it
        /// </summary>
        /// <typeparam name="T">The type to search</typeparam>
        /// <param name="name">The name of the method to find</param>
        /// <returns></returns>
        public static MethodInfo GetMethodCached<T>(string? name) =>
            typeof(T).GetMethodCached(name);

        /// <summary>
        /// Gets the method on the specified static type and caches it
        /// </summary>
        /// <param name="T">The type to search</param>
        /// <param name="name">The name of the method to find</param>
        /// <returns>The found <see cref="MethodInfo"/></returns>
        public static MethodInfo GetMethodCached(this Type? T, string? name) =>
            MethodCache.GetOrAddOnNull((T, name), x => x.T.GetMethod(x.name, AllFlags)
                ?? throw new Exception($"Could not find {nameof(MethodInfo)} on {T.FullName} with the name {name}"));

        /// <summary>
        /// Gets the generic method of the specified type with the specified generic type definition parameter
        /// </summary>
        /// <param name="T">The type to search</param>
        /// <param name="name">The name of the method to find</param>
        /// <param name="genericTypeDefinition">The generic type definition parameter</param>
        /// <returns>The found <see cref="MethodInfo"/></returns>
        public static MethodInfo GetMethodWithConstructedGenericParameter(this Type? T, string? name, Type? genericTypeDefinition) {
            return T.GetMethods().First(method => {
                if (method.Name != name) {
                    return false;
                }

                var parameterType = method.GetParameters().First().ParameterType;
                if (!parameterType.IsConstructedGenericType) {
                    return false;
                }

                var t = parameterType.GetGenericArguments().First();
                return parameterType == genericTypeDefinition.MakeGenericType(t);
            });
        }

        /// <summary>
        /// Gets the method on the specified type and caches it. This overload is used when the method is ambiguous
        /// </summary>
        /// <typeparam name="T">The type to search</typeparam>
        /// <param name="name">The name of the method to find</param>
        /// <param name="argumentTypes">The types of the argument</param>
        /// <returns></returns>
        public static MethodInfo GetMethodCached<T>(string? name, Type?[]? argumentTypes) =>
            typeof(T).GetMethodCached(name, argumentTypes);

        /// <summary>
        /// Gets the method on the specified static type and caches it. This overload is used when the method is ambiguous
        /// </summary>
        /// <param name="T">The type to search</param>
        /// <param name="name">The name of the method to find</param>
        /// <param name="argumentTypes">The types of the argument</param>
        public static MethodInfo GetMethodCached(this Type? T, string? name, Type?[]? argumentTypes) =>
            OverloadedMethodCache.GetOrAddOnNull((T, name, argumentTypes),
                x => x.T.GetMethod(x.name, AllFlags, null, x.argumentTypes, null)
                     ?? throw new Exception($"Could not find {nameof(MethodInfo)} on {T.FullName} with the name {name} and arguments: " +
                                            $"{string.Join(",", argumentTypes.Select(a => a.FullName))}"));

        /// <summary>
        /// Invoke a method on the specified object by name
        /// </summary>
        /// <typeparam name="TReturn">The return type of the method</typeparam>
        /// <param name="instance">The object to invoke the method on</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <returns></returns>
        public static TReturn InvokeMethod<TReturn>(this object? instance, string? methodName) =>
            instance.InvokeMethod<TReturn>(methodName, null);

        /// <summary>
        /// Invoke a static method on the specified type by name
        /// </summary>
        /// <typeparam name="TReturn">The return type of the method</typeparam>
        /// <param name="staticType">The static type to search</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <returns></returns>
        public static TReturn InvokeMethod<TReturn>(this Type? staticType, string? methodName) =>
            staticType.InvokeMethod<TReturn>(methodName, null);

        /// <summary>
        /// Invoke a method on the specified object by name
        /// </summary>
        /// <param name="instance">The object to invoke the method on</param>
        /// <param name="methodName">The name of the method to invoke</param>
        public static void InvokeMethod(this object? instance, string? methodName) =>
            instance.InvokeMethod<object>(methodName);

        /// <summary>
        /// Invoke a static method on the specified type by name
        /// </summary>
        /// <param name="staticType">The static type to search</param>
        /// <param name="methodName">The name of the method to invoke</param>
        public static void InvokeMethod(this Type? staticType, string? methodName) =>
            staticType.InvokeMethod<object>(methodName);

        /// <summary>
        /// Invoke a method on the specified object by name with parameters
        /// </summary>
        /// <typeparam name="TReturn">The return type of the method</typeparam>
        /// <param name="instance">The object to invoke the method on</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="methodParams"></param>
        /// <returns></returns>
        public static TReturn InvokeMethod<TReturn>(this object? instance, string? methodName,
            params object[] methodParams) {
            var methodInfo = (methodParams == null
                    ? instance.GetType()
                        .GetMethodCached(methodName)
                    : instance.GetType()
                        .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray())
               ) ?? throw new Exception($"Could not find method on type {instance.GetType()} with the name of {methodName} with the arguments specified.");

            return (TReturn)methodInfo.GetMethodDelegateCached()(instance, methodParams);
        }

        /// <summary>
        /// Invoke a static method on the specified type by name with parameters
        /// </summary>
        /// <typeparam name="TReturn">The return type of the method</typeparam>
        /// <param name="staticType">The static type to search</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="methodParams">The method parameters</param>
        /// <returns></returns>
        public static TReturn InvokeMethod<TReturn>(this Type? staticType, string? methodName,
            params object[] methodParams) {
            var methodInfo = (methodParams == null
                ? staticType
                    .GetMethodCached(methodName)
                : staticType
                    .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray()))
                ?? throw new Exception($"Could not find method on type {staticType} with the name of {methodName} with the arguments specified.");

            return (TReturn)methodInfo.GetMethodDelegateCached()(null, methodParams);
        }

        /// <summary>
        /// Invoke a method on the specified object by name with parameters
        /// </summary>
        /// <param name="instance">The object to invoke the method on</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="methodParams"></param>
        public static void InvokeMethod(this object? instance, string? methodName, params object?[]? methodParams) =>
            instance.InvokeMethod<object>(methodName, methodParams);

        /// <summary>
        /// Invoke a static method on the specified type by name with parameters
        /// </summary>
        /// <param name="staticType">The static type to search</param>
        /// <param name="methodName">The name of the method to invoke</param>
        /// <param name="methodParams"></param>
        public static void InvokeMethod(this Type? staticType, string? methodName, params object?[]? methodParams) =>
            staticType.InvokeMethod<object>(methodName, methodParams);


        private static FastReflectionDelegate GetMethodDelegateCached(this MethodInfo methodInfo) =>
            DelegateCache.GetOrAdd(methodInfo, method => method.GenerateCallDelegate());

        #endregion

        #region Class

        /// <summary>
        /// Gets the constructor on the specified type with specified arguments and caches it
        /// </summary>
        /// <typeparam name="T">The type to search</typeparam>
        /// <param name="argumentTypes">The types of the arguments on the constructor to find</param>
        /// <returns></returns>
        public static ConstructorInfo GetConstructorCached<T>(Type?[]? argumentTypes) =>
            GetConstructorCached(typeof(T), argumentTypes);

        /// <summary>
        /// Gets the constructor on the specified static type with specified arguments and caches it
        /// </summary>
        /// <param name="T">The type to search</param>
        /// <param name="argumentTypes">The types of the arguments on the constructor to find</param>
        /// <returns></returns>
        public static ConstructorInfo GetConstructorCached(this Type? T, Type?[]? argumentTypes) =>
            ConstructorCache.GetOrAddOnNull((T, argumentTypes), x => x.T.GetConstructor(x.argumentTypes)
                ?? throw new Exception($"Could not find {nameof(ConstructorInfo)} on {T.FullName} with the arguments {string.Join(",", argumentTypes.Select(a => a.FullName))}"));

        /// <summary>
        /// Gets the nested type on the specified type
        /// </summary>
        /// <typeparam name="T">The type to search</typeparam>
        /// <param name="name">The name of the nested type to find</param>
        /// <returns></returns>
        public static Type GetNestedType<T>(string? name) =>
            typeof(T).GetNestedTypeCached(name);

        /// <summary>
        /// Gets the nested type on the specified type
        /// </summary>
        /// <typeparam name="T">The type to search</typeparam>
        /// <param name="name">The name of the nested type to find</param>
        /// <returns></returns>
        public static Type GetNestedTypeCached<T>(string? name) =>
            typeof(T).GetNestedTypeCached(name);

        /// <summary>
        /// Gets the nested type on the specified static type
        /// </summary>
        /// <param name="T">The static type to search</param>
        /// <param name="name">The name of the nested type to find</param>
        /// <returns></returns>
        public static Type GetNestedTypeCached(this Type? T, string? name) =>
            NestedTypeCache.GetOrAddOnNull((T, name), x => x.T.GetNestedType(x.name, AllFlags)
                ?? throw new Exception($"Could not find nested {nameof(Type)} on {T.FullName} with the name {name}"));

        /// <summary>
        /// Instatiates the specified type
        /// </summary>
        /// <param name="type">The type to instantiate</param>
        /// <returns></returns>
        public static object Instantiate(this Type? type) =>
            Activator.CreateInstance(type, true);

        /// <summary>
        /// Instatiates the specified type with specified constructor arguments
        /// </summary>
        /// <param name="type">The type to instantiate</param>
        /// <param name="constructorArguments">The constructor arguments</param>
        /// <returns></returns>
        public static object Instantiate(this Type? type, params object?[]? constructorArguments) =>
            type.GetConstructorCached(constructorArguments.Select(x => x.GetType()).ToArray())
                .Invoke(constructorArguments);

        /// <summary>
        /// Instantiates the specified generic type
        /// </summary>
        /// <typeparam name="TClass">The type to instantiate</typeparam>
        /// <param name="typeArgument">The type of the generic type argument</param>
        /// <returns></returns>
        public static object InstantiateGeneric<TClass>(this Type? typeArgument) =>
            typeof(TClass).MakeGenericType(typeArgument).Instantiate();

        /// <summary>
        /// Instantiates the specified generic types
        /// </summary>
        /// <typeparam name="TClass">The type to instantiate</typeparam>
        /// <param name="typeArgument">The types of the generic type arguments</param>
        /// <returns></returns>
        public static object InstantiateGeneric<TClass>(this Type?[]? typeArgument) =>
            typeof(TClass).MakeGenericType(typeArgument).Instantiate();

        /// <summary>
        /// Instantiates a list of the specified generic type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IList InstantiateList(this Type? type) =>
            (IList)typeof(List<>).MakeGenericType(type).Instantiate();

        #endregion

        #region Fast Reflection

        private static GetDelegate<TReturn> CreateGetDelegate<TReturn>(this FieldInfo field) {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            if (!typeof(TReturn).IsAssignableFrom(field.FieldType)) {
                throw new Exception($"Field type {field.FieldType} does not match the requested type {typeof(TReturn)}.");
            }


            using (var method = new DynamicMethodDefinition($"{field} Getter", typeof(TReturn), new[] { typeof(object) })) {
                var il = method.GetILProcessor();

                if (!field.IsStatic) {
                    il.Emit(OpCodes.Ldarg_0);
                    if (field.DeclaringType.GetTypeInfo().IsValueType) {
                        il.Emit(OpCodes.Unbox_Any, field.DeclaringType);
                    }
                }

                il.Emit(!field.IsStatic ? OpCodes.Ldfld : OpCodes.Ldsfld, field);
                il.Emit(OpCodes.Ret);

                return (GetDelegate<TReturn>)method.Generate().CreateDelegate(typeof(GetDelegate<TReturn>));
            }
        }

        private static SetDelegate<TValue> CreateSetDelegate<TValue>(this FieldInfo field) {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            if (!field.FieldType.IsAssignableFrom(typeof(TValue))) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {field.FieldType}.");
            }

            using (var method = new DynamicMethodDefinition($"{field} Setter", typeof(void),
                new[] { typeof(object), typeof(TValue) })) {
                var il = method.GetILProcessor();

                if (!field.IsStatic) {
                    il.Emit(OpCodes.Ldarg_0);
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(!field.IsStatic ? OpCodes.Stfld : OpCodes.Stsfld, field);

                il.Emit(OpCodes.Ret);

                return (SetDelegate<TValue>)method.Generate().CreateDelegate(typeof(SetDelegate<TValue>));
            }
        }

        private static SetDelegateRef<TInstance, TValue> CreateSetDelegateRef<TInstance, TValue>(this FieldInfo field) where TInstance : struct {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            if (field.FieldType != typeof(TValue)) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {field.FieldType}.");
            }

            using (var method = new DynamicMethodDefinition($"{field} SetterByRef", typeof(void),
                new[] { typeof(TInstance).MakeByRefType(), typeof(TValue) })) {
                var il = method.GetILProcessor();

                if (!field.IsStatic) {
                    il.Emit(OpCodes.Ldarg_0);
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(!field.IsStatic ? OpCodes.Stfld : OpCodes.Stsfld, field);
                il.Emit(OpCodes.Ret);

                return (SetDelegateRef<TInstance, TValue>)method.Generate().CreateDelegate(typeof(SetDelegateRef<TInstance, TValue>));
            }
        }

        private static GetDelegate<TReturn> CreateGetDelegate<TReturn>(this PropertyInfo property) {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (!typeof(TReturn).IsAssignableFrom(property.PropertyType)) {
                throw new Exception($"Field type {property.PropertyType} does not match the requested type {typeof(TReturn)}.");
            }

            using (var method = new DynamicMethodDefinition($"{property} Getter", typeof(TReturn), new[] { typeof(object) })) {
                var il = method.GetILProcessor();

                var getMethod = property.GetGetMethod(nonPublic: true);

                if (!getMethod.IsStatic) {
                    il.Emit(OpCodes.Ldarg_0);
                }

                il.Emit(OpCodes.Call, getMethod);
                il.Emit(OpCodes.Ret);

                return (GetDelegate<TReturn>)method.Generate().CreateDelegate(typeof(GetDelegate<TReturn>));
            }
        }

        private static GetDelegateRef<TInstance, TReturn> CreateGetDelegate<TInstance, TReturn>(this PropertyInfo property) where TInstance : struct {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (!typeof(TReturn).IsAssignableFrom(property.PropertyType)) {
                throw new Exception($"Field type {property.PropertyType} does not match the requested type {typeof(TReturn)}.");
            }

            using (var method = new DynamicMethodDefinition($"{property} Getter", typeof(TReturn), new[] { typeof(TInstance).MakeByRefType() })) {
                var il = method.GetILProcessor();

                // Cache this as well?
                var getMethod = property.GetGetMethod(nonPublic: true);

                if (!getMethod.IsStatic) {
                    il.Emit(OpCodes.Ldarg_0);
                }

                il.Emit(OpCodes.Call, getMethod);
                il.Emit(OpCodes.Ret);

                return (GetDelegateRef<TInstance, TReturn>)method.Generate().CreateDelegate(typeof(GetDelegateRef<TInstance, TReturn>));
            }
        }


        private static SetDelegate<TValue> CreateSetDelegate<TValue>(this PropertyInfo property) {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (!property.PropertyType.IsAssignableFrom(typeof(TValue))) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {property.PropertyType}.");
            }

            using (var method = new DynamicMethodDefinition($"{property} Setter", typeof(void),
                new[] { typeof(object), typeof(TValue) })) {
                var il = method.GetILProcessor();

                var setMethod = property.GetSetMethod(true);

                if (!setMethod.IsStatic) {
                    il.Emit(OpCodes.Ldarg_0);
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, setMethod);
                il.Emit(OpCodes.Ret);

                return (SetDelegate<TValue>)method.Generate().CreateDelegate(typeof(SetDelegate<TValue>));
            }
        }

        private static SetDelegateRef<TInstance, TValue> CreateSetDelegateRef<TInstance, TValue>(this PropertyInfo property) where TInstance : struct {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (!property.PropertyType.IsAssignableFrom(typeof(TValue))) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {property.PropertyType}.");
            }

            using (var method = new DynamicMethodDefinition($"{property} SetterByRef", typeof(void),
                new[] { typeof(TInstance).MakeByRefType(), typeof(TValue) })) {
                var il = method.GetILProcessor();

                var setMethod = property.GetSetMethod(true);

                if (!setMethod.IsStatic) {
                    il.Emit(OpCodes.Ldarg_0);
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, setMethod);
                il.Emit(OpCodes.Ret);

                return (SetDelegateRef<TInstance, TValue>)method.Generate().CreateDelegate(typeof(SetDelegateRef<TInstance, TValue>));
            }
        }

        // Partial hack from https://github.com/0x0ade/MonoMod/blob/master/MonoMod.Utils/FastReflectionHelper.cs
        // to get fast call delegates
        private static FastReflectionDelegate GenerateCallDelegate(this MethodInfo method) {
            if (method == null) {
                throw new ArgumentException("Method cannot be null.", nameof(method));
            }

            using var dmd = new DynamicMethodDefinition(
                $"CallDelegate<{method.Name}>", typeof(object), new[] { typeof(object), typeof(object[]) });
            var il = dmd.GetILProcessor();

            var args = method.GetParameters();

            if (!method.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
                if (method.DeclaringType.GetTypeInfo().IsValueType) {
                    il.Emit(OpCodes.Unbox_Any, method.DeclaringType);
                }
            }

            for (var i = 0; i < args.Length; i++) {
                var argType = args[i].ParameterType;
                var argIsByRef = argType.IsByRef;
                if (argIsByRef)
                    argType = argType.GetElementType();
                var argIsValueType = argType.GetTypeInfo().IsValueType;

                if (argIsByRef && argIsValueType) {
                    // Used later when storing back the reference to the new box in the array.
                    il.Emit(OpCodes.Ldarg_1);
                    il.EmitFast_Ldc_I4(i);
                }

                il.Emit(OpCodes.Ldarg_1);
                il.EmitFast_Ldc_I4(i);

                if (argIsByRef && !argIsValueType) {
                    il.Emit(OpCodes.Ldelema, typeof(object));
                }
                else {
                    il.Emit(OpCodes.Ldelem_Ref);
                    if (!argIsValueType) continue;
                    il.Emit(!argIsByRef ? OpCodes.Unbox_Any : OpCodes.Unbox, argType);
                }
            }

            if (method.IsFinal || !method.IsVirtual) {
                il.Emit(OpCodes.Call, method);
            }
            else {
                il.Emit(OpCodes.Callvirt, method);
            }

            var returnType = method.IsConstructor ? method.DeclaringType : method.ReturnType;
            if (returnType != typeof(void)) {
                if (returnType.GetTypeInfo().IsValueType) {
                    il.Emit(OpCodes.Box, returnType);
                }
            }
            else {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Ret);

            return (FastReflectionDelegate)dmd.Generate().CreateDelegate(typeof(FastReflectionDelegate));
        }

        // https://github.com/0x0ade/MonoMod/blob/master/MonoMod.Utils/FastReflectionHelper.cs
        public static void EmitFast_Ldc_I4(this ILProcessor? il, int value) {
            switch (value) {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
            else
                il.Emit(OpCodes.Ldc_I4, value);
        }

        public static byte ReadLocalIndex(OpCode opCode, object? operand) {
            if (opCode == OpCodes.Ldloc_0 || opCode == OpCodes.Stloc_0) {
                return 0;
            }
            if (opCode == OpCodes.Ldloc_1 || opCode == OpCodes.Stloc_1) {
                return 1;
            }
            if (opCode == OpCodes.Ldloc_2 || opCode == OpCodes.Stloc_2) {
                return 2;
            }
            if (opCode == OpCodes.Ldloc_3 || opCode == OpCodes.Stloc_3) {
                return 3;
            }
            if (opCode == OpCodes.Ldloc_S || opCode == OpCodes.Stloc_S) {
                return (byte)operand;
            }

            throw new Exception($"Could not read index for opcode and operand: {opCode} - {operand}");
        }

        #endregion

        public static System.Reflection.FieldInfo GetNestedField(Type type, string fieldName) {
            var nestedTypes = type.GetNestedTypes((System.Reflection.BindingFlags)(-1));
            foreach (Type nestedType in nestedTypes) {
                var fieldInfo = nestedType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null) {
                    return fieldInfo;
                }
                fieldInfo = nestedType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (fieldInfo != null) {
                    return fieldInfo;
                }
                fieldInfo = nestedType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null) {
                    return fieldInfo;
                }
                fieldInfo = nestedType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (fieldInfo != null) {
                    return fieldInfo;
                }
            }
            return null;
        }

        public static System.Reflection.MethodInfo GetNestedMethod(Type type, string methodName) {
            var nestedTypes = type.GetNestedTypes((System.Reflection.BindingFlags)(-1));
            foreach (Type nestedType in nestedTypes) {
                var methodInfo = nestedType.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (methodInfo != null) {
                    return methodInfo;
                }
                methodInfo = nestedType.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (methodInfo != null) {
                    return methodInfo;
                }
                methodInfo = nestedType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (methodInfo != null) {
                    return methodInfo;
                }
                methodInfo = nestedType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (methodInfo != null) {
                    return methodInfo;
                }
            }
            return null;
        }
        public static void LogCursorOpcodes(ILCursor cursor) {
            cursor.Goto(0);
            while (cursor.Next != null) {
                Debug.Log(cursor.Next.OpCode);
                cursor.Index++;
                cursor.Goto(cursor.Index);
            }
        }

        public static MethodInfo GetGenericMethod(Type type, string name, Type[] parameters) {
            var classMethods = type.GetMethods((System.Reflection.BindingFlags)(-1));
            foreach (System.Reflection.MethodInfo methodInfo in classMethods) {
                if (methodInfo.Name == name) {
                    System.Reflection.ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                    if (parameterInfos.Length == parameters.Length) {
                        bool parameterMatch = true;
                        for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++) {
                            if (parameterInfos[parameterIndex].ParameterType.Name != parameters[parameterIndex].Name) {
                                parameterMatch = false;
                                break;
                            }
                        }
                        if (parameterMatch) {
                            return methodInfo;
                        }
                    }
                }
            }
            return null;
        }
    }
}
