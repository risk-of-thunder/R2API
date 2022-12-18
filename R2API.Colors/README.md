# R2API.Colors - Easy Addition of Colors and Damage Colors

## About

R2API.Colors is a submodule for R2API that adds a single unified method for adding both Colors and Damage Colors to the game.

These new colors can be used on Attacks for adding unique coloration to the damage numbers, and add unique colors to ItemTiers, and more miscellaneous color usages.

## Use Cases / Features

R2API.Colors can be used for mods to add their own ColorIndex and DamageColorIndex to the game, which then can be used in a plethora of scenarios.

This is done via code by methods that return the ColorCatalog.ColorIndex or DamageColorIndex that will represent said color.

Alongsided this, R2API.Colors also adds two new Scriptable Objects, which can be used to pre-serialize Color Indices.

These are the SerializableColorCatalogEntry and the SerializableDamageColor.

These scriptable objects can later be used for example, in EntityStateConfigurations for serializing the DamageColorIndex of a certain attack in an entity state.

## Related Pages

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
