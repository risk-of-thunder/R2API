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

		public static FieldInfo CGetField<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return CGetField(typeof(T), name, bindingFlags);
		}

		public static FieldInfo CGetField(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (fieldCache.ContainsKey(key)) {
				return fieldCache[key];
			}

			var fieldInfo = T.CGetField(name, bindingFlags);
			fieldCache[key] = fieldInfo;
			return fieldInfo;
		}

		public static void CSetFieldValue<T>(this object instance, string name, T value, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			CGetField(type, name, bindingFlags).SetValue(instance, value);
		}

		public static T CGetFieldValue<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)CGetField(type, name, bindingFlags).GetValue(instance);
		}

		public static MethodInfo CGetMethod<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return CGetMethod(typeof(T), name, bindingFlags);
		}

		public static MethodInfo CGetMethod(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (methodCache.ContainsKey(key)) {
				return methodCache[key];
			}

			var methodInfo = T.CGetMethod(name, bindingFlags);
			methodCache[key] = methodInfo;
			return methodInfo;
		}

		public static MethodInfo CGetMethod(this Type T, string name, Type[] argumentTypes, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new Tuple<Type, string, Type[]>(T, name, argumentTypes);
			if (overloadedMethodCache.ContainsKey(key)) {
				return overloadedMethodCache[key];
			}

			var methodInfo = T.CGetMethod(name, bindingFlags);
			overloadedMethodCache[key] = methodInfo;
			return methodInfo;
		}

		public static T InvokeMethod<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags, params object[] args)
		{
			var type = instance.GetType();
			return (T)CGetMethod(type, name, bindingFlags).Invoke(instance, args);
		}

		public static T InvokeMethod<T>(this object instance, string name, Type[] argumentTypes, BindingFlags bindingFlags = _bindingFlags, params object[] args)
		{
			var type = instance.GetType();
			return (T)CGetMethod(type, name, argumentTypes, bindingFlags).Invoke(instance, args);
		}


		public static PropertyInfo CGetProperty<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return CGetProperty(typeof(T), name, bindingFlags);
		}

		public static PropertyInfo CGetProperty(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new KeyValuePair<Type, string>(T, name);
			if (propertyCache.ContainsKey(key)) {
				return propertyCache[key];
			}

			var propertyInfo = T.CGetProperty(name, bindingFlags);
			propertyCache[key] = propertyInfo;
			return propertyInfo;
		}

		public static T CGetPropertyValue<T>(this object instance, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			return (T)CGetProperty(type, name, bindingFlags).GetValue(instance);
		}

		public static void CSetPropertyValue<T>(this object instance, string name, T value, BindingFlags bindingFlags = _bindingFlags)
		{
			var type = instance.GetType();
			CGetProperty(type, name, bindingFlags).SetValue(instance, value);
		}
	}
}
