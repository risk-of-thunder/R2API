
# R2API – a modding API for Risk of Rain 2
[![Build Status - DEPRECATED](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_apis/build/status/Risk%20of%20Rain%202%20Modding-.NET%20Desktop-CI?branchName=master)](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_build/latest?definitionId=1&branchName=master)
![GitHub Actions Build](https://github.com/risk-of-thunder/R2API/workflows/CI%20Build/badge.svg)


## RoR2 1.0 - IMPORTANT NOTICE

In the Release version of RoR2, Hopoo Games made a `NetworkModCompatibilityHelper` class, which can be given a mod list that is then transformed into a hash that will be checked upon client connection in multiplayer.
If the hash doesnt correspond between the server and the connecting client, the connection is refused.

R2API will add mods to that mod list if they:

* Use the `NetworkCompatibility` Attribute, either as an custom assembly attribute, or at the top of their plugin class inheriting from `BaseUnityPlugin` with the first argument being `CompatibilityLevel.EveryoneMustHaveMod`.
* Don't have the `NetworkCompatibility` Attribute but depend on R2API (have `[BepInDependency("com.bepis.r2api")]` attribute).

## About

R2API is a modding framework for other mods to work in, providing centralized and simplified APIs for Risk of Rain 2. This helps keeping mods compatible with each other.

At it's core, R2API should not change how the game behaves without any other mod installed. The only change is to keep mods out of quickplay and prismatic trials by request of the **Hopoo Games** team. 

## Manual installation

The contents of `R2API` should be extracted into the `BepInEx` folder, such that the files inside the `plugins` folder in the zip sit in the `plugins` folder in BepInEx.

Also you need to install [HookGenPatcher](https://thunderstore.io/package/RiskofThunder/HookGenPatcher/) because R2API wont function without it.

## Developing

A lot of documentation is in the included *xmldocs*, and further information may be on the dedicated [R2API wiki](https://github.com/risk-of-thunder/R2API/wiki). Do not hestiate to ask in [the modding discord](https://discord.gg/5MbXZvd) too!


## Bleeding Edge

**Unless you are a mod developer, you won't need this.**

Want to get the latest versions of R2API? The latest bleeding edge builds of `master` are hosted on [Azure](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_build/latest?definitionId=1&branchName=master), and may be downloaded using the `Artifacts` drop down menu.

Note that such builds may be **unstable**.

## Changelog

The most recent changelog can always be found on the [GitHub](https://github.com/risk-of-thunder/R2API/blob/master/Archived%20changelogs.md). In this readme, only the most recent *minor* version will have a changelog.

**Current**
* [ItemAPI now warns that ItemDef/EquipmentDef.pickupModelPrefab should have an ItemDisplay attached to them when they have ParentedPrefab display rules linked to them](https://github.com/risk-of-thunder/R2API/pull/311)
* [EliteAPI now exposes the default elite tiers array (through VanillaEliteTiers) before any changes are made to it for modder that want to change the vanilla elite tiers. Also, adding to the custom elite tier array now by default insert based on the cost multiplier of the elite tier.](https://github.com/risk-of-thunder/R2API/pull/308)
* [RecalculateStatsAPI now warns modders that the submodule could be not loaded](https://github.com/risk-of-thunder/R2API/pull/307)
* [Fix SoundAPI throwing on dedicated server](https://github.com/risk-of-thunder/R2API/pull/306)
* [Added TempVisualEffectAPI](https://github.com/risk-of-thunder/R2API/pull/313)

**3.0.59**

* [Extended SoundAPI for adding custom music](https://github.com/risk-of-thunder/R2API/pull/305) 
* [Added support for using existing UnlockableDefs in UnlockableAPI](https://github.com/risk-of-thunder/R2API/pull/304)
* [fixing server unlockables](https://github.com/risk-of-thunder/R2API/pull/302)

**3.0.52**

* [Add NetworkSoundEventDef registration to SoundAPI](https://github.com/risk-of-thunder/R2API/pull/301)

**3.0.50**

* [Added ArtifactCodeAPI](https://github.com/risk-of-thunder/R2API/pull/299)
* [Added support for new Artifact Code compounds](https://github.com/risk-of-thunder/R2API/pull/300)

**3.0.48**

* [Documentation for ItemDropAPI](https://github.com/risk-of-thunder/R2API/blob/master/ItemDropAPI%20Instructions%20For%20Use.txt)
* [ItemDropAPI Overhall](https://github.com/risk-of-thunder/R2API/pull/295)
* [Added MonsterItemsAPI back in](https://github.com/risk-of-thunder/R2API/pull/295)

**3.0.44**

* [Fixed PrefabAPI network registration](https://github.com/risk-of-thunder/R2API/pull/294)

**3.0.43**

* **IMPORTANT FOR MOD DEVS:** [R2API will no longer register mods to network if they don't depend on it with HardDependecy](https://github.com/risk-of-thunder/R2API/pull/286)
* [Added DeployableAPI](https://github.com/risk-of-thunder/R2API/pull/279)
* [Added DamageAPI](https://github.com/risk-of-thunder/R2API/pull/284)
* [Added RecalcStatsAPI, migrated from TILER2](https://github.com/risk-of-thunder/R2API/pull/287)
* [Updated DifficultyAPI, now has sprite ref overload](https://github.com/risk-of-thunder/R2API/pull/288)
* [RecalcStatsAPI fixes](https://github.com/risk-of-thunder/R2API/pull/290)
* [Missing MMHOOK/Publicized Assembly methods fixes](https://github.com/risk-of-thunder/R2API/pull/289)

**3.0.30**

* Fixes for current patch

**3.0.25**

* **IMPORTANT FOR MOD DEVS:** [R2API will no longer register mods to network if they don't depend on it](https://github.com/risk-of-thunder/R2API/pull/269)
* [DotAPI fixes](https://github.com/risk-of-thunder/R2API/pull/270)
* [EliteAPI fixes](https://github.com/risk-of-thunder/R2API/pull/271)

**3.0.13**

* [Updated UnlockableAPI, ItemDropAPI Overhall](https://github.com/risk-of-thunder/R2API/pull/265)
* [Update internals for 1.1.1.2 game version](https://github.com/risk-of-thunder/R2API/pull/267)
* Removed MonsterItemsAPI
* Removed `patchers` folder

**3.0.11**

* [Updated ResourceAPI error messages](https://github.com/risk-of-thunder/R2API/pull/258)
* SurvivorAPI Fixes: [A](https://github.com/risk-of-thunder/R2API/pull/259) [B](https://github.com/risk-of-thunder/R2API/pull/261)

**3.0.7**

* [Added ArtifactAPI and ProjectileAPI, BuffAPI fix](https://github.com/risk-of-thunder/R2API/pull/256)

**3.0.1**

* [Fixes for EffectAPI, LoadoutAPI and SoundAPI](https://github.com/risk-of-thunder/R2API/pull/254)

**3.0.0**

* Updated for the game `Anniversary Update`
* No longer include `monomod` folder
* [Various API Fixes. Removed AssetsAPI, InvetoryAPI. Moved MMHook to separate mod called (HookGenPatcher)](https://github.com/risk-of-thunder/R2API/pull/252)
* Removed obsolete APIs and methods: [A](https://github.com/risk-of-thunder/R2API/pull/249) [B](https://github.com/risk-of-thunder/R2API/pull/243)
* ItemAPI, ItemDropApi overhall. Added MonsterItemAPI: [A](https://github.com/risk-of-thunder/R2API/pull/214) [B](https://github.com/risk-of-thunder/R2API/pull/223) [C](https://github.com/risk-of-thunder/R2API/pull/228) [D](https://github.com/risk-of-thunder/R2API/pull/233) [E](https://github.com/risk-of-thunder/R2API/pull/234) [F](https://github.com/risk-of-thunder/R2API/pull/240) [G](https://github.com/risk-of-thunder/R2API/pull/245)
* LanguageAPI refactoring and fixes: [A](https://github.com/risk-of-thunder/R2API/pull/229) [B](https://github.com/risk-of-thunder/R2API/pull/244)
* [Added ILLine](https://github.com/risk-of-thunder/R2API/pull/230)
* [Fix for networked achievements](https://github.com/risk-of-thunder/R2API/pull/208)
* [Added SceneAssetAPI](https://github.com/risk-of-thunder/R2API/pull/210)
* [Added InteractablesAPI](https://github.com/risk-of-thunder/R2API/pull/216)
