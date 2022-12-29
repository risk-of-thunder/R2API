using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace R2API;

/// <summary>
/// Represents an Extended version of a RuleChoiceDef
/// Unlike regular RuleChoiceDefs, an ExtendedRulechoiceDef can have multiple requirements, such as multiple expansions or multiple unlockables required.
/// <para>Utilize these collections instead of the singular requirements</para>
/// </summary>
public class ExtendedRuleChoiceDef : RuleChoiceDef
{
    /// <summary>
    /// The Unlockables required for this rule choice to be enabled
    /// </summary>
    public List<UnlockableDef> requiredUnlockables = new List<UnlockableDef>();

    /// <summary>
    /// The RuleChoiceDefs that need to be enabled for this rule choice to be enabled
    /// </summary>
    public List<RuleChoiceDef> requiredChoiceDefs = new List<RuleChoiceDef>();

    /// <summary>
    /// The EntitlementDefs that the players in the lobby need to have for this rule choice to be enabled
    /// </summary>
    public List<EntitlementDef> requiredEntitlementDefs = new List<EntitlementDef>();

    /// <summary>
    /// The ExpansionDefs that need to be enabled for this rule choice to be enabled.
    /// </summary>
    public List<ExpansionDef> requiredExpansionDefs = new List<ExpansionDef>();
}
