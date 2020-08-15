
# R2API – a modding API for Risk of Rain 2
[![Build Status - DEPRECATED](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_apis/build/status/Risk%20of%20Rain%202%20Modding-.NET%20Desktop-CI?branchName=master)](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_build/latest?definitionId=1&branchName=master)
![GitHub Actions Build](https://github.com/risk-of-thunder/R2API/workflows/CI%20Build/badge.svg)


## RoR2 1.0 - IMPORTANT NOTICE

In the Release version of RoR2, Hopoo Games made a `NetworkModCompatibilityHelper` class, which can be given a mod list that is then transformed into a hash that will be checked upon client connection in multiplayer.
If the hash doesnt correspond between the server and the connecting client, the connection is refused.

R2API will add mods to that mod list if they:

* Use the `NetworkCompatibility` Attribute, either as an custom assembly attribute, or at the top of their plugin class inheriting from `BaseUnityPlugin` with the first argument being `CompatibilityLevel.EveryoneMustHaveMod`.
* Don't have the `NetworkCompatibility` Attribute or the `ManualNetworkRegistrationAttribute` anywhere in their assembly.

## About

R2API is a modding framework for other mods to work in, providing centralized and simplified APIs for Risk of Rain 2. This helps keeping mods compatible with each other.

At it's core, R2API should not change how the game behaves without any other mod installed. The only change is to keep mods out of quickplay and prismatic trials by request of the **Hopoo Games** team. 

## Installation

The contents of `R2API` should be extracted into the `BepInEx` folder, such that the files inside the `monomod` folder in the zip sit in the `monomod` folder in BepInEx and the files and folder in `plugins` in the archive is inside your `plugins` folder.

A succesful installation should look like this:
![installation](https://cdn.discordapp.com/attachments/575082050097381412/667394037229027328/unknown.png)
*[(click to enlarge)](https://cdn.discordapp.com/attachments/575082050097381412/667394037229027328/unknown.png)*

## Developing

A lot of documentation is in the included *xmldocs*, and further information may be on the dedicated [R2API wiki](https://github.com/risk-of-thunder/R2API/wiki). Do not hestiate to ask in [the modding discord](https://discord.gg/5MbXZvd) too!


## Bleeding Edge

**Unless you are a mod developer, you won't need this.**

Want to get the latest versions of R2API? The latest bleeding edge builds of `master` are hosted on [Azure](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_build/latest?definitionId=1&branchName=master), and may be downloaded using the `Artifacts` drop down menu.

Note that such builds may be **unstable**.

## Changelog

The most recent changelog can always be found on the [Github](https://github.com/risk-of-thunder/R2API/commits/master). In this readme, only the most recent *minor* version will have a changelog.

**Current**

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
