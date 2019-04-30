using EntityStates;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace R2API
{
	public static class EntityAPI
	{
		public static void InitHooks()
		{
			var detour = new Hook(
				typeof(SerializableEntityStateType).GetMethod("set_stateType", BindingFlags.Public | BindingFlags.Instance),
				typeof(EntityAPI).GetMethod(nameof(set_stateType_Hook), BindingFlags.Public | BindingFlags.Static)
			);

			detour.Apply();
		}


		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void set_stateType_Hook(ref SerializableEntityStateType self, Type value)
		{
			var typeName = typeof(SerializableEntityStateType).GetFieldCached("_typeName");
			typeName.SetValue(self,
				value != null && value.IsSubclassOf(typeof(EntityState)) ? value.AssemblyQualifiedName : "");
		}
	}
}