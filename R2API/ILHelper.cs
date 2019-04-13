using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static void RunVSCode(string file) {
			var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");
			var paths = enviromentPath.Split(';');
			var exePath = paths.Select(x => Path.Combine(x, "code.cmd"))
							   .Where(x => File.Exists(x))
							   .FirstOrDefault();


			if (string.IsNullOrWhiteSpace(exePath) == false) {
				var process = new System.Diagnostics.ProcessStartInfo(exePath) {
					Arguments = $"\"{file}\""
				};
				System.Diagnostics.Process.Start(process);
			}
		}

		public static void Log(this List<string> things, bool openVsCode = false) {
			var dir = Directory.GetCurrentDirectory();
			var file = Path.Combine(dir, $"r2api.debug.{DateTime.Now.ToFileTime()}.txt");
			Debug.Log("[R2API]: Debug file at: " + file);
			File.WriteAllLines(file, things);
			if (openVsCode) {
				RunVSCode(file);
			}
		}

		public static void Log(this string thing, bool openVsCode = false) {
			var dir = Directory.GetCurrentDirectory();
			var file = Path.Combine(dir, $"r2api.debug.{DateTime.Now.ToFileTime()}.log");
			Debug.Log("[R2API]: Debug file at: " + file);
			File.WriteAllText(file, thing);
			if (openVsCode) {
				RunVSCode(file);
			}
		}

		public static List<string> PrintInstrs(this ILContext il, Action<string> action = null)
		{
			var instructions = new List<string>();

			if (action == null) {
				action = Debug.Log;
			}

			var start = 0;
			foreach (var inst in il.Instrs) {
				var str = $"{start++,3} {inst.OpCode.ToString()} {(inst.Operand != null ? inst.Operand.ToString() : "")}";
				instructions.Add(str);

				action?.Invoke(str);
			}

			return instructions;
		}
	}
}
