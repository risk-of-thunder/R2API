# R2API.Unlockable - Creation of Unlockables, Achievement and Tying them together.

## About

R2API.Unlockable is a submodule assembly for R2API that allows mod creators to add new Unlockables and Achievements to the game. These can later be used to gate certain content pieces from your mods so that an achievement needs to be unlocked before you can get to use a specific content (such as an item)

## Use Cases / Features

R2API.Unlockable is mainly used for adding Unlockables that get unlocked after obtaining an AchievementDef, please note that unlocks such as stage / monsters log books should not be added with R2API.Unlockable but rather with `R2API.ContentAddition`

A valid unlockable and achievement pair is defined by a class that inherits from both BaseAchievement and IModdedUnlockableDataProvider, which gives enough data to create both the UnlockableDef and AchievementDef.

## Related Pages

## Changelog

### '1.0.2'
* Add missing `BepInDependency` to `R2API.ContentManagement`

### '1.0.1'
* Fix the NuGet package which had a dependency on a non-existent version of `R2API.Core`.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
