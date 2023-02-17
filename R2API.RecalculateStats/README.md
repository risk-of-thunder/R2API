# R2API.RecalculateStats - Global solution for manipulating Character Stats.

## About

R2API.RecalculateStats is a submodule assembly for R2API that allows mod creators to finely tune stat changes for Characters ingame

It is intended to be a global solution for mods in general.

## Use Cases / Features

R2API.RecalculateStats works via the GetStatCoefficients event, which allows a mod creator to run logic and modify the incoming stat changes for a Character.

These stat changes are represented in the StatHookEventArgs, which includes arguments for modifying a variety of stats, these include:

* Max Health
* Health Regeneration
* Movement Speed
* Jump Power
* Damage
* Attack Speed
* Critical Strike Chance
* Armor
* Curse
* Cooldown reduction for skills
* Shield
* Critical Strike Damage

## Related Pages

## Changelog

### '1.2.0'
* Added `levelFlatAdd` stat for changing effective level additively.
* Added `sprintSpeedAdd` stat for changing sprint speed multiplier.

### '1.1.0'
* Added `attackSpeedReductionMultAdd` stat for reducing attack speed.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
