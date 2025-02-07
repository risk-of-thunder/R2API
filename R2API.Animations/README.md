# R2API.Animations - Modifying RuntimeAnimatorController

## About

R2API.Animations is a submodule assembly for R2API that allows mod creators to modify existing RuntimeAnimatorController at runtime. 

You can add states (both clips and blend trees), transitions and parameters.

## Unity

When imported into Unity, adds an option in context menu `Create/R2API/Animations/AnimatorDiff`. In `AnimatorDiff` you specify `Source Controller` which is used as base for the diff,
and `Modified Controller` which should have all things that `Source Controller` has (recommended just select controller in hierarchy and pressing `ctrl + d` to duplicate it) + your modifications.

After that you add the `AnimatorDiff` to an `AssetBundle` and in code you can create an instance of `AnimatorModifications` with `AnimatorModifications.CreateFromDiff()`.

## Changelog

### '1.1.0'
* Added support for `BlendTree`s.
* Added `AnimatorDiff` that you can create inside editor and load at runtime.

### '1.0.1'
* Values for `NewStates` and `NewTransitions` dictionaries are now lists

### '1.0.0'
* Release
