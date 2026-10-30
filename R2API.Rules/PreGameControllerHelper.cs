using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace R2API;

/// <summary>
/// A class with helper methods that are used for the Rules submodule
/// </summary>
internal static class PreGameControllerHelper
{
    public static bool AnyUserHasAllUnlockables(IEnumerable<UnlockableDef> unlockables)
    {
        foreach (UnlockableDef unlockableDef in unlockables)
        {
            if(PreGameController.AnyUserHasUnlockable(unlockableDef) == false)
            {
                return false;
            }
        }
        return true;
    }

    public static bool AreAllExpansionsActive(PreGameController controllerInstance, IEnumerable<ExpansionDef> expansionDefs)
    {
        IEnumerable<RuleChoiceDef> rules = expansionDefs.Select(expansion => expansion.enabledChoice);
        return AreAllChoicesActive(controllerInstance, rules);
    }
    public static bool AreAllChoicesActive(PreGameController controllerInstance, IEnumerable<RuleChoiceDef> ruleChoices)
    {
        if (controllerInstance.readOnlyRuleBook == null)
            return false;

        var ruleBookInstance = controllerInstance.readOnlyRuleBook;
        foreach (RuleChoiceDef choiceDef in ruleChoices)
        {
            if(ruleBookInstance.IsChoiceActive(choiceDef) == false)
            {
                return false;
            }
        }
        return true;
    }

    public static bool AnyUserHasAllEntitlements(IEnumerable<EntitlementDef> entitlementDefs, bool checkNetworkInsteadOfLocal)
    {
        foreach (EntitlementDef entitlementDef in entitlementDefs)
        {
            bool requirementMet = checkNetworkInsteadOfLocal ? EntitlementManager.networkUserEntitlementTracker.AnyUserHasEntitlement(entitlementDef) : EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(entitlementDef);
            if(requirementMet == false)
            {
                return false;
            }
        }
        return true;
    }
}
