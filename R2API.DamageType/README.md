# R2API.DamageType - Custom DamageType creation and management

## About

R2API.DamageType is a submodule assembly for R2API that allows mod creators to add their own DamageTypes to attacks.

## Use Cases / Features

R2API.DamageType can be used for mod creators to add their own DamageTypes to the game, and later use said damage types on different damage sources.

This is done via the DamageAPI class, which is used for reserving DamageTypes and adding them to the following damage sources:
* DamageInfo
* BulletAttack
* DamageOrb
* GenericDamageOrb
* LightningOrb
* BlastAttack
* OverlapAttack
* DotController.DotStack
* CrocoDamageTypeController

## Related Pages

## Changelog

### '1.1.7'
* Changed bounds exception to just log error to not completely break mods that already try to use not registered damage types.

### '1.1.6'
* Changed bounds check and minimum damage type value to make it easier to notice when using unregistered damage type.

### '1.1.5'
* Fixed an issue where `FireProjectileInfo.damageTypeOverride` wasn't applied to a projectile if it only had ModdedDamageType set.

### '1.1.4'
* Fixed an issue where removing vanilla damage type with `a &= ~b` would also remove all modded damage types.

### '1.1.3'
* Internal rewrite for easier support in the future.

### '1.1.2'
* More fixes for SOTS DLC2 Release.

### '1.1.1'
* Initial fixes for SOTS DLC2 Release.

### '1.1.0'
* CrocoDamageTypeController support added,allowing better implementations of alt passives for Acrid.

### '1.0.4'
* Memory optimization

### '1.0.3'
* Optimization

### '1.0.2'
* Fix projectile IL hooks

### '1.0.1'
* Made IL hooks more robust to stop API failing because of other mods

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
