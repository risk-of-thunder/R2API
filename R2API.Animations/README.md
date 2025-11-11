# R2API.Animations - Modifying RuntimeAnimatorController

## About

R2API.Animations is a submodule assembly for R2API that allows mod creators to modify existing RuntimeAnimatorController at runtime. 

You can add:
* Parameters.
* Layers.
* Sub-State Machines.
* Behaviours.
* States (both clips and blend trees).
* Transitions.

Since applying modifications to a controller is somewhat complicated and slow operation, the changes are cached.
The cache for a controller is rebuilt when a mod version is changed.
For development there is a config option `IgnoreCache`, so that you don't have to constantly change mod version or manually delete cache.

## Unity

When imported into Unity, adds an option in context menu `Create/R2API/Animations/AnimatorDiff`. In `AnimatorDiff` you specify `Modified Controller` which should have all things that `Source Controller` has + your modifications.

To create `Modified Controller` use option `Assets/R2API/Animation/Copy AnimatorController for modification` in context menu on a `Source Controller`.

After that you add the `AnimatorDiff` to an `AssetBundle` and in code you can create an instance of `AnimatorModifications` with `AnimatorModifications.CreateFromDiff()`.

## Changelog
### '1.2.0'
* Added support for behaviours.
* Added support for child `StateMachine`s.
* Added support for new layers.
* Added `Copy AnimatorController for modification` button to context menu in Unity.
* `ClipBundlePath` is no longer required for `State` and `ChildMotion`.
* Cache detection has been simplified. Now only changes to mod version will cause a rebuild. During development change config option `IgnoreCache` to true.

### '1.1.1'
* Fix NuGet package

### '1.1.0'
* Added support for `BlendTree`s.
* Added `AnimatorDiff` that you can create inside editor and load at runtime.

### '1.0.1'
* Values for `NewStates` and `NewTransitions` dictionaries are now lists

### '1.0.0'
* Release
