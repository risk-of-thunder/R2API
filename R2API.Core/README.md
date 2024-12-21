# R2API.Core - Core Module of R2API

## About

R2API.Core is the main module of R2API, and contains the backbone and framework for all the other submodule assemblies of R2API, providing utilities and tools for developing new API's.

By itself it doesn't change how the game behaves in any way shape or form.

## Use Cases / Features

R2API.Core is used in all the submodule assemblies of R2API, as such, it is necessary for most if not all other submodules to function properly.

## Related Pages

A lot of documentation is in the included xmldocs, and further information may be on the dedicated [R2API wiki](https://github.com/risk-of-thunder/R2API/wiki).

Do not hesitate to ask in [the modding discord](https://discord.gg/5MbXZvd) too!

## Changelog
### '5.1.4'
* Fixed CompressedFlagArrayUtilities.RemoveImmutable.

### '5.1.4'
* Fixed CompressedFlagArrayUtilities.AddImmutable.

### '5.1.3'
* Added methods for immutable Array operation to CompressedFlagArrayUtilities

### '5.1.2'
* Fix SystemInitializerInjector

### '5.1.1'
* Initial fixes for SOTS DLC2 Release.

### '5.1.0'
* Add Array to Array operations to CompressedFlagArrayUtilities

### '5.0.12'

* Bump GameBuildId version.

### '5.0.11'

* Fix SystemInitializerInjector.

### '5.0.10'
* Make R2API Reflection methods return / take into account inherited members.

### '5.0.9'
* Some R2API submodules were incorrectly marked as required by everyone in a multiplayer lobby

### '5.0.8'
* Fix NuGet Package

### '5.0.7'
* Make private some methods that were not supposed to be public
* Move the Hook logger to RoR2BepInExPack

### '5.0.6'
* CompressedFlagArrayUtilities fixes after optimization

### '5.0.5'
* CompressedFlagArrayUtilities optimization

### '5.0.4'
* Added SystemInitializerInjector class to the Utils namespace

### '5.0.3'
* Also move NetworkCompatibility initialization back to the Core module.

### '5.0.2'
* Move NetworkCompatibility back to the Core module for back compatibility reasons.

### '5.0.1'
* Add back an utility class that was removed by mistake.

### '5.0.0'
* Split from the main R2API.dll into its own submodule.
