using RoR2;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("R2API.CharacterBody")]

namespace R2API;

internal static class CharacterBodyInterop
{
    public static byte[] GetModdedBodyFlags(CharacterBody characterBody) => characterBody.r2api_moddedBodyFlags;
    public static void SetModdedBodyFlags(CharacterBody characterBody, byte[] value) => characterBody.r2api_moddedBodyFlags = value;
    // TODO: Implement this later
    /*
    public static float GetPrimarySkillDamageAddition(CharacterBody characterBody) => characterBody.r2api_primarySkillDamageAddition;
    public static void SetPrimarySkillDamageAddition(CharacterBody characterBody, float value) => characterBody.r2api_primarySkillDamageAddition = value;
    public static float GetPrimarySkillDamageMultiplier(CharacterBody characterBody) => characterBody.r2api_primarySkillDamageMultiplier;
    public static void SetPrimarySkillDamageMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_primarySkillDamageMultiplier = value;
    public static float GetSecondarySkillDamageAddition(CharacterBody characterBody) => characterBody.r2api_secondarySkillDamageAddition;
    public static void SetSecondarySkillDamageAddition(CharacterBody characterBody, float value) => characterBody.r2api_secondarySkillDamageAddition = value;
    public static float GetSecondarySkillDamageMultiplier(CharacterBody characterBody) => characterBody.r2api_secondarySkillDamageMultiplier;
    public static void SetSecondarySkillDamageMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_secondarySkillDamageMultiplier = value;
    public static float GetUtilitySkillDamageAddition(CharacterBody characterBody) => characterBody.r2api_utilitySkillDamageAddition;
    public static void SetUtilitySkillDamageAddition(CharacterBody characterBody, float value) => characterBody.r2api_utilitySkillDamageAddition = value;
    public static float GetUtilitySkillDamageMultiplier(CharacterBody characterBody) => characterBody.r2api_utilitySkillDamageMultiplier;
    public static void SetUtilitySkillDamageMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_utilitySkillDamageMultiplier = value;
    public static float GetSpecialSkillDamageAddition(CharacterBody characterBody) => characterBody.r2api_specialSkillDamageAddition;
    public static void SetSpecialSkillDamageAddition(CharacterBody characterBody, float value) => characterBody.r2api_specialSkillDamageAddition = value;
    public static float GetSpecialSkillDamageMultiplier(CharacterBody characterBody) => characterBody.r2api_specialSkillDamageMultiplier;
    public static void SetSpecialSkillDamageMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_specialSkillDamageMultiplier = value;
    public static float GetEquipmentDamageAddition(CharacterBody characterBody) => characterBody.r2api_equipmentDamageAddition;
    public static void SetEquipmentDamageAddition(CharacterBody characterBody, float value) => characterBody.r2api_equipmentDamageAddition = value;
    public static float GetEquipmentDamageMultiplier(CharacterBody characterBody) => characterBody.r2api_equipmentDamageMultiplier;
    public static void SetEquipmentDamageMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_equipmentDamageMultiplier = value;
    public static float GetDOTDamageAddition(CharacterBody characterBody) => characterBody.r2api_dotDamageAddition;
    public static void SetDOTDamageAddition(CharacterBody characterBody, float value) => characterBody.r2api_dotDamageAddition = value;
    public static float GetDOTDamageMultiplier(CharacterBody characterBody) => characterBody.r2api_dotDamageMultiplier;
    public static void SetDOTDamageMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_dotDamageMultiplier = value;
    public static float GetHazardDamageAddition(CharacterBody characterBody) => characterBody.r2api_hazardDamageAddition;
    public static void SetHazardDamageAddition(CharacterBody characterBody, float value) => characterBody.r2api_hazardDamageAddition = value;
    public static float GetHazardDamageMultiplier(CharacterBody characterBody) => characterBody.r2api_hazardDamageMultiplier;
    public static void SetHazardDamageMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_hazardDamageMultiplier = value;
    public static float GetPrimarySkillVulnerabilityAddition(CharacterBody characterBody) => characterBody.r2api_primarySkillVulnerabilityAddition;
    public static void SetPrimarySkillVulnerabilityAddition(CharacterBody characterBody, float value) => characterBody.r2api_primarySkillVulnerabilityAddition = value;
    public static float GetPrimarySkillVulnerabilityMultiplier(CharacterBody characterBody) => characterBody.r2api_primarySkillVulnerabilityMultiplier;
    public static void SetPrimarySkillVulnerabilityMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_primarySkillVulnerabilityMultiplier = value;
    public static float GetSecondarySkillVulnerabilityAddition(CharacterBody characterBody) => characterBody.r2api_secondarySkillVulnerabilityAddition;
    public static void SetSecondarySkillVulnerabilityAddition(CharacterBody characterBody, float value) => characterBody.r2api_secondarySkillVulnerabilityAddition = value;
    public static float GetSecondarySkillVulnerabilityMultiplier(CharacterBody characterBody) => characterBody.r2api_secondarySkillVulnerabilityMultiplier;
    public static void SetSecondarySkillVulnerabilityMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_secondarySkillVulnerabilityMultiplier = value;
    public static float GetUtilitySkillVulnerabilityAddition(CharacterBody characterBody) => characterBody.r2api_utilitySkillVulnerabilityAddition;
    public static void SetUtilitySkillVulnerabilityAddition(CharacterBody characterBody, float value) => characterBody.r2api_utilitySkillVulnerabilityAddition = value;
    public static float GetUtilitySkillVulnerabilityMultiplier(CharacterBody characterBody) => characterBody.r2api_utilitySkillVulnerabilityMultiplier;
    public static void SetUtilitySkillVulnerabilityMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_utilitySkillVulnerabilityMultiplier = value;
    public static float GetSpecialSkillVulnerabilityAddition(CharacterBody characterBody) => characterBody.r2api_specialSkillVulnerabilityAddition;
    public static void SetSpecialSkillVulnerabilityAddition(CharacterBody characterBody, float value) => characterBody.r2api_specialSkillVulnerabilityAddition = value;
    public static float GetSpecialSkillVulnerabilityMultiplier(CharacterBody characterBody) => characterBody.r2api_specialSkillVulnerabilityMultiplier;
    public static void SetSpecialSkillVulnerabilityMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_specialSkillVulnerabilityMultiplier = value;
    public static float GetEquipmentVulnerabilityAddition(CharacterBody characterBody) => characterBody.r2api_equipmentVulnerabilityAddition;
    public static void SetEquipmentVulnerabilityAddition(CharacterBody characterBody, float value) => characterBody.r2api_equipmentVulnerabilityAddition = value;
    public static float GetEquipmentVulnerabilityMultiplier(CharacterBody characterBody) => characterBody.r2api_equipmentVulnerabilityMultiplier;
    public static void SetEquipmentVulnerabilityMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_equipmentVulnerabilityMultiplier = value;
    public static float GetDOTVulnerabilityAddition(CharacterBody characterBody) => characterBody.r2api_dotVulnerabilityAddition;
    public static void SetDOTVulnerabilityAddition(CharacterBody characterBody, float value) => characterBody.r2api_dotVulnerabilityAddition = value;
    public static float GetDOTVulnerabilityMultiplier(CharacterBody characterBody) => characterBody.r2api_dotVulnerabilityMultiplier;
    public static void SetDOTVulnerabilityMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_dotVulnerabilityMultiplier = value;
    public static float GetHazardVulnerabilityAddition(CharacterBody characterBody) => characterBody.r2api_hazardVulnerabilityAddition;
    public static void SetHazardVulnerabilityAddition(CharacterBody characterBody, float value) => characterBody.r2api_hazardVulnerabilityAddition = value;
    public static float GetHazardVulnerabilityMultiplier(CharacterBody characterBody) => characterBody.r2api_hazardVulnerabilityMultiplier;
    public static void SetHazardVulnerabilityMultiplier(CharacterBody characterBody, float value) => characterBody.r2api_hazardVulnerabilityMultiplier = value;*/
}
