# R2API.Dot - Custom Damage over time effects

## About

R2API.Dot is a submodule assembly for R2API that allows mod creators to add new Damage over time effects to the game, DOT's include the game's Burn, Bleeding, and more.

## Use Cases / Features

R2API.Dot is used for adding new DotIndices and DotDefs to the game's DotController, this is done via the RegisterDotDef method.

Alongside adding new DotIndices and DotDefs, One can also provide CustomDotBehaviours and CustomDotVisuals, these are Delegates that are used for giving extra features to your DamageOverTime effects.

* CustomDotBehaviour: Runs the delegate after AddDot succesfully adds your DamageOverTime
* CustomDotVisual: Functions like a FixedUpdate method for your dot, use this to handle the Visual effect of your DOT.

## Related Pages

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.