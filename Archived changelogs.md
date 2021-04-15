All changelogs before 3.0.0 version

**2.5.14**

* Update for 5440050 game build
* [Fix Reply/Request Messages for NetworkingAPI](https://github.com/risk-of-thunder/R2API/pull/206)

**2.5.11**

* [Fixed NetworkAPI double sending messages](https://github.com/risk-of-thunder/R2API/pull/201)
* [Monomod patch detection fix and some refactoring](https://github.com/risk-of-thunder/R2API/pull/202)
* [Added logging for ILHooks usage](https://github.com/risk-of-thunder/R2API/pull/203)
* [LanguageAPI: implemented LanguageOverlays](https://github.com/risk-of-thunder/R2API/pull/204)

**2.5.7**

* [EliteAPI, NetworkcCompatibility fixes](https://github.com/risk-of-thunder/R2API/pull/200)

**2.5.6**

* [Fixed NetworkingAPI initialization](https://github.com/risk-of-thunder/R2API/pull/196)
* [LanguageAPI: First loaded language from file will be added used if here is no localized values for other languages](https://github.com/risk-of-thunder/R2API/pull/197)

**2.5.4**

* [Eclipse fix for modded characters](https://github.com/risk-of-thunder/R2API/pull/190)
* [Fixed an issue where ItemDropAPI allowed items with `WorldUnique` tag be in drop tables](https://github.com/risk-of-thunder/R2API/pull/192) 
* [NetworkCompatibility fixes](https://github.com/risk-of-thunder/R2API/pull/193)
* [LanguageAPI api fix for replacing vanilla tokens](https://github.com/risk-of-thunder/R2API/pull/194)
* [Using non-requested submodules will now throw an error](https://github.com/risk-of-thunder/R2API/pull/195)

**2.5.0**

* Fix things for 1.0 release
* **IMPORTANT FOR MOD DEVS:** [Add NetworkCompatibility Helper](https://github.com/risk-of-thunder/R2API/pull/188)
* [Remove 'MOD' from dedicated server listings as it's already shown in the tags](https://github.com/risk-of-thunder/R2API/pull/150)
* [Fixed bug with ConvertToFullpath by enforcing that ModPrefix start with @](https://github.com/risk-of-thunder/R2API/pull/168)
* [Fixed being unable to have assets with the same path but different type in UnbundledResourceProvider](https://github.com/risk-of-thunder/R2API/pull/167)
* [Added NetworkingAPI now integrating networking components into R2API!](https://github.com/risk-of-thunder/R2API/pull/163)
* [Added DotAPI to handle Dots](https://github.com/risk-of-thunder/R2API/pull/161)
* [Update EliteAPI to spawn correctly](https://github.com/risk-of-thunder/R2API/pull/160)
* [Add UnlockablesAPI](https://github.com/risk-of-thunder/R2API/pull/156)

**2.4.29**

* [disable ConCommand steam_quickplay_start](https://github.com/risk-of-thunder/R2API/pull/154)
* [Add Skymeadow to directorAPI](https://github.com/risk-of-thunder/R2API/pull/153)
* [ItemDropAPI: Allow public removal of items from the default drop lists](https://github.com/risk-of-thunder/R2API/pull/149)
* [Fix SurvivorAPI misidentifies survivors with missing newline characters](https://github.com/risk-of-thunder/R2API/pull/148)
* [Prevent adding XML unsafe items](https://github.com/risk-of-thunder/R2API/pull/146)
* [Fix Custom equipment not displaying on bodies](https://github.com/risk-of-thunder/R2API/pull/144)
* [Converted LoadRequest to use cecil](https://github.com/risk-of-thunder/R2API/pull/143)

**2.4.16**

* [Fix R2API plugin dependency of MonoMod patcher file](https://github.com/risk-of-thunder/R2API/pull/140)
* [Remove faulty hook around chests' item rolling.](https://github.com/risk-of-thunder/R2API/pull/138)
* [Remove savety hooks for userprofile as RoR2 does that now out of the box](https://github.com/risk-of-thunder/R2API/pull/135)
* [Seperate BuffAPI and EliteAPI from ItemAPI](https://github.com/risk-of-thunder/R2API/pull/135)
* [Fix Reflection utils](https://github.com/risk-of-thunder/R2API/pull/135)
* [Update to latest BepInEx for building](https://github.com/risk-of-thunder/R2API/pull/134)


**2.4.10**

* [Fix for the descriptionToken of custom survivors](https://github.com/risk-of-thunder/R2API/pull/130)
* Update for latest game update:[A]](https://github.com/risk-of-thunder/R2API/pull/128) [B](https://github.com/risk-of-thunder/R2API/pull/131) [C](https://github.com/risk-of-thunder/R2API/pull/132)
* [Add UnbundledResourceProvider (allows to add generated assets easily)](https://github.com/risk-of-thunder/R2API/pull/125)

**2.4.2**

* [Fix R2API nullReffing on start when not loading R2API](https://github.com/risk-of-thunder/R2API/pull/121)

**2.4.1**

* [Allow mods to check if a R2API Submodule is loaded without needing an R2API instance](https://github.com/risk-of-thunder/R2API/pull/118)
* [Allow custom survivors to define display rules for custom items & improve code quality of display rules](https://github.com/risk-of-thunder/R2API/pull/116)
* [Allow custom items to define display rules for individual models](https://github.com/risk-of-thunder/R2API/pull/115)
* [Added more overloads for Direct Messages](https://github.com/risk-of-thunder/R2API/pull/114)
* [Rewrote the readme](https://github.com/risk-of-thunder/R2API/pull/113)
* [Updated AssetPlus to allow mass language changes without reloading the current language](https://github.com/risk-of-thunder/R2API/pull/112)

**2.3.22**

* [Disabled ModListAPI as it was causing issues in multiplayer games](https://github.com/risk-of-thunder/R2API/pull/111)
* [Prevent issues with itemDropAPI regarding overgrown printers](https://github.com/risk-of-thunder/R2API/commit/d1079631430d44e0e8d9ced7469f04c7dfdc0485)

**2.3.20**

* [Fix a lot of things in ModListAPI](https://github.com/risk-of-thunder/R2API/pull/108)
* Added safeties to certain APIs: [A](https://github.com/risk-of-thunder/R2API/pull/107) [B](https://github.com/risk-of-thunder/R2API/pull/103)
* [Added `IsLoaded(string)` to check if a submodule is loaded correctly](https://github.com/risk-of-thunder/R2API/pull/107)
* [Change ItemAPI to return more useful values](https://github.com/risk-of-thunder/R2API/pull/106)
* [Fix R2API not checking for submodule dependencies in certain folders](https://github.com/risk-of-thunder/R2API/pull/106)

**2.3.17**

* [Added warning when monomod patch is missing](https://github.com/risk-of-thunder/R2API/pull/100)
* [Add ModListAPI](https://github.com/risk-of-thunder/R2API/pull/99) *([And this one](https://github.com/risk-of-thunder/R2API/pull/102))*
* [Merged EntiyAPI, SkillAPI, SkinAPI, and LoadoutAPI](https://github.com/risk-of-thunder/R2API/pull/99)
* [Protect userprofiles from out of range indexes](https://github.com/risk-of-thunder/R2API/pull/99)
* [Fix R2API not loading when not in own subfolder](https://github.com/risk-of-thunder/R2API/pull/96)
* [Add `[mod]` prefix to dedicated servers](https://github.com/risk-of-thunder/R2API/pull/94)
* [Added utils for direct messages to clients](https://github.com/risk-of-thunder/R2API/pull/93)

**2.3.7**
* [Fix issue with Twisted Scavengers loot](https://github.com/risk-of-thunder/R2API/pull/91)

**2.3.5**

* Update for 4478858
* [APISubmodule now only loads requested submodules](https://github.com/risk-of-thunder/R2API/pull/89)
* [Added 5 new APIs: DirectorAPI, PrefabAPI, OrbAPI, SkinAPI, SkillAPI, EffectAPI](https://github.com/risk-of-thunder/R2API/pull/86)

**2.3.0**

* Added Submodule Dependencies.
* [Add more AssetPlus language features](https://github.com/risk-of-thunder/R2API/pull/78)
* [Add UnityEngine.Color overload for colored messages](https://github.com/risk-of-thunder/R2API/pull/75)
* [Added DifficultyAPI](https://github.com/risk-of-thunder/R2API/pull/74)
* [Disable vanilla sorting of modded entries](https://github.com/risk-of-thunder/R2API/pull/73)
* [Ease the use of CommandHelper](https://github.com/risk-of-thunder/R2API/pull/71) and [added ability to add convars](https://github.com/risk-of-thunder/R2API/pull/68)
* [Added ResourcesAPI](https://github.com/risk-of-thunder/R2API/pull/70)
* [Added ItemAPI](https://github.com/risk-of-thunder/R2API/pull/70)
* [Fix issues in vanilla mod helpers](https://github.com/risk-of-thunder/R2API/pull/70)
