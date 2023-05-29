# R2API.Language - Automatic loading of language files and Languate token overrides

## About

R2API.Language is a submodule assembly for R2API that allows mod creators to add proper Language tokens to the game, or override text ingame.

## Use Cases / Features

R2API.Language is mainly used for adding new Tokens to the game, the Token system allows you to specify how specific assets should be named depending on the language.

R2API.Language automatically loads files in your mods folder that end in the .language extension, these files will be automatically applied to the game.

Alongside this, R2API.Language allows you to override existing tokens with your own, this can be used for mods that modify behaviours of certain things ingame.

## Related Pages

An example tutorial on how to use .language files to load tokens can be found [here](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/)

## Changelog

### '1.0.1'
* Fix the NuGet package which had a dependency on a non-existent version of `R2API.Core`.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
