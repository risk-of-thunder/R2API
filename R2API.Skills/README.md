# R2API.Skills

## Use Cases / Features

Adds extra fields (can be accessed with extension methods) to GenericSkill that controls its appearance in character selection screen:
* Hide a GenericSkill in skills tab when first skill is selected (for example a passive selection where you can select an empty skill that does nothing)
* Hide a GenericSkill in loadout tab.
* Set a GenericSkill priority to reorder the rows in skills and loadout tabs.
* Set a title token override to change default one (like `Misc`, `Primary` etc) in loadout tab.

Adds extra fields (can be accessed with extension methods) to SkillDef:
* GetBonusStockMultiplier and SetBonusStockMultiplier (bonus stock multiplier for this skill)

## Changelog

### '1.0.3'
* Add GetBlacklistAmmoPack and SetBlacklistAmmoPack to SkillDef

### '1.0.2'
* Add GetBonusStockMultiplier and SetBonusStockMultiplier to SkillDef

### '1.0.1'
* Fix an incompatibility with another mod

### '1.0.0'
* Initial Release
