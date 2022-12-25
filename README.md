# R2API – a modding API for Risk of Rain 2

![GitHub Actions Build](https://github.com/risk-of-thunder/R2API/workflows/CI%20Build/badge.svg)

## About

R2API is a modding framework for other mods to work in, providing centralized and simplified APIs for Risk of Rain 2.

This helps keep mods compatible with each other.

At its core, R2API should not change how the game behaves without any other mod installed.

The only change is to keep mods out of quickplay and prismatic trials by request of the **Hopoo Games** team.

## Installation

The usage of a mod manager is heavily recommended, you can use r2modman or the thunderstore mod manager.

If you want to manually install still:

-  Install [BepInEx](https://thunderstore.io/package/bbepis/BepInExPack/).

-  Install [HookGenPatcher](https://thunderstore.io/package/RiskofThunder/HookGenPatcher/).

-  Copy the files inside the `plugins` folder from the zip into the `BepInEx/plugins`.

## Developing mods using R2API

Since the R2API `5.0.0` version update, mod creators should ideally only reference the packages they need in their C# projects and their thunderstore packages.

In the dependency array of their thunderstore manifest, they should reference the R2API packages from the `RiskofThunder` team, for example: `RiskofThunder-R2API_LobbyConfig-1.0.0`

In their C# projects, they should only get the corresponding R2API submodules dll they need.

You can do that in a multitude of ways, depending on your workflow: 

-  By directly editing by hand the .csproj file of your C# project, add such a line under an `ItemGroup`:
   ```xml
   <PackageReference Include="R2API.Networking" Version="1.0.2" />
   ```

-  Doing it through the NuGet Package Manager, that you can access by right clicking your project within the Solution Explorer when using Visual Studio, and installing it through that manager directly:

   The packages are available under the `RiskofThunder` nuget account, and you can find them through the search bar by typing `R2API`.

-  You can also download the dlls directly from thunderstore, depending on your workflow, and add them through the Solution Explorer, right clicking your project, Add -> Project Reference, and selecting the wanted .dll files.

-  If you use Unity, you can download the dlls, and drag and drop the modules dll directly into your Unity Project under any folders that are under the root Assets/ folder.

A lot of documentation is in the included *xmldocs*, and further information may be on the dedicated [R2API wiki](https://github.com/risk-of-thunder/R2API/wiki).

Do not hesitate to ask in [the modding discord](https://discord.gg/5MbXZvd) too!

## Bleeding Edge / Artifacts

Artifacts builds are available under the Action tab, they are built after each commit / pull requests.

## Contributing to R2API

We welcome any contributions, please read the [CONTRIBUTING.md file](https://github.com/risk-of-thunder/R2API/blob/master/CONTRIBUTING.md)

## Changelog

The changelog for all previous major versions can always be found on [GitHub](https://github.com/risk-of-thunder/R2API/blob/master/Archived%20changelogs.md).

If you want the changelog for the current major version, please check the corresponding R2API modules READMEs.
