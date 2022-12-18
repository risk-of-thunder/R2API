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

## Related Pages

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.