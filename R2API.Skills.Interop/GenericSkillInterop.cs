using System;
using System.Runtime.CompilerServices;
using RoR2;

[assembly:InternalsVisibleTo("R2API.Skills")]
namespace R2API.Skills.Interop;

public static class GenericSkillInterop
{
    public static bool GetHideInCharacterSelectIfFirstSkillSelected(GenericSkill genericSkill) => genericSkill.r2api_hideInCharacterSelectIfFirstSkillSelected;
    public static void SetHideInCharacterSelectIfFirstSkillSelected(GenericSkill genericSkill, bool value) => genericSkill.r2api_hideInCharacterSelectIfFirstSkillSelected = value;
    public static int GetOrderPriority(GenericSkill genericSkill) => genericSkill.r2api_orderPriority;
    public static void SetOrderPriority(GenericSkill genericSkill, int value) => genericSkill.r2api_orderPriority = value;
}
