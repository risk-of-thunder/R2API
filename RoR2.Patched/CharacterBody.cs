using System;
using System.Collections.Generic;
using System.Text;

namespace RoR2;
public class CharacterBody
{
    public byte[] r2api_moddedBodyFlags;
    public Dictionary<string, float> r2api_damageSourceDamageMultiplier;
    public Dictionary<string, float> r2api_damageSourceDamageAddition;
    public Dictionary<string, float> r2api_damageSourceVulnerabilityMultiplier;
    public Dictionary<string, float> r2api_damageSourceVulnerabilityAddition;
}
