using RoR2;

namespace R2API;

/// <summary>
/// Provides a list of names that correspond to each <see cref="CombatDirector.EliteTierDef"/> in the <see cref="EliteAPI.VanillaEliteTiers"/> array
/// </summary>
public enum VanillaEliteTier : int
{
    /// <summary> No Elites </summary>
    None = 0,
    /// <summary> Stages 1-2 </summary>
    BaseTier1 = 1,
    /// <summary> Stages 1-2 with <see cref="SpawnCard.EliteRules.ArtifactOnly"/> </summary>
    BaseTier1Honor = 2,
    /// <summary> All <see cref="BaseTier1Honor"/> elites plus <see cref="DLC2Content.Elites.AurelioniteHonor"/>, Stages 3+ with <see cref="SpawnCard.EliteRules.ArtifactOnly"/> </summary>
    FullTier1Honor = 3,
    /// <summary> All <see cref="BaseTier1"/> elites plus <see cref="DLC2Content.Elites.Aurelionite"/>, Stages 3+ </summary>
    FullTier1 = 4,
    /// <summary> When <see cref="Run.stageClearCount"/> is greater or equal to <see cref="Run.stagesPerLoop"/> </summary>
    Tier2 = 5,
    /// <summary> Only for <see cref="SpawnCard.EliteRules.Lunar"/> </summary>
    Lunar = 6
}
