# R2API.Elites - Elite, EliteDef and a Global EliteRamp implementations

## About

R2API.Elites is a submodule assembly for R2API that allows mod creators to add new Elites, Elitetiers and properly working Elite Ramps for Elites in game.

## Use Cases / Features

R2API.Elites is used for adding new EliteDefs, EliteTierDefs and properly working Elite Ramps to the game.

The addition of Elites is handled via the CustomElite class, which contains the following information:

    EliteDef: The Elite being added
    EliteRamp: A Texture2D that's going to be the Elite's Ramp, this is what makes elites such as blazing elites have a different color palette
    EliteTierDefs: The Elite being added will be added to the specified EliteTierDefs.

The EliteRamp implementation is handled inside the EliteRamp class, you can use the method ``AddRamp`` for tying a Ramp to an eliteDef, this can be useful in scenarios where you dont want to add Elites via the main EliteAPI

## Related Pages

## Changelog

### '1.0.3'
* Fix some incorrect internal code.

### '1.0.2'
* Add missing `BepInDependency` to `R2API.ContentManagement`

### '1.0.1'
* Fix some non working custom ramps (ramp index not properly set)
* Always enable EliteAPI hooks (potential fix for a Spikestrip elite color bug)

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
