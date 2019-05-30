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

        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                              BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private delegate T GetDelegate<out T>(object instance);

        private delegate void SetDelegate<in T>(object instance, T value);

        private delegate object CallDelegate(object instance, object[] arguments);

        private delegate void SetDelegateRef<TInstance, in TValue>(ref TInstance instance, TValue value) where TInstance : struct;

        public delegate T GetDelegateRef<TInstance, out T>(ref TInstance instance) where TInstance : struct;

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

        private static readonly ConcurrentDictionary<MethodInfo, CallDelegate> DelegateCache =
            new ConcurrentDictionary<MethodInfo, CallDelegate>();


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

        public static FieldInfo GetFieldCached<T>(string name) =>
            typeof(T).GetFieldCached(name);

        public static FieldInfo GetFieldCached(this Type T, string name) =>
            FieldCache.GetOrAddOnNull((T, name), x => x.T.GetFieldFull(x.name)
                ?? throw new Exception($"Could not find {nameof(FieldInfo)} on {T.FullName} with the name {name}"));

        public static TReturn GetFieldValue<TReturn>(this object instance, string fieldName) =>
            instance.GetType()
                .GetFieldCached(fieldName)
                .GetFieldGetDelegate<TReturn>()
                (instance);

        public static TReturn GetFieldValue<TReturn>(this Type staticType, string fieldName) =>
            staticType
                .GetFieldCached(fieldName)
                .GetFieldGetDelegate<TReturn>()
                (null);

        public static void SetFieldValue<TValue>(this object instance, string fieldName, TValue value) =>
            instance.GetType()
                .GetFieldCached(fieldName)
                .GetFieldSetDelegate<TValue>()
                (instance, value);

        public static void SetFieldValue<TValue>(this Type staticType, string fieldName, TValue value) =>
            staticType
                .GetFieldCached(fieldName)
                .GetFieldSetDelegate<TValue>()
                (null, value);

        public static void SetStructFieldValue<TInstance, TValue>(this ref TInstance instance, string fieldName, TValue value)
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

        private static GetDelegate<TReturn> GetFieldGetDelegate<TReturn>(this FieldInfo field) =>
            (GetDelegate<TReturn>)FieldGetDelegateCache.GetOrAdd(field, x => x.CreateGetDelegate<TReturn>());

        private static SetDelegate<TValue> GetFieldSetDelegate<TValue>(this FieldInfo field) =>
            (SetDelegate<TValue>)FieldSetDelegateCache.GetOrAdd(field, x => x.CreateSetDelegate<TValue>());

        private static SetDelegateRef<TInstance, TValue> GetFieldSetDelegateRef<TInstance, TValue>(this FieldInfo field) where TInstance : struct =>
            (SetDelegateRef<TInstance, TValue>)FieldSetDelegateCache.GetOrAdd(field, x => x.CreateSetDelegateRef<TInstance, TValue>());

        #endregion

        #region Property

        public static PropertyInfo GetPropertyCached<T>(string name) =>
            typeof(T).GetPropertyCached(name);

        public static PropertyInfo GetPropertyCached(this Type T, string name) =>
            PropertyCache.GetOrAddOnNull((T, name), x => x.T.GetProperty(x.name, AllFlags));

        public static TReturn GetPropertyValue<TReturn>(this object instance, string propName) =>
            (TReturn) instance.GetType()
                .GetPropertyCached(propName)
                .GetPropertyGetDelegate<TReturn>()
                (instance);

        public static TReturn GetPropertyValue<TReturn>(this Type staticType, string propName) =>
            (TReturn) staticType
                .GetPropertyCached(propName)
                .GetPropertyGetDelegate<TReturn>()
                (null);

        public static void SetPropertyValue<TValue>(this object instance, string propName, TValue value) =>
            instance.GetType()
                .GetPropertyCached(propName)?
                .GetPropertySetDelegate<TValue>()
                (instance, value);

        public static void SetPropertyValue<TValue>(this Type staticType, string propName, TValue value) =>
            staticType.GetPropertyCached(propName)?
                .GetPropertySetDelegate<TValue>()
                (null, value);

        public static void SetStructPropertyValue<TInstance, TValue>(this ref TInstance instance, string propName,
            TValue value)
            where TInstance : struct =>
            typeof(TInstance)
                .GetPropertyCached(propName)
                .GetPropertySetDelegateRef<TInstance, TValue>()
                (ref instance, value);

        public static TValue GetStructPropertyValue<TInstance, TValue>(this ref TInstance instance, string propName)
            where TInstance : struct =>
            typeof(TInstance)
                .GetPropertyCached(propName)
                .GetPropertyGetDelegateRef<TInstance, TValue>()
                (ref instance);

        private static GetDelegate<TReturn> GetPropertyGetDelegate<TReturn>(this PropertyInfo property) =>
            (GetDelegate<TReturn>)PropertyGetDelegateCache.GetOrAdd(property, prop => prop.CreateGetDelegate<TReturn>());

        private static GetDelegateRef<TInstance, TReturn> GetPropertyGetDelegateRef<TInstance, TReturn>(this PropertyInfo property)
            where TInstance : struct =>
            (GetDelegateRef<TInstance, TReturn>)PropertyGetDelegateCache.GetOrAdd(property, prop => prop.CreateGetDelegate<TInstance, TReturn>());

        private static SetDelegate<TValue> GetPropertySetDelegate<TValue>(this PropertyInfo property) =>
            (SetDelegate<TValue>)PropertySetDelegateCache.GetOrAdd(property, prop => prop.CreateSetDelegate<TValue>());

        private static SetDelegateRef<TInstance, TValue> GetPropertySetDelegateRef<TInstance, TValue>(
            this PropertyInfo property)
            where TInstance : struct =>
            (SetDelegateRef<TInstance, TValue>) PropertySetDelegateCache.GetOrAdd(property,
                prop => prop.CreateSetDelegateRef<TInstance, TValue>());

        #endregion

        #region Method

        public static MethodInfo GetMethodCached<T>(string name) =>
            typeof(T).GetMethodCached(name);

        public static MethodInfo GetMethodCached(this Type T, string name) =>
            MethodCache.GetOrAddOnNull((T, name), x => x.T.GetMethod(x.name, AllFlags)
                ?? throw new Exception($"Could not find {nameof(MethodInfo)} on {T.FullName} with the name {name}"));

        public static MethodInfo GetMethodCached<T>(string name, Type[] argumentTypes) =>
            typeof(T).GetMethodCached(name, argumentTypes);

        public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes) =>
            OverloadedMethodCache.GetOrAddOnNull((T, name, argumentTypes),
                x => x.T.GetMethod(x.name, AllFlags, null, x.argumentTypes, null)
                     ?? throw new Exception($"Could not find {nameof(MethodInfo)} on {T.FullName} with the name {name} and arguments: " +
                                            $"{string.Join(",", argumentTypes.Select(a => a.FullName))}"));

        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName) =>
            instance.InvokeMethod<TReturn>(methodName, null);

        public static TReturn InvokeMethod<TReturn>(this Type staticType, string methodName) =>
            staticType.InvokeMethod<TReturn>(methodName, null);

        public static void InvokeMethod(this object instance, string methodName) =>
            instance.InvokeMethod<object>(methodName);

        public static void InvokeMethod(this Type staticType, string methodName) =>
            staticType.InvokeMethod<object>(methodName);

        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName,
            params object[] methodParams) {
            var methodInfo = (methodParams == null
                    ? instance.GetType()
                        .GetMethodCached(methodName)
                    : instance.GetType()
                        .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray())
                ) ?? throw new Exception($"Could not find method on type {instance.GetType()} with the name of {methodName} with the arguments specified.");

            return (TReturn) methodInfo.GetMethodDelegateCached()(instance, methodParams);
        }

        public static TReturn InvokeMethod<TReturn>(this Type staticType, string methodName,
            params object[] methodParams) {
            var methodInfo = (methodParams == null
                ? staticType
                    .GetMethodCached(methodName)
                : staticType
                    .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray()))
                ?? throw new Exception($"Could not find method on type {staticType} with the name of {methodName} with the arguments specified.");

            return (TReturn) methodInfo.GetMethodDelegateCached()(null, methodParams);
        }

        public static void InvokeMethod(this object instance, string methodName, params object[] methodParams) =>
            instance.InvokeMethod<object>(methodName, methodParams);

        public static void InvokeMethod(this Type staticType, string methodName, params object[] methodParams) =>
            staticType.InvokeMethod<object>(methodName, methodParams);


        private static CallDelegate GetMethodDelegateCached(this MethodInfo methodInfo) =>
            DelegateCache.GetOrAdd(methodInfo, method => method.GenerateCallDelegate());

        #endregion

        #region Class

        public static ConstructorInfo GetConstructorCached<T>(Type[] argumentTypes) =>
            GetConstructorCached(typeof(T), argumentTypes);

        public static ConstructorInfo GetConstructorCached(this Type T, Type[] argumentTypes) =>
            ConstructorCache.GetOrAddOnNull((T, argumentTypes), x => x.T.GetConstructor(x.argumentTypes)
                ?? throw new Exception($"Could not find {nameof(ConstructorInfo)} on {T.FullName} with the arguments {string.Join(",", argumentTypes.Select(a => a.FullName))}"));

        public static Type GetNestedType<T>(string name) =>
            typeof(T).GetNestedTypeCached(name);

        public static Type GetNestedTypeCached<T>(string name) =>
            typeof(T).GetNestedTypeCached(name);

        public static Type GetNestedTypeCached(this Type T, string name) =>
            NestedTypeCache.GetOrAddOnNull((T, name), x => x.T.GetNestedType(x.name, AllFlags)
                ?? throw new Exception($"Could not find nested {nameof(Type)} on {T.FullName} with the name {name}"));

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

        #region Fast Reflection

        private static GetDelegate<TReturn> CreateGetDelegate<TReturn>(this FieldInfo field) {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            if (field.FieldType != typeof(TReturn)) {
                throw new Exception($"Field type {field.FieldType} does not match the requested type {typeof(TReturn)}.");
            }


            var method = new DynamicMethodDefinition($"{field} Getter", typeof(TReturn), new[] { typeof(object) });
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

        private static SetDelegate<TValue> CreateSetDelegate<TValue>(this FieldInfo field) {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            if (field.FieldType != typeof(TValue)) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {field.FieldType}.");
            }

            var method = new DynamicMethodDefinition($"{field} Setter", typeof(void),
                new[] { typeof(object), typeof(TValue) });
            var il = method.GetILProcessor();

            if (!field.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(!field.IsStatic ? OpCodes.Stfld : OpCodes.Stsfld, field);

            il.Emit(OpCodes.Ret);

            return (SetDelegate<TValue>)method.Generate().CreateDelegate(typeof(SetDelegate<TValue>));
        }

        private static SetDelegateRef<TInstance, TValue> CreateSetDelegateRef<TInstance, TValue>(this FieldInfo field) where TInstance : struct {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            if (field.FieldType != typeof(TValue)) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {field.FieldType}.");
            }

            var method = new DynamicMethodDefinition($"{field} SetterByRef", typeof(void),
                new[] { typeof(TInstance).MakeByRefType(), typeof(TValue) });
            var il = method.GetILProcessor();

            if (!field.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(!field.IsStatic ? OpCodes.Stfld : OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);

            return (SetDelegateRef<TInstance, TValue>)method.Generate().CreateDelegate(typeof(SetDelegateRef<TInstance, TValue>));
        }

        private static GetDelegate<TReturn> CreateGetDelegate<TReturn>(this PropertyInfo property) {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (property.PropertyType != typeof(TReturn)) {
                throw new Exception($"Field type {property.PropertyType} does not match the requested type {typeof(TReturn)}.");
            }

            var method = new DynamicMethodDefinition($"{property} Getter", typeof(TReturn), new[] { typeof(object) });
            var il = method.GetILProcessor();

            var getMethod = property.GetGetMethod(nonPublic: true);

            if (!getMethod.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Call, getMethod);
            il.Emit(OpCodes.Ret);

            return (GetDelegate<TReturn>)method.Generate().CreateDelegate(typeof(GetDelegate<TReturn>));
        }

        public static GetDelegateRef<TInstance, TReturn> CreateGetDelegate<TInstance, TReturn>(this PropertyInfo property) where TInstance : struct {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (property.PropertyType != typeof(TReturn)) {
                throw new Exception($"Field type {property.PropertyType} does not match the requested type {typeof(TReturn)}.");
            }

            var method = new DynamicMethodDefinition($"{property} Getter", typeof(TReturn), new[] { typeof(TInstance).MakeByRefType() });
            var il = method.GetILProcessor();

            var getMethod = property.GetGetMethod(nonPublic: true);

            if (!getMethod.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Call, getMethod);
            il.Emit(OpCodes.Ret);

            return (GetDelegateRef<TInstance, TReturn>)method.Generate().CreateDelegate(typeof(GetDelegateRef<TInstance, TReturn>));
        }


        private static SetDelegate<TValue> CreateSetDelegate<TValue>(this PropertyInfo property) {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (property.PropertyType != typeof(TValue)) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {property.PropertyType}.");
            }

            var method = new DynamicMethodDefinition($"{property} Setter", typeof(void),
                new[] { typeof(object), typeof(TValue) });
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

        private static SetDelegateRef<TInstance, TValue> CreateSetDelegateRef<TInstance, TValue>(this PropertyInfo property) where TInstance : struct {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            if (property.PropertyType != typeof(TValue)) {
                throw new Exception($"Value type type {typeof(TValue)} does not match the requested type {property.PropertyType}.");
            }

            var method = new DynamicMethodDefinition($"{property} SetterByRef", typeof(void),
                new[] { typeof(TInstance).MakeByRefType(), typeof(TValue) });
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

        // Partial hack from https://github.com/0x0ade/MonoMod/blob/master/MonoMod.Utils/FastReflectionHelper.cs
        // to get fast call delegates
        private static CallDelegate GenerateCallDelegate(this MethodInfo method) {
            if (method == null) {
                throw new ArgumentException("Method cannot be null.", nameof(method));
            }

            var dmd = new DynamicMethodDefinition(
                $"CallDelegate<{method.GetFindableID(simple: true)}>", typeof(object), new[] { typeof(object), typeof(object[]) });
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

            return (CallDelegate) dmd.Generate().CreateDelegate(typeof(CallDelegate));
        }

        // https://github.com/0x0ade/MonoMod/blob/master/MonoMod.Utils/FastReflectionHelper.cs
        private static void EmitFast_Ldc_I4(this ILProcessor il, int value) {
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
                il.Emit(OpCodes.Ldc_I4_S, (sbyte) value);
            else
                il.Emit(OpCodes.Ldc_I4, value);
        }

        #endregion
    }
}
