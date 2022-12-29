using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace R2API;

/// <summary>
/// Extensions that the Rules submodule uses
/// </summary>
public static class RulesExtensions
{
    /// <summary>
    /// Adds a new ExtendedRuleChoiceDef to a RuleDef
    /// </summary>
    /// <param name="ruleDef">The RuleDef being modified</param>
    /// <param name="choiceName">The Rulechoice's name</param>
    /// <param name="extraData">Extra data that's passed into the ExtendedRuleChoiceDef</param>
    /// <param name="excludeByDefault">Wether this rulechoiceDef is excluded by default</param>
    /// <returns>The newely created and added ExtendedRuleChoiceDef</returns>
    public static ExtendedRuleChoiceDef AddExtendedRuleChoiceDef(this RuleDef ruleDef, string choiceName, object extraData = null, bool excludeByDefault = false)
    {
        RuleCatalogExtras.SetHooks();

        ExtendedRuleChoiceDef extendedRuleChoiceDef = new ExtendedRuleChoiceDef();
        extendedRuleChoiceDef.ruleDef = ruleDef;
        extendedRuleChoiceDef.localName = choiceName;
        extendedRuleChoiceDef.globalName = ruleDef.globalName + "." + choiceName;
        extendedRuleChoiceDef.extraData = extraData;
        extendedRuleChoiceDef.excludeByDefault = excludeByDefault;
        ruleDef.choices.Add(extendedRuleChoiceDef);
        return extendedRuleChoiceDef;
    }

    /// <summary>
    /// Attempts to cast a RuleChoiceDef into an ExtendedRulechoiceDef
    /// </summary>
    /// <param name="choice">The RuleChoiceDef to attempt to cast</param>
    /// <param name="extendedRuleChoiceDef">The casted RuleChoiceDef, null if the returned value is false</param>
    /// <returns>True if the cast was succesful, false otherwise.</returns>
    public static bool TryCastToExtendedRuleChoiceDef(this RuleChoiceDef choice, out ExtendedRuleChoiceDef extendedRuleChoiceDef)
    {
        RuleCatalogExtras.SetHooks();

        if(choice is ExtendedRuleChoiceDef ercd)
        {
            extendedRuleChoiceDef = ercd;
            return true;
        }
        extendedRuleChoiceDef = null;
        return false;
    }
}
