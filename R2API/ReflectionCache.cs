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

		public static FieldInfo GetField<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetField(typeof(T), name, bindingFlags);
		}

		public static FieldInfo GetField(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (fieldCache.ContainsKey(key)) {
				return fieldCache[key];
			}

			var fieldInfo = T.GetField(name, bindingFlags);
			fieldCache[key] = fieldInfo;
			return fieldInfo;
		}

		public static void SetFieldValue<T>(this object instance, string name, T value, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			GetField(type, name, bindingFlags).SetValue(instance, value);
		}

		public static T GetFieldValue<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)GetField(type, name, bindingFlags).GetValue(instance);
		}

		public static MethodInfo GetMethod<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetMethod(typeof(T), name, bindingFlags);
		}

		public static MethodInfo GetMethod(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (methodCache.ContainsKey(key)) {
				return methodCache[key];
			}

			var methodInfo = T.GetMethod(name, bindingFlags);
			methodCache[key] = methodInfo;
			return methodInfo;
		}

		public static MethodInfo GetMethod(this Type T, string name, Type[] argumentTypes, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new Tuple<Type, string, Type[]>(T, name, argumentTypes);
			if (overloadedMethodCache.ContainsKey(key)) {
				return overloadedMethodCache[key];
			}

			var methodInfo = T.GetMethod(name, bindingFlags);
			overloadedMethodCache[key] = methodInfo;
			return methodInfo;
		}

		public static T InvokeMethod<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags, params object[] args)
		{
			var type = instance.GetType();
			return (T)GetMethod(type, name, bindingFlags).Invoke(instance, args);
		}

		public static T InvokeMethod<T>(this object instance, string name, Type[] argumentTypes, BindingFlags bindingFlags = _bindingFlags, params object[] args)
		{
			var type = instance.GetType();
			return (T)GetMethod(type, name, argumentTypes, bindingFlags).Invoke(instance, args);
		}


		public static PropertyInfo GetProperty<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetProperty(typeof(T), name, bindingFlags);
		}

		public static PropertyInfo GetProperty(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (propertyCache.ContainsKey(key)) {
				return propertyCache[key];
			}

			var propertyInfo = T.GetProperty(name, bindingFlags);
			propertyCache[key] = propertyInfo;
			return propertyInfo;
		}

		public static T GetPropertyValue<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)GetProperty(type, name, bindingFlags).GetValue(instance);
		}

		public static void SetPropertyValue<T>(this object instance, string name, T value, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			GetProperty(type, name, bindingFlags).SetValue(instance, value);
		}
	}
}
