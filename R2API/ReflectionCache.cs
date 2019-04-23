using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace R2API
{
	public static class ReflectionCache
	{
		private static readonly Dictionary<Tuple<Type, string>, FieldInfo> fieldCache = new Dictionary<Tuple<Type, string>, FieldInfo>();
		private static readonly Dictionary<Tuple<Type, string>, MethodInfo> methodCache = new Dictionary<Tuple<Type, string>, MethodInfo>();
		private static readonly Dictionary<Tuple<Type, string, Type[]>, MethodInfo> overloadedMethodCache = new Dictionary<Tuple<Type, string, Type[]>, MethodInfo>();
		private static readonly Dictionary<Tuple<Type, string>, PropertyInfo> propertyCache = new Dictionary<Tuple<Type, string>, PropertyInfo>();

		private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

		public static FieldInfo GetFieldCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetFieldCached(typeof(T), name, bindingFlags);
		}

		public static FieldInfo GetFieldCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new Tuple<Type, string>(T, name);
			if (fieldCache.ContainsKey(key)) {
				return fieldCache[key];
			}

			var fieldInfo = T.GetField(name, bindingFlags);
			fieldCache[key] = fieldInfo;
			return fieldInfo;
		}

		public static MethodInfo GetMethodCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetMethodCached(typeof(T), name, bindingFlags);
		}

		public static MethodInfo GetMethodCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new Tuple<Type, string>(T, name);
			if (methodCache.ContainsKey(key)) {
				return methodCache[key];
			}

			var methodInfo = T.GetMethod(name, bindingFlags);
			methodCache[key] = methodInfo;
			return methodInfo;
		}

		public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new Tuple<Type, string, Type[]>(T, name, argumentTypes);
			if (overloadedMethodCache.ContainsKey(key)) {
				return overloadedMethodCache[key];
			}

			var methodInfo = T.GetMethod(name, bindingFlags);
			overloadedMethodCache[key] = methodInfo;
			return methodInfo;
		}

		public static PropertyInfo GetPropertyCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetPropertyCached(typeof(T), name, bindingFlags);
		}

		public static PropertyInfo GetPropertyCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			var key = new Tuple<Type, string>(T, name);
			if (propertyCache.ContainsKey(key)) {
				return propertyCache[key];
			}

			var propertyInfo = T.GetProperty(name, bindingFlags);
			propertyCache[key] = propertyInfo;
			return propertyInfo;
		}
	}
}
