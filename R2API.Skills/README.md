# R2API.Skills

## Use Cases / Features

Adds extra fields (can be accessed with extension methods) to GenericSkill that controls its appearance in character selection screen:
* Hide a GenericSkill in skills tab when first skill is selected (for example a passive selection where you can select an empty skill that does nothing)
* Set a GenericSkill priority to reorder the rows in skills and loadout tabs.

Adds extra fields (can be accessed with extension methods) to SkillDef:
* GetBonusStockMultiplier and SetBonusStockMultiplier (bonus stock multiplier for this skill)
* GetCooldownRefreshSound and SetCooldownRefreshSound (custom cooldown refresh sound for this skill)

## Changelog

### `1.0.6
* Obsoleted Hide In Loadout and Title Token Override since they got added to vanilla
* Fixed edgecase of partially applied hooks leading to broken menus

### '1.0.5'
* Update for DLC 4 release 1

### '1.0.4'
* Add GetCooldownRefreshSound and SetCooldownRefreshSound to SkillDef

### '1.0.3'
* Remake bonusStockMultiplier logic

### '1.0.2'
* Add GetBonusStockMultiplier and SetBonusStockMultiplier to SkillDef

### '1.0.1'
* Fix an incompatibility with another mod

### '1.0.0'
* Initial Release
