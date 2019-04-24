using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace R2API
{
	public static class ReflectionCache
	{
		private static readonly ConcurrentDictionary<(Type, string), FieldInfo> fieldCache = new ConcurrentDictionary<(Type, string), FieldInfo>();
		private static readonly ConcurrentDictionary<(Type, string), MethodInfo> methodCache = new ConcurrentDictionary<(Type, string), MethodInfo>();
		private static readonly ConcurrentDictionary<(Type, string, Type[]), MethodInfo> overloadedMethodCache = new ConcurrentDictionary<(Type, string, Type[]), MethodInfo>();
		private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> propertyCache = new ConcurrentDictionary<(Type, string), PropertyInfo>();

		private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

		public static FieldInfo GetFieldCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetFieldCached(typeof(T), name, bindingFlags);
		}

		public static FieldInfo GetFieldCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return fieldCache.GetOrAdd((T, name), T.GetField(name, bindingFlags));
		}

		public static MethodInfo GetMethodCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetMethodCached(typeof(T), name, bindingFlags);
		}

		public static MethodInfo GetMethodCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return methodCache.GetOrAdd((T, name), T.GetMethod(name, bindingFlags));
		}

		public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes, BindingFlags bindingFlags = _bindingFlags)
		{
			return overloadedMethodCache.GetOrAdd((T, name, argumentTypes), T.GetMethod(name, bindingFlags, null, argumentTypes, null));
		}

		public static PropertyInfo GetPropertyCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetPropertyCached(typeof(T), name, bindingFlags);
		}

		public static PropertyInfo GetPropertyCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return propertyCache.GetOrAdd((T, name), T.GetProperty(name, bindingFlags));
		}
	}
}
