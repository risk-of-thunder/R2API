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

        private delegate object GetDelegate(object instance);

        private delegate void SetDelegate(object instance, object value);

        private delegate object CallDelegate(object instance, object[] arguments);

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
            FieldCache.GetOrAddOnNull((T, name), x => x.T.GetFieldFull(x.name));

        public static TReturn GetFieldValue<TReturn>(this object instance, string fieldName) =>
            (TReturn) instance.GetType()
                .GetFieldCached(fieldName)?
                .GetFieldGetDelegate<TReturn>()
                (instance);

        public static TReturn GetFieldValue<TReturn>(this Type staticType, string fieldName) =>
            (TReturn) staticType
                .GetFieldCached(fieldName)?
                .GetFieldGetDelegate<TReturn>()
                (null);

        public static void SetFieldValue<TValue>(this object instance, string fieldName, TValue value) =>
            instance.GetType()
                .GetFieldCached(fieldName)?
                .GetFieldSetDelegate()
                (instance, value);

        public static void SetFieldValue<TValue>(this Type staticType, string fieldName, TValue value) =>
            staticType
                .GetFieldCached(fieldName)?
                .GetFieldSetDelegate()
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

        private static GetDelegate GetFieldGetDelegate<TValue>(this FieldInfo field) =>
            FieldGetDelegateCache.GetOrAdd(field, x => x.CreateGetDelegate<TValue>());

        private static SetDelegate GetFieldSetDelegate(this FieldInfo field) =>
            FieldSetDelegateCache.GetOrAdd(field, x => x.CreateSetDelegate());

        #endregion

        #region Property

        public static PropertyInfo GetPropertyCached<T>(string name) =>
            typeof(T).GetPropertyCached(name);

        public static PropertyInfo GetPropertyCached(this Type T, string name) =>
            PropertyCache.GetOrAddOnNull((T, name), x => x.T.GetProperty(x.name, AllFlags));

        public static TReturn GetPropertyValue<TReturn>(this object instance, string propName) =>
            (TReturn) instance.GetType()
                .GetPropertyCached(propName)?
                .GetPropertyGetDelegate<TReturn>()
                (instance);

        public static TReturn GetPropertyValue<TReturn>(this Type staticType, string propName) =>
            (TReturn) staticType
                .GetPropertyCached(propName)?
                .GetPropertyGetDelegate<TReturn>()
                (null);

        public static void SetPropertyValue(this object instance, string propName, object value) =>
            instance.GetType()
                .GetPropertyCached(propName)?
                .GetPropertySetDelegate()
                (instance, value);

        public static void SetPropertyValue(this Type staticType, string propName, object value) =>
            staticType.GetPropertyCached(propName)?
                .GetPropertySetDelegate()
                (null, value);


        private static GetDelegate GetPropertyGetDelegate<TValue>(this PropertyInfo property) =>
            PropertyGetDelegateCache.GetOrAdd(property, prop => prop.CreateGetDelegate<TValue>());

        private static SetDelegate GetPropertySetDelegate(this PropertyInfo property) =>
            PropertySetDelegateCache.GetOrAdd(property, prop => prop.CreateSetDelegate());

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
                x => x.T.GetMethod(x.name, AllFlags, null, x.argumentTypes, null));

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

            return (TReturn) methodInfo.GetMethodDelegateCached()(null, methodParams);
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
            // Thanks 0x0 :)
            DelegateCache.GetOrAdd(methodInfo, method => method.GenerateCallDelegate());

        #endregion

        #region Class

        public static ConstructorInfo GetConstructorCached<T>(Type[] argumentTypes) =>
            GetConstructorCached(typeof(T), argumentTypes);

        public static ConstructorInfo GetConstructorCached(this Type T, Type[] argumentTypes) =>
            ConstructorCache.GetOrAddOnNull((T, argumentTypes), x => x.T.GetConstructor(x.argumentTypes));

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

        #region Fast Reflection

        private static GetDelegate CreateGetDelegate<T>(this FieldInfo field) {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            var method = new DynamicMethodDefinition($"{field} Getter", typeof(T), new[] { typeof(object) });
            var il = method.GetILProcessor();

            if (!field.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
            } else {
                il.Emit(OpCodes.Ldsfld, field);
            }

            il.Emit(OpCodes.Ret);

            return (GetDelegate)method.Generate().CreateDelegate(typeof(GetDelegate));
        }

        private static SetDelegate CreateSetDelegate(this FieldInfo field) {
            if (field == null) {
                throw new ArgumentException("Field cannot be null.", nameof(field));
            }

            var method = new DynamicMethodDefinition($"{field} Setter", typeof(void),
                new[] { typeof(object), typeof(object) });
            var il = method.GetILProcessor();

            if (!field.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
            } else {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stsfld, field);
            }

            il.Emit(OpCodes.Ret);

            return (SetDelegate)method.Generate().CreateDelegate(typeof(SetDelegate));
        }

        private static GetDelegate CreateGetDelegate<TValue>(this PropertyInfo property) {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            var method = new DynamicMethodDefinition($"{property} Getter", typeof(TValue), new[] { typeof(object) });
            var il = method.GetILProcessor();

            var getMethod = property.GetGetMethod(true);

            if (!getMethod.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Call, getMethod);
            il.Emit(OpCodes.Ret);

            return (GetDelegate)method.Generate().CreateDelegate(typeof(GetDelegate));
        }

        private static SetDelegate CreateSetDelegate(this PropertyInfo property) {
            if (property == null) {
                throw new ArgumentException("Property cannot be null.", nameof(property));
            }

            var method = new DynamicMethodDefinition($"{property} Setter", typeof(void),
                new[] { typeof(object), typeof(object) });
            var il = method.GetILProcessor();

            var setMethod = property.GetSetMethod(true);

            if (!setMethod.IsStatic) {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, setMethod);
            il.Emit(OpCodes.Ret);

            return (SetDelegate)method.Generate().CreateDelegate(typeof(SetDelegate));
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
