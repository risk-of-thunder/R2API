
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

## Changelog

The most recent changelog can always be found on the [GitHub](https://github.com/risk-of-thunder/R2API/blob/master/Archived%20changelogs.md). In this readme, only the most recent *minor* version will have a changelog.

**Current**
* [Fix LanguageAPI being case dependent, add support for ChainGunOrb in DamageAPI by @KingEnderBrine](https://github.com/risk-of-thunder/R2API/pull/402)
* [UnlockableAPI: Fix custom achievements not working for current patch by @xiaoxiao921](https://github.com/risk-of-thunder/R2API/pull/391)
* [UnlockableAPI: Marked as Obsolete by @Nebby1999](https://github.com/risk-of-thunder/R2API/pull/390)
* [DirectorAPI: Add AddNewMonsterToStagesWhere & AddSelectedNewMonsters, and publicize base AddNewMonster by @bb010g](https://github.com/risk-of-thunder/R2API/pull/382)
* [ItemAPI: Specify IDRS log is in the log file by @Nebby1999](https://github.com/risk-of-thunder/R2API/pull/387)
* [ItemAPI: Tier error fix by @Nebby1999](https://github.com/risk-of-thunder/R2API/pull/394)
* [Fix R2API erroring in the Epic Games version of the game by @xiaoxiao921](https://github.com/risk-of-thunder/R2API/pull/392)

* [Reflection: Improve speed by @DemoJameson](https://github.com/risk-of-thunder/R2API/pull/378)
* [Reflection: Fix MethodInfo/ConstructorInfo not cache properly by @DemoJameson](https://github.com/risk-of-thunder/R2API/pull/380)
* [Reflection: Fix CombineHashCode returning the same result on arrays of different order by @DemoJameson](https://github.com/risk-of-thunder/R2API/pull/381)
* [Reflection: Fix different generic parameters returning the same delegate by @DemoJameson](https://github.com/risk-of-thunder/R2API/pull/384)
* [Reflection: Fix delegate not box value by @DemoJameson](https://github.com/risk-of-thunder/R2API/pull/385)
* [Reflection: Support reading constant field by @DemoJameson](https://github.com/risk-of-thunder/R2API/pull/386)
* [Reflection: Use normal dictionaries instead of Concurrent ones by @Windows10CE](https://github.com/risk-of-thunder/R2API/pull/383)


**4.3.x**
* [Update DirectorAPI by @xiaoxiao921](https://github.com/risk-of-thunder/R2API/pull/368)
* [Fix ItemDisplay issues by @KomradeSpectre](https://github.com/risk-of-thunder/R2API/pull/369)
* [ItemAPI ItemDisplay fixes by @Nebby1999](https://github.com/risk-of-thunder/R2API/pull/371)
* [Fix EliteAPI and bug fix DirectorAPI by @xiaoxiao921](https://github.com/risk-of-thunder/R2API/pull/372)
* [Friendlier internal stage names by @bb010g](https://github.com/risk-of-thunder/R2API/pull/373)
* [Fix DirectorApi spawning by @xiaoxiao921](https://github.com/risk-of-thunder/R2API/pull/374)

**4.2.x**
* [NuGet package is now available!](https://www.nuget.org/packages/R2API/)

**4.1.x**

* [Fix ItemDisplay addition for Items and Equipment](https://github.com/risk-of-thunder/R2API/pull/369)
* [Fix contentpacks that did not have the correct name in some cases](https://github.com/risk-of-thunder/R2API/pull/366)
* [Crit damage multiplier is now a float](https://github.com/risk-of-thunder/R2API/pull/365)
* [ArtifactCode fix, ContentLogging fix](https://github.com/risk-of-thunder/R2API/pull/361)

**4.0.11**

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
