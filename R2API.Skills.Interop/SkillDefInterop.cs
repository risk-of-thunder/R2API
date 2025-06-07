﻿using System;
using System.Runtime.CompilerServices;
using RoR2.Skills;

[assembly:InternalsVisibleTo("R2API.Skills")]
namespace R2API.Skills.Interop;

public static class SkillDefInterop
{
    public static int GetBonusStockMultiplier(SkillDef skillDef) => skillDef.r2api_bonusStockMultiplier;
    public static void SetBonusStockMultiplier(SkillDef skillDef, int value) => skillDef.r2api_bonusStockMultiplier = value;
}
