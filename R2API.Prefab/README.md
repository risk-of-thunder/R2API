# R2API.Prefab - Prefab Creation, Networking and Duplication.

## About

R2API.Prefab is a submodule assembly for R2API that allows mod creators to add new Prefabs to the game, these prefabs can later be instantiated like regular prefabs.

## Use Cases / Features

The prefabs created via PrefabAPI are mainly created using the InstantiateClone method, which instantiates a clone of an existing prefab and leaves it disabled and stored in memory, this sleeping prefab can later be instantiated like regular prefabs.

Prefabs created like this and that have NetworkIdentity components are not networked, as such, you can manually network your prefabs using PrefabAPI's RegisterNetworkPrefab method.

## Related Pages

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.