# R2API.SceneAsset - Retrieving assets that live exclusively in game scenes

## About

R2API.SceneAsset is a submodule assembly for R2API that allows mod creators to obtain Assets that live exclusively in scenes. 

## Use Cases / Features

R2API.SceneAsset works via the AddAssetRequest method, which allows you to run an Action when the RoR2 splashscreen appears, this internally causes R2API to load the scene you need and later run the event so you can retrieve the specified asset you're looking for.

## Related Pages

## Changelog

### '1.1.2'
* Fix the api not working with mods that would load scene very early.

### '1.1.1'
* Fix the game potentially not going past the splash / intro.

### '1.1.0'
* Fix the API not working since SOTV patch.

### '1.0.1'
* Fix the NuGet package which had a dependency on a non-existent version of `R2API.Core`.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
