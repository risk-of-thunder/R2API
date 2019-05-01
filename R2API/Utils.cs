using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace R2API.Utils {
    public static class Reflection {
        private const BindingFlags _defaultFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        #region Field

        public static TReturn GetFieldValue<TReturn>(this object instance, string fieldName) {
            return (TReturn) instance.GetType()
                .GetFieldCached(fieldName, _defaultFlags | BindingFlags.Instance)
                .GetValue(instance);
        }

        public static TReturn GetFieldValue<TClass, TReturn>(string fieldName) {
            return (TReturn) typeof(TClass)
                .GetFieldCached(fieldName, _defaultFlags | BindingFlags.Static)
                .GetValue(null);
        }

        public static void SetFieldValue(this object instance, string fieldName, object value) {
            instance.GetType()
                .GetFieldCached(fieldName, _defaultFlags | BindingFlags.Instance)
                .SetValue(instance, value);
        }

        public static void SetFieldValue<TClass>(string fieldName, object value) {
            typeof(TClass)
                .GetFieldCached(fieldName, _defaultFlags | BindingFlags.Static)
                .SetValue(null, value);
        }

        #endregion

        #region Property

        public static TReturn GetPropertyValue<TReturn>(this object instance, string propName) {
            return (TReturn) instance.GetType()
                .GetPropertyCached(propName, _defaultFlags | BindingFlags.Instance)
                .GetValue(instance);
        }

        public static TReturn GetPropertyValue<TClass, TReturn>(string propName) {
            return (TReturn) typeof(TClass)
                .GetPropertyCached(propName, _defaultFlags | BindingFlags.Static)
                .GetValue(null);
        }

        public static void SetPropertyValue(this object instance, string propName, object value) {
            instance.GetType()
                .GetPropertyCached(propName, _defaultFlags | BindingFlags.Instance)
                .SetValue(instance, value);
        }

        public static void SetPropertyValue<TClass>(string propName, object value) {
            typeof(TClass).GetPropertyCached(propName, _defaultFlags | BindingFlags.Static)
                .SetValue(null, value);
        }

        #endregion

        #region Method

        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName) =>
            instance.InvokeMethod<TReturn>(methodName, null);

        public static TReturn InvokeMethod<TClass, TReturn>(string methodName) =>
            InvokeMethod<TClass, TReturn>(methodName, null);

        public static void InvokeMethod(this object instance, string methodName) =>
            instance.InvokeMethod<object>(methodName);

        public static void InvokeMethod<TClass>(string methodName) =>
            InvokeMethod<TClass, object>(methodName);


        public static TReturn InvokeMethod<TReturn>(this object instance, string methodName,
            params object[] methodParams) {
            return (TReturn) (methodParams == null
                    ? instance.GetType()
                        .GetMethodCached(methodName, _defaultFlags | BindingFlags.Instance)
                    : instance.GetType()
                        .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray(),
                            _defaultFlags | BindingFlags.Instance)
                )
                .Invoke(instance, methodParams);
        }

        public static TReturn InvokeMethod<TClass, TReturn>(string methodName, params object[] methodParams) {
            return (TReturn) (methodParams == null
                    ? typeof(TClass)
                        .GetMethodCached(methodName, _defaultFlags | BindingFlags.Instance)
                    : typeof(TClass)
                        .GetMethodCached(methodName, methodParams.Select(x => x.GetType()).ToArray(),
                            _defaultFlags | BindingFlags.Instance)
                )
                .Invoke(null, methodParams);
        }

        public static void InvokeMethod(this object instance, string methodName, params object[] methodParams) =>
            instance.InvokeMethod<object>(methodName, methodParams);

        public static void InvokeMethod<TClass>(string methodName, params object[] methodParams) =>
            InvokeMethod<TClass, object>(methodName, methodParams);

        #endregion

        #region Class

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
