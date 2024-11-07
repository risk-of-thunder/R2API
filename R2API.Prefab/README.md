# R2API.Prefab - Prefab Creation, Networking and Duplication.

## About

R2API.Prefab is a submodule assembly for R2API that allows mod creators to add new Prefabs to the game, these prefabs can later be instantiated like regular prefabs.

## Use Cases / Features

The prefabs created via PrefabAPI are mainly created using the `InstantiateClone` method, which instantiates a clone of an existing prefab and leaves it disabled and stored in memory, 
this sleeping state mimic the behaviour like regular prefabs in the Unity Editor.

By default the `InstantiateClone` method assume that it is a prefab with a `NetworkIdentity` component, and will attempt to register it internally using the `RegisterNetworkPrefab` method described below

If its not the case make sure to make the `bool` argument is `false` when calling `InstantiateClone`.

You can also use this API for registering prefab to the network client catalog so that its properly networked (when using NetworkServer.Spawn for example) 
by using the `RegisterNetworkPrefab` method.

## Related Pages

## Changelog

### '1.0.4'
* Initial fixes for SOTS DLC2 Release.

### '1.0.3'
* Add missing `BepInDependency` to `R2API.ContentManagement`

### '1.0.2'
* Fix the NuGet package which had a dependency on a non-existent version of `R2API.Core`.

### '1.0.1'
* Fix some rare cases of prefabs not being correctly network registered.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
