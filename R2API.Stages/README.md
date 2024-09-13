# R2API.Stages - Easy Registration of Stages and Stage Variants + Stage Utils

## About

R2API.Stages is a submodule for R2API that adds a single unified method to register both new stages and variants of stages. 

R2API.Stages also hosts an abundant amount of utils to assist in stage making.

## Use Cases / Features

You can use `StageRegistration.RegisterSceneDefToNormalProgression` quickly register a SceneDef to its proper place in the normal progression. This method can add both variants and new custom stages. To add a variant, make sure the `baseSceneNameOverride` in your SceneDef is set to the same string as the stage you want to make a variant of. It also has extra parameters to control if the stage is pre-loop and post-loop.

The StageRegistration class also hosts `stageVariantDictionary`, which is a readonly dictionary to grab all the variants of a stage by inputting the `baseSceneNameOverride`. For example, if you input "golemplains" you will get a list of SceneDefs with atleast `golemplains` and `golemplains2`.

## Related Pages

## Changelog

### '1.0.2'
- Stripped the Variant Rebalancer and Blacklister. This was to account for the inconsistencies in the stage weights in DLC2 and give more control to the user.
- You can now register stages into post-loop scene collections.
- New way to register scenes to the normal stage progression: `RegisterSceneDefToNormalProgression`. Supports extra parameters such as `weight`, `preLoop`, `postLoop`.
- Marked `RegisterSceneDefToLoop` as obsolete.

### '1.0.1'
- Fix r2api content management DLL being shipped in that package

### '1.0.0'
- Initial Release


