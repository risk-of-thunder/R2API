using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace R2API
{
	public static class ILHelper
	{
		public static void PrintMethods(this Type T)
		{
			foreach (var method in T.GetMethods()) {
				Debug.Log("M: " + method.Name);
			}

			foreach (var method in T.GetRuntimeMethods()) {
				Debug.Log("RM: " + method.Name);
			}
		}

		public static void PrintInstrs(this ILContext il)
		{
			var start = 0;
			foreach (var inst in il.Instrs) {
				Debug.Log($"{start++,3} {inst.OpCode.ToString()} {(inst.Operand != null ? inst.Operand.ToString() : "")}");
			}
		}
	}
}
