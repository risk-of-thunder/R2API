using HG.BlendableTypes;
using IL.RoR2.Networking;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// This class is obsolete, as light replacements are now a vanilla feature. You can find it inside <see cref="SkinDefParams.lightReplacements"/>.
/// </summary>
[Obsolete("This class never worked. SkinLightReplacements are now a vanilla feature within the Skin system's SkinDefParams object.")]
public static class SkinLightReplacement
{
    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <returns>true</returns>
    public static bool AddLightReplacement(SkinDef targetSkinDef, params LightReplacement[] lightReplacements)
    {
        return true;
    }
}

/// <summary>
/// Struct that represents a LightReplacement for a Skin
/// </summary>
[Serializable, Obsolete("This struct is obsolete, you can utilize a SkinDefParams instead to specify light replacements.")]
public struct LightReplacement
{
    //Tooltips work as documentation as well
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Tooltip("The light which we will modify it's color")]
    [PrefabReference]
    public Light light;
    [Tooltip($"The new color for the light")]
    public Color color;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
