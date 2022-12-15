# R2API.TempVisualEffect - Custom temporary visual effects for characters.

## About

R2API.TempVisualEffect is a submodule assembly for R2API that allows mod creators to easily add new VisualEffects to Characters that appear after a certain condition has been fulfilled.

## Use Cases / Features

R2API.TempVisualEffect works via the method AddTemporaryVisualEffect, which allows you to have a prefab be instantiated in a CharacterBody after a specific condition becomes true. The prefab supplied must have the TemporaryVisualEffect component.

For the condition you use the EffectCondition delegate, which returns true if the visual effect should appear for the body.

You can also specify if the effect should be scaled using the body's best fit radius, or if the effect should spawn in a specific ChildLocator entry.

A separate overload for AddTemporaryVisualEffect takes an EffectRadius delegate, which returns a specific radius for the visual effect. 

## Related Pages

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
