# R2API.Difficulty - New Run Difficulties

## About

R2API.Difficulty is a submodule assembly for R2API that allows mod creators to add their own Difficulties to the game.

## Use Cases / Features

R2API.Difficulty is used for adding new difficulties to the game, These difficulties can include new behaviours depending on how the mod creator decides to implement the difficulty itself.

Difficulties added by DifficultyAPI by default will use negative DifficultyIndices, this is done because after RoR2 1.0.0.0, any DifficultyDef with an index greater than Eclipse 8 will recieve all the Eclipse modifiers. If you want this, you can always set "preferPositive" to true.

DifficultyAPI also comes bundled with the SerializableDifficultyDef, a ScriptableObject that holds the definition of a DifficultyDef, after being added by DifficultyAPI, the DifficultyDef  and DifficultyIndex properties are assignedd values.

## Related Pages

## Changelog

### '1.0.1'
* Fix hooks not being properly enabled.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
