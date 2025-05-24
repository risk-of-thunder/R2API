using System;
using System.Runtime.CompilerServices;
using RoR2;

[assembly:InternalsVisibleTo("R2API.Skills")]
namespace R2API.Skills.Interop;

public static class SkillDefInterop
{
    public static int GetBonusStockMultiplier(SkillDef skillDef) => skillDef.r2api_bonusStockMultiplier;
}
