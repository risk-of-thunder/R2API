using EntityStates;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace R2API
{
	public static class EntityAPI
	{
		public static void InitHooks()
		{
			var detour = new NativeDetour(
				typeof(SerializableEntityStateType).GetMethod("set_stateType", BindingFlags.Public | BindingFlags.Instance),
				typeof(EntityAPI).GetMethod(nameof(SetStateTypeDetour), BindingFlags.NonPublic | BindingFlags.Static));

			detour.Apply();
		}

		private static void SetStateTypeDetour(SerializableEntityStateType self, Type value)
		{
			var typeName = typeof(SerializableEntityStateType).GetFieldCached("_typeName");
			typeName.SetValue(self, ((value != null && value.IsSubclassOf(typeof(EntityState))) ? value.AssemblyQualifiedName : ""));
		}
	}
}
