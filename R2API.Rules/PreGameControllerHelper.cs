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
        bool requirementsMet = false;
        foreach (UnlockableDef unlockableDef in unlockables)
        {
            requirementsMet = PreGameController.AnyUserHasUnlockable(unlockableDef);
        }
        return requirementsMet;
    }

    public static bool AreAllExpansionsActive(PreGameController controllerInstance, IEnumerable<ExpansionDef> expansionDefs)
    {
        IEnumerable<RuleChoiceDef> rules = expansionDefs.Select(expansion => expansion.enabledChoice);
        return AreAllChoicesActive(controllerInstance, rules);
    }
    public static bool AreAllChoicesActive(PreGameController controllerInstance, IEnumerable<RuleChoiceDef> ruleChoices)
    {
        bool requirementsMet = false;
        if (controllerInstance.readOnlyRuleBook == null)
            return requirementsMet;

        var ruleBookInstance = controllerInstance.readOnlyRuleBook;
        foreach(RuleChoiceDef choiceDef in ruleChoices)
        {
            requirementsMet = ruleBookInstance.IsChoiceActive(choiceDef);
        }
        return requirementsMet;
    }

    public static bool AnyUserHasAllEntitlements(IEnumerable<EntitlementDef> entitlementDefs, bool checkNetworkInsteadOfLocal)
    {
        bool requirementsMet = false;

        foreach(EntitlementDef entitlementDef in entitlementDefs)
        {
            requirementsMet = checkNetworkInsteadOfLocal ? EntitlementManager.networkUserEntitlementTracker.AnyUserHasEntitlement(entitlementDef) : EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(entitlementDef);
        }
        return requirementsMet;
    }
}
