# R2API.Rules - Creation and Management of custom Rules for the Rule Catalog

## About

R2API.Rules is a submodule assembly for R2API that simplifies and augments the process of interacting with the game's RuleDef system. The RuleDef system is what the game uses in the lobby for deciding what Artifacts, Expansions, and Difficulty will be chosen in the run.

## Use Cases / Features

R2API.Rules can be used for mods to add their own RuleDefs to existing RuleCategoryDefs, or to new RuleCategoryDefs that can be created from code.

The main feature of the Rules submodule is the addition of the ExtendedRuleChoiceDef, an extension of RuleChoiceDef that allows mod creators to specify multiple requirements for a RuleDef to be selectable/enabled in the game. (Such as having multiple ExpansionDefs or UnlockableDef requirements.)

## Related Pages

A Wiki page explaining how the Rule system works and how to create new rules will be added to the R2Wiki Soon(tm)

## Changelog

### '1.0.1'

Fix wrong logger being used

### '1.0.0'

Initial Release
