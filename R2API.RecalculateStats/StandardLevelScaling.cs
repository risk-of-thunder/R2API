namespace R2API;

/// <summary>
/// A collection of standard level scaling values, for use with <see cref="RecalculateStatsAPI"/>.
/// </summary>
public static class StandardLevelScaling
{
    /// <summary>The standard level scaling coefficient for base health bonuses. Based on <see cref="RoR2.CharacterBody.PerformAutoCalculateLevelStats"/></summary>
    public const float Health = 0.3f;

    /// <summary>The standard level scaling coefficient for base shield bonuses. Based on <see cref="RoR2.CharacterBody.PerformAutoCalculateLevelStats"/></summary>
    public const float Shield = 0.3f;

    /// <summary>The standard level scaling coefficient for base health regeneration bonuses. Based on <see cref="RoR2.CharacterBody.RecalculateStats"/></summary>
    public const float Regen = 0.2f;

    /// <summary>The standard level scaling coefficient for base damage bonuses. Based on <see cref="RoR2.CharacterBody.PerformAutoCalculateLevelStats"/></summary>
    public const float Damage = 0.2f;
}
