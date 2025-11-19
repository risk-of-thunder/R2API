# R2API.Skins - Adding new Skins, Skin-Specific IDRS and VFX and Skill-Specific parts

## About

R2API.Skins is a submodule for R2API that takes the Skin related methods and utilities from ``R2API.Loadout`` and implements them in this new submodule

Alongside the old skin creation methods from ``R2API.Loadout``, R2API.Skins also contains utilities for improving the skin experience, such as having Skin-Specific ItemDisplayRuleSets, custom VFX for skins, providing a system for overriding Light colors and having Skill-Specific parts

## Changelog

### `1.4.1`

* Update for DLC 3 release 2.

### `1.4.0`

* Update for DLC 3 release.

### `1.3.1`

* Fix missing Patcher / Interop dlls

### `1.3.0`

* Added SkinSkillVariants feature for adding skill-specific skin parts replacements.

### '1.2.2'
* Removed the SkinLightReplacement system, as it was not used by a single mod in the entire thunderstore, alongside the fact that the Memory Optimization patch made its concept obsolete.

### `1.2.1`

* Moved to SkinDefParams creation
* SkinDefInfo is now obsolete. Use SkinDefParamsInfo instead.
* Added precautions to automatically add and populate the ModelSkinsController on displayPrefabs, when necessary. This should be done by the devs.
* Added validity checks for name and nameTokens

### `1.2.0`

* Added the ability to replace the color LightInfos for a skin

### `1.1.2`

* Initial fixes for SOTS DLC2 Release.

### `1.1.1`

* Added the ability to override `DisplayGroupRule` per item for a skin. Which allows to add skin-specic item display without requiring to create IDRS with all items. See `SkinIDRS.AddGroupOverride()`.

### `1.1.0`

* Added the SkinVFX class for adding skin-specific effect replacements.

### `1.0.0`

* Initial Release
