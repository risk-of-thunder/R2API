using RoR2;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("R2API.CharacterBody")]

namespace R2API;

internal static class CharacterBodyInterop
{
    public static byte[] GetModdedBodyFlags(CharacterBody characterBody) => characterBody.r2api_moddedBodyFlags;
    public static void SetModdedBodyFlags(CharacterBody characterBody, byte[] value) => characterBody.r2api_moddedBodyFlags = value;
    public static float GetDamageSourceDamageMultiplier(CharacterBody characterBody, string value)
    {
        if (characterBody.r2api_damageSourceDamageMultiplier == null) characterBody.r2api_damageSourceDamageMultiplier = new System.Collections.Generic.Dictionary<string, float>();
        float output = 1f;
        if (value == "SkillMask")
        {
            output += GetDamageSourceDamageMultiplier(characterBody, "Primary") - 1f;
            output += GetDamageSourceDamageMultiplier(characterBody, "Secondary") - 1f;
            output += GetDamageSourceDamageMultiplier(characterBody, "Utility") - 1f;
            output += GetDamageSourceDamageMultiplier(characterBody, "Special") - 1f;
            return output;
        }
        if (characterBody.r2api_damageSourceDamageMultiplier.ContainsKey(value))
        {
            output = characterBody.r2api_damageSourceDamageMultiplier[value];
        }
        if (value == "Primary" || value == "Secondary" || value == "Utility" || value == "Special")
        {
            output += GetDamageSourceDamageMultiplier(characterBody, "SkillMaskOverride") - 1f;
        }
        return output;
    }
    public static void SetDamageSourceDamageMultiplier(CharacterBody characterBody, string value, float value2)
    {
        if (characterBody.r2api_damageSourceDamageMultiplier == null) characterBody.r2api_damageSourceDamageMultiplier = new System.Collections.Generic.Dictionary<string, float>();
        if (value == "SkillMask")
        {
            SetDamageSourceDamageMultiplier(characterBody, "SkillMaskOverride", value2);
            return;
        }
        if (characterBody.r2api_damageSourceDamageMultiplier.ContainsKey(value))
        {
            characterBody.r2api_damageSourceDamageMultiplier[value] = value2;
        }
        else
        {
            characterBody.r2api_damageSourceDamageMultiplier.Add(value, value2);
        }
    }
    public static float GetDamageSourceDamageAddition(CharacterBody characterBody, string value)
    {
        if (characterBody.r2api_damageSourceDamageAddition == null) characterBody.r2api_damageSourceDamageAddition = new System.Collections.Generic.Dictionary<string, float>();
        float output = 0f;
        if (value == "SkillMask")
        {
            output += GetDamageSourceDamageMultiplier(characterBody, "Primary");
            output += GetDamageSourceDamageMultiplier(characterBody, "Secondary");
            output += GetDamageSourceDamageMultiplier(characterBody, "Utility");
            output += GetDamageSourceDamageMultiplier(characterBody, "Special");
            return output;
        }
        if (characterBody.r2api_damageSourceDamageAddition.ContainsKey(value))
        {
            output = characterBody.r2api_damageSourceDamageAddition[value];
        }
        if (value == "Primary" || value == "Secondary" || value == "Utility" || value == "Special")
        {
            output += GetDamageSourceDamageAddition(characterBody, "SkillMaskOverride");
        }
        return output;
    }
    public static void SetDamageSourceDamageAddition(CharacterBody characterBody, string value, float value2)
    {
        if (characterBody.r2api_damageSourceDamageAddition == null) characterBody.r2api_damageSourceDamageAddition = new System.Collections.Generic.Dictionary<string, float>();
        if (value == "SkillMask")
        {
            SetDamageSourceDamageAddition(characterBody, "SkillMaskOverride", value2);
            return;
        }
        if (characterBody.r2api_damageSourceDamageAddition.ContainsKey(value))
        {
            characterBody.r2api_damageSourceDamageAddition[value] = value2;
        }
        else
        {
            characterBody.r2api_damageSourceDamageAddition.Add(value, value2);
        }
    }
    public static float GetDamageSourceVulnerabilityMultiplier(CharacterBody characterBody, string value)
    {
        if (characterBody.r2api_damageSourceVulnerabilityMultiplier == null) characterBody.r2api_damageSourceVulnerabilityMultiplier = new System.Collections.Generic.Dictionary<string, float>();
        float output = 1f;
        if (value == "SkillMask")
        {
            output += GetDamageSourceDamageMultiplier(characterBody, "Primary") - 1f;
            output += GetDamageSourceDamageMultiplier(characterBody, "Secondary") - 1f;
            output += GetDamageSourceDamageMultiplier(characterBody, "Utility") - 1f;
            output += GetDamageSourceDamageMultiplier(characterBody, "Special") - 1f;
            return output;
        }
        if (characterBody.r2api_damageSourceVulnerabilityMultiplier.ContainsKey(value))
        {
            output = characterBody.r2api_damageSourceVulnerabilityMultiplier[value];
        }
        if (value == "Primary" || value == "Secondary" || value == "Utility" || value == "Special")
        {
            output += GetDamageSourceVulnerabilityMultiplier(characterBody, "SkillMaskOverride") - 1f;
        }
        return output;
    }
    public static void SetDamageSourceVulnerabilityMultiplier(CharacterBody characterBody, string value, float value2)
    {
        if (characterBody.r2api_damageSourceVulnerabilityMultiplier == null) characterBody.r2api_damageSourceVulnerabilityMultiplier = new System.Collections.Generic.Dictionary<string, float>();
        if (value == "SkillMask")
        {
            SetDamageSourceDamageMultiplier(characterBody, "SkillMaskOverride", value2);
            return;
        }
        if (characterBody.r2api_damageSourceVulnerabilityMultiplier.ContainsKey(value))
        {
            characterBody.r2api_damageSourceVulnerabilityMultiplier[value] = value2;
        }
        else
        {   
            characterBody.r2api_damageSourceVulnerabilityMultiplier.Add(value, value2);
        }
    }
    public static float GetDamageSourceVulnerabilityAddition(CharacterBody characterBody, string value)
    {
        if (characterBody.r2api_damageSourceVulnerabilityAddition == null) characterBody.r2api_damageSourceVulnerabilityAddition = new System.Collections.Generic.Dictionary<string, float>();
        float output = 0f;
        if (value == "SkillMask")
        {
            output += GetDamageSourceDamageMultiplier(characterBody, "Primary");
            output += GetDamageSourceDamageMultiplier(characterBody, "Secondary");
            output += GetDamageSourceDamageMultiplier(characterBody, "Utility");
            output += GetDamageSourceDamageMultiplier(characterBody, "Special");
            return output;
        }
        if (characterBody.r2api_damageSourceVulnerabilityAddition.ContainsKey(value))
        {
            output = characterBody.r2api_damageSourceVulnerabilityAddition[value];
        }
        if (value == "Primary" || value == "Secondary" || value == "Utility" || value == "Special")
        {
            output += GetDamageSourceVulnerabilityAddition(characterBody, "SkillMaskOverride");
        }
        return output;
    }
    public static void SetDamageSourceVulnerabilityAddition(CharacterBody characterBody, string value, float value2)
    {
        if (characterBody.r2api_damageSourceVulnerabilityAddition == null) characterBody.r2api_damageSourceVulnerabilityAddition = new System.Collections.Generic.Dictionary<string, float>();
        if (value == "SkillMask")
        {
            SetDamageSourceDamageAddition(characterBody, "SkillMaskOverride", value2);
            return;
        }
        if (characterBody.r2api_damageSourceVulnerabilityAddition.ContainsKey(value))
        {
            characterBody.r2api_damageSourceVulnerabilityAddition[value] = value2;
        }
        else
        {
            characterBody.r2api_damageSourceVulnerabilityAddition.Add(value, value2);
        }
    }
}

