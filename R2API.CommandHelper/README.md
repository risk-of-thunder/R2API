# R2API.CommandHelper - Adding console commands.

## About

R2API.CommandHelper is a submodule for R2API that provides a way for adding console commands for the in-game console.

This submodule is mainly here for legacy purposes and retro compatibility with old mods as you can nowadays register console commands
by simply using the `[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]` assembly attribute

## Changelog

### '1.0.2'
* Initial fixes for SOTS DLC2 Release.

### '1.0.1'
* Fix the NuGet package which had a dependency on a non-existent version of `R2API.Core`.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
