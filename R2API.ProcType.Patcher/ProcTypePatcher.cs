using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using System.Collections.Generic;
using System.Net;

namespace R2API;

public static class ProcTypePatcher
{
    private static readonly ManualLogSource logger = Logger.CreateLogSource("ProcTypePatcher");

    public static IEnumerable<string> TargetDLLs
    {
        get
        {
            yield return "RoR2.dll";
        }
    }

    public static void Patch(AssemblyDefinition assembly)
    {
        logger.LogInfo("Patching!");
        TypeDefinition procChainMask = assembly.MainModule.GetType("RoR2", "ProcChainMask");
        logger.LogInfo(procChainMask.FullName);
        procChainMask.Fields.Add(new FieldDefinition("r2api_moddedMask", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool[]))));
        logger.LogInfo("Done!");
    }
}
