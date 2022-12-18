# R2API – a modding API for Risk of Rain 2

## About

R2API is a modding framework for other mods to work in, providing centralized and simplified APIs for Risk of Rain 2.

This helps keep mods compatible with each other.

At its core, R2API should not change how the game behaves without any other mod installed.

The only change is to keep mods out of quickplay and prismatic trials by request of the **Hopoo Games** team.

## Installation

**The usage of a mod manager is heavily recommended, you can use r2modman or the thunderstore mod manager.**

If you want to manually install still:

-  Install [BepInEx](https://thunderstore.io/package/bbepis/BepInExPack/).

-  Install [HookGenPatcher](https://thunderstore.io/package/RiskofThunder/HookGenPatcher/).

-  Copy the files inside the `plugins` folder from the zip into the `BepInEx/plugins`.

## Developing mods using R2API

Since the R2API `5.0.0` version update, mod creators should ideally only reference the packages they need in their C# projects and their thunderstore packages.

In the dependency array of their thunderstore manifest, they should reference the R2API packages from the `RiskofThunder` team, for example: `RiskofThunder-R2API_LobbyConfig-1.0.0`

In their C# projects, they should only get the corresponding R2API submodules dll they need.

You can use the nuget packages, or download the dlls directly from thunderstore, depending on your workflow.

A lot of documentation is in the included *xmldocs*, and further information may be on the dedicated [R2API wiki](https://github.com/risk-of-thunder/R2API/wiki).

Do not hesitate to ask in [the modding discord](https://discord.gg/5MbXZvd) too!

## Changelog

Older changelogs for this package can be found on [GitHub](https://github.com/risk-of-thunder/R2API/blob/master/Archived%20changelogs.md).

### '5.0.0'
* R2API modules are now split into their own package.

  Mod creators should ideally only reference the packages they need in their C# projects and their thunderstore packages. Please refer to the `Developing mods using R2API` section for more info.

  If you are a mod user, there should be no difference whatsoever with this update.

  If you find any issues, please tell us in the [the modding discord](https://discord.gg/5MbXZvd) or the [GitHub repository](https://github.com/risk-of-thunder/R2API)
