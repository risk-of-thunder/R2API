# R2API.Orb - Creation and management of custom Orb types.

## About

R2API.Orb is a submodule assembly for R2API that allows mod creators to add new Orbs to the game.
Orbs include things like Lightning Orbs, healing orbs and more.

## Use Cases / Features

R2API.Orb works via the OrbAPI class, where you can use AddOrb() to add a new type of orb that subclasses from the class ``RoR2.Orbs.Orb``

## Related Pages

## Changelog

### '1.0.1'
* Fix the NuGet package which had a dependency on a non-existent version of `R2API.Core`.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
