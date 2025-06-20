using RoR2;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("R2API.Elites")]

namespace R2API;

internal static class ElitesInterop
{
    public static string GetEliteTierDefName(CombatDirector.EliteTierDef eliteTierDef) => eliteTierDef.r2api_name;

    public static void SetEliteTierDefName(CombatDirector.EliteTierDef eliteTierDef, string value) => eliteTierDef.r2api_name = value;
}
