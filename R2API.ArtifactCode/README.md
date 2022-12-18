# R2API.ArtifactCode - Artifact Codes and Compound addition

## About

R2API.ArtifactCode is a submodule assembly for R2API that simplifies the process of adding new Artifact Codes to the Artifact Portal Dialer in Sky Meadow,
alongside the additon of custom Artifact Compounds (Such as the vanilla compounds, Circle, Square, Triangle & Diamond).

## Use Cases / Features

R2API.ArtifactCode can be used for mods to add their own ArtifactTrials to the game so their artifacts can be unlocked like regular, vanilla artifacts.

This is done via the ArtifactCode scriptable object, which works as a simplified way of creating the required Sha256HashAsset using int values to represent the individual Compound Values.

The Int values are represented by 3 Vector3Ints, representing the top row, middle row and bottom row of compounds.

Adding compounds is done using the ArtifactCompoundDef scriptable object from the base game. The value of the ArtifactCompoundDef must be unique.

A static class populated with constant values that represent the default, vanilla compounds, these are:

    Empty = 11
    Square = 7
    Circle = 1
    Triangle = 3
    Diamond = 5

## Related Pages

The page for [Artifacts](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Artifacts/) in the R2Wiki contains a tutorial on how to use the API

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
