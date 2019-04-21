using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace R2API
{
	public static class ReflectionCache
	{
		private static readonly Dictionary<KeyValuePair<Type, string>, FieldInfo> fieldCache = new Dictionary<KeyValuePair<Type, string>, FieldInfo>();
		private static readonly Dictionary<KeyValuePair<Type, string>, MethodInfo> methodCache = new Dictionary<KeyValuePair<Type, string>, MethodInfo>();
		private static readonly Dictionary<Tuple<Type, string, Type[]>, MethodInfo> overloadedMethodCache = new Dictionary<Tuple<Type, string, Type[]>, MethodInfo>();
		private static readonly Dictionary<KeyValuePair<Type, string>, PropertyInfo> propertyCache = new Dictionary<KeyValuePair<Type, string>, PropertyInfo>();

		private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

		public static FieldInfo GetFieldCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetFieldCached(typeof(T), name, bindingFlags);
		}

		public static FieldInfo GetFieldCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (fieldCache.ContainsKey(key)) {
				return fieldCache[key];
			}

			var fieldInfo = T.GetFieldCached(name, bindingFlags);
			fieldCache[key] = fieldInfo;
			return fieldInfo;
		}

		public static void SetFieldValueCached<T>(this object instance, string name, T value, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			GetFieldCached(type, name, bindingFlags).SetValue(instance, value);
		}

		public static T GetFieldValueCached<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)GetFieldCached(type, name, bindingFlags).GetValue(instance);
		}

		public static MethodInfo GetMethodCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetMethodCached(typeof(T), name, bindingFlags);
		}

		public static MethodInfo GetMethodCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (methodCache.ContainsKey(key)) {
				return methodCache[key];
			}

			var methodInfo = T.GetMethodCached(name, bindingFlags);
			methodCache[key] = methodInfo;
			return methodInfo;
		}

		public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new Tuple<Type, string, Type[]>(T, name, argumentTypes);
			if (overloadedMethodCache.ContainsKey(key)) {
				return overloadedMethodCache[key];
			}

			var methodInfo = T.GetMethodCached(name, bindingFlags);
			overloadedMethodCache[key] = methodInfo;
			return methodInfo;
		}

		public static T InvokeMethod<T>(this object instance, string name, object[] args, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)GetMethodCached(type, name, bindingFlags).Invoke(instance, args);
		}

		public static void InvokeMethod(this object instance, string name, object arg, BindingFlags bindingFlags = _bindingFlags)
		{
			InvokeMethod(instance, name, new object[] { arg }, bindingFlags);
		}

		public static void InvokeMethod(this object instance, string name, object[] args, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			GetMethodCached(type, name, bindingFlags).Invoke(instance, args);
		}

		public static T InvokeMethod<T>(this object instance, string name, Type[] argumentTypes, object[] args, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)GetMethodCached(type, name, argumentTypes, bindingFlags).Invoke(instance, args);
		}


		public static PropertyInfo GetPropertyCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetPropertyCached(typeof(T), name, bindingFlags);
		}

		public static PropertyInfo GetPropertyCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (propertyCache.ContainsKey(key)) {
				return propertyCache[key];
			}

			var propertyInfo = T.GetPropertyCached(name, bindingFlags);
			propertyCache[key] = propertyInfo;
			return propertyInfo;
		}

		public static T GetPropertyValueCached<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)GetPropertyCached(type, name, bindingFlags).GetValue(instance);
		}

		public static void SetPropertyValueCached<T>(this object instance, string name, T value, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			GetPropertyCached(type, name, bindingFlags).SetValue(instance, value);
		}
	}
}
