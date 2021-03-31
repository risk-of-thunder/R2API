using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace R2API.Patcher {
    public static class Patcher {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        private static readonly ManualLogSource Log = Logger.CreateLogSource("R2API");

        private const string SearchableAttributeTypeFullName = "RoR2.SearchableAttribute";

        private static readonly HashSet<string> AssemblyBlacklist = new HashSet<string> {
            "BepInEx",
            "BepInEx.Preloader",
            "BepInEx.Harmony",
            "BepInEx.MonoMod.Loader",
            "BepInEx.MonoMod.HookGenPatcher",
            "0Harmony",
            "Mono.Cecil",
            "Mono.Cecil.Pdb",
            "Mono.Cecil.Mdb",
            "MonoMod",
            "MonoMod.RuntimeDetour",
            "MonoMod.RuntimeDetour.HarmonySharedState",
            "MonoMod.RuntimeDetour.HookGen",
            "MonoMod.Utils",
            "MonoMod.Utils.GetManagedSizeHelper",
            "MonoMod.Utils.Cil.ILGeneratorProxy",
            "R2API",
            "MMHOOK_Assembly-CSharp",
        };

        public static void Patch(AssemblyDefinition assembly) {
            try {
                var module = assembly.MainModule;
                var searchableAttributeType = module.GetType(SearchableAttributeTypeFullName);

                var cctor = searchableAttributeType.Methods.First(md => md.Name == ".cctor");
                var il = cctor.Body.GetILProcessor();

                var hashSetStringAddInstruction = cctor.Body.Instructions.First(
                    i => i.OpCode == OpCodes.Callvirt &&
                ((MethodReference)i.Operand).FullName.Contains("HashSet`1<System.String>::Add(!0)"));

                var hashSetStringAddMethod = (MethodReference)hashSetStringAddInstruction.Operand;

                var targetPopInstructionInstruction = hashSetStringAddInstruction.Next;

                foreach (var assemblyName in AssemblyBlacklist) {
                    var dupInstruction = Instruction.Create(OpCodes.Dup);
                    il.InsertAfter(targetPopInstructionInstruction, dupInstruction);

                    var ldstrInstruction = Instruction.Create(OpCodes.Ldstr, assemblyName);
                    il.InsertAfter(dupInstruction, ldstrInstruction);

                    var callvirtInstruction = Instruction.Create(OpCodes.Callvirt, hashSetStringAddMethod);
                    il.InsertAfter(ldstrInstruction, callvirtInstruction);

                    var popInstruction = Instruction.Create(OpCodes.Pop);
                    il.InsertAfter(callvirtInstruction, popInstruction);
                }

                module.Types.Add(new TypeDefinition("R2API", "SearchableAttributeScanFix", TypeAttributes.Class, module.ImportReference(typeof(object))));
            }
            catch (Exception ex) {
                Log.LogError($"Failed to patch {SearchableAttributeTypeFullName}\n{ex}");
            }
        }
    }
}
