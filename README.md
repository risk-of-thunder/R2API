
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

* Updated for the game `Survivors of The Void`
* [Added SOTVSerializableContentPack](https://github.com/risk-of-thunder/R2API/commit/423a6b3de16da31e42ef57d6aaf7bc2b781eab2a)
* [Complete Refractoring of Content Addition Systems, now uses the R2APIContentManager](https://github.com/risk-of-thunder/R2API/pull/338)
* [Each mod now has it's own ContentPack managed by R2API](https://github.com/risk-of-thunder/R2API/pull/338#issue-1137783592)
* [A Mod can add a PreExisting content pack to the R2APIContentManager](https://github.com/risk-of-thunder/R2API/pull/338#issuecomment-1040337885)
* [A PreExisting ContentPack can opt out from being loaded by R2API's Systems](https://github.com/risk-of-thunder/R2API/pull/338#issuecomment-1040337885)
* [Each mod can now add a valid content piece via ContentAdditionHelpers](https://github.com/risk-of-thunder/R2API/pull/338#issuecomment-1041783985)
* [Assets added by R2API will always have unique names](https://github.com/risk-of-thunder/R2API/pull/338#issue-1137783592)
* [Made LoadRoR2ContentEarly public, mods can now access a readonly version of RoR2's ContentPack](https://github.com/risk-of-thunder/R2API/pull/338#issuecomment-1040337885)
* [UnlockableAPI's Overload Methods now point to a single private method](https://github.com/risk-of-thunder/R2API/pull/338/commits/82d6edb8933af7974683f411c65a256378a45ae1)
* [Marked the following APIs As Obsolete: ArtifactAPI, BuffAPI, EffectAPI, ProjectileAPI and SurvivorAPI](https://github.com/risk-of-thunder/R2API/pull/338#issuecomment-1041484037)
* [Marked the following methods in LoadoutAPI as Obsolete: AddSkill, StateTypeOf, AddSkillDef, AddSkillFamily](https://github.com/risk-of-thunder/R2API/pull/338#issuecomment-1041484037)
* [Marked the AddNetworkedSoundEvent method in SoundAPI  as Obsolete](https://github.com/risk-of-thunder/R2API/pull/338#issuecomment-1041484037)
* [NetworkingAPI now has Send overloads for sending to specific network connections. Also have some documentation now](https://github.com/risk-of-thunder/R2API/pull/333)
* [Allow character mods to opt out from default item display rules](https://github.com/risk-of-thunder/R2API/pull/330)
* [Make dotAPI parts accessible for modders](https://github.com/risk-of-thunder/R2API/pull/339)
* [RecalculateStatsAPI can now modify Critical Hit Damage](https://github.com/risk-of-thunder/R2API/pull/346)
