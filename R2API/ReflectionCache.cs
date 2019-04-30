using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace R2API
{
	public static class ReflectionCache
	{
		private static readonly ConcurrentDictionary<(Type T, string name), FieldInfo> fieldCache =
			new ConcurrentDictionary<(Type, string), FieldInfo>();

		private static readonly ConcurrentDictionary<(Type T, string name), MethodInfo> methodCache =
			new ConcurrentDictionary<(Type, string), MethodInfo>();

		private static readonly ConcurrentDictionary<(Type T, string name, Type[]), MethodInfo> overloadedMethodCache =
			new ConcurrentDictionary<(Type, string, Type[]), MethodInfo>();

		private static readonly ConcurrentDictionary<(Type T, string name), PropertyInfo> propertyCache =
			new ConcurrentDictionary<(Type, string), PropertyInfo>();

		
		private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
		

		public static FieldInfo GetFieldCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetFieldCached(typeof(T), name, bindingFlags);
		}

		public static FieldInfo GetFieldCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return fieldCache.GetOrAdd((T, name), x => x.T.GetField(x.name, bindingFlags));
		}

		public static MethodInfo GetMethodCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetMethodCached(typeof(T), name, bindingFlags);
		}

		public static MethodInfo GetMethodCached(this Type T, string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return methodCache.GetOrAdd((T, name), x => x.T.GetMethod(x.name, bindingFlags));
		}

		public static MethodInfo GetMethodCached(this Type T, string name, Type[] argumentTypes,
			BindingFlags bindingFlags = _bindingFlags)
		{
			return overloadedMethodCache.GetOrAdd((T, name, argumentTypes),
				x => x.T.GetMethod(x.name, bindingFlags, null, argumentTypes, null));
		}

		public static PropertyInfo GetPropertyCached<T>(string name, BindingFlags bindingFlags = _bindingFlags)
		{
			return GetPropertyCached(typeof(T), name, bindingFlags);
		}

		public static PropertyInfo GetPropertyCached(this Type T, string name,
			BindingFlags bindingFlags = _bindingFlags)
		{
			return propertyCache.GetOrAdd((T, name), x => x.T.GetProperty(x.name, bindingFlags));
		}
	}
}