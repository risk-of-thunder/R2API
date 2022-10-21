# R2API.ScecneAsset - Retrieve Assets from game scenes

## About

R2API.SceneAsset is a submodule assembly for R2API that allows mod creators to obtain Assets that live exclusively in scenes. 

## Use Cases / Features

R2API.SceneAsset works via the AddAssetRequest method, which allows you to run an Action when the RoR2 splashscreen appears, this internally causes R2API to load all the required scenes and later run the Action so you can retrieve the specified asset you're looking for.

## Related Pages

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.