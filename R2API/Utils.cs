using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace R2API.Utils
{
	public static class Reflection
	{
		private static readonly BindingFlags _defaultFlags
			= BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		#region Field

		public static TReturn GetFieldValue<TReturn>(this object instance, string fieldName)
		{
			return (TReturn)instance.GetType()
									.GetField(fieldName, _defaultFlags | BindingFlags.Instance)
									.GetValue(instance);
		}

		public static TReturn GetFieldValue<TClass, TReturn>(string fieldName)
		{
			return (TReturn)typeof(TClass)
							.GetField(fieldName, _defaultFlags | BindingFlags.Static)
							.GetValue(null);
		}

		public static void SetFieldValue(this object instance, string fieldName, object value)
		{
			instance.GetType()
					.GetField(fieldName, _defaultFlags | BindingFlags.Instance)
					.SetValue(instance, value);
		}

		public static void SetFieldValue<TClass>(string fieldName, object value)
		{
			typeof(TClass)
				.GetField(fieldName, _defaultFlags | BindingFlags.Static)
				.SetValue(null, value);
		}

		#endregion

		#region Property

		public static TReturn GetProperyValue<TReturn>(this object instance, string propName)
		{
			return (TReturn)instance.GetType()
									.GetProperty(propName, _defaultFlags | BindingFlags.Instance)
									.GetValue(instance);
		}

		public static TReturn GetProperyValue<TClass, TReturn>(string propName)
		{
			return (TReturn)typeof(TClass)
							.GetProperty(propName, _defaultFlags | BindingFlags.Static)
							.GetValue(null);
		}

		public static void SetProperyValue(this object instance, string propName, object value)
		{
			instance.GetType()
					.GetProperty(propName, _defaultFlags | BindingFlags.Instance)
					.SetValue(instance, value);
		}

		public static void SetProperyValue<TClass>(string propName, object value)
		{
			typeof(TClass).GetProperty(propName, _defaultFlags | BindingFlags.Static)
						  .SetValue(null, value);
		}

		#endregion

		#region Method

		public static TReturn InvokeMethod<TReturn>(this object instance, string methodName, params object[] methodParams)
		{
			return (TReturn)instance.GetType()
									.GetMethod(methodName, _defaultFlags | BindingFlags.Instance)
									.Invoke(instance, methodParams);
		}

		public static TReturn InvokeMethod<TClass, TReturn>(string methodName, params object[] methodParams)
		{
			return (TReturn)typeof(TClass)
							.GetMethod(methodName, _defaultFlags | BindingFlags.Static)
							.Invoke(null, methodParams);
		}

		public static void InvokeMethod(this object instance, string methodName, params object[] methodParams)
		{
			instance.InvokeMethod<object>(methodName, methodParams);
		}

		public static void InvokeMethod<TClass>(string methodName, params object[] methodParams)
		{
			InvokeMethod<TClass, object>(methodName, methodParams);
		}

		#endregion

		#region Class

		public static Type GetNestedType<TParent>(string name)
		{
			return typeof(TParent).GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic);
		}

		public static object Instantiate(this Type type)
		{
			return Activator.CreateInstance(type, true);
		}

		public static object InstantiateGeneric<TClass>(this Type typeArgument)
		{
			return typeof(TClass).MakeGenericType(typeArgument).Instantiate();
		}

		public static object InstantiateGeneric<TClass>(this Type[] typeArgument)
		{
			return typeof(TClass).MakeGenericType(typeArgument).Instantiate();
		}

		public static IList InstantiateList(this Type type)
		{
			return (IList)typeof(List<>).MakeGenericType(type).Instantiate();
		}

		#endregion
	}
}