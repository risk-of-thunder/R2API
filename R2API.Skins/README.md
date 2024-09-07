# R2API.Skins - Adding new Skins and Skin-Specific IDRS

## About

R2API.Skins is a submodule for R2API that takes the Skin related methods and utilities from ``R2API.Loadout`` and implements them in this new submodule

Alongside the old skin creation methods from ``R2API.Loadout``, R2API.Skins also contains utilities for improving the skin experience, such as having Skin-Specific ItemDisplayRuleSets, custom VFX for skins and providing a system for overriding Light colors

## Changelog


### '1.2.0

* Added the ability to replace the color LightInfos for a skin

### '1.1.2'

* Initial fixes for SOTS DLC2 Release.

### '1.1.1

* Added the ability to override `DisplayGroupRule` per item for a skin. Which allows to add skin-specic item display without requiring to create IDRS with all items. See `SkinIDRS.AddGroupOverride()`.

### '1.1.0'

* Added the SkinVFX class for adding skin-specific effect replacements.

### '1.0.0'

* Initial Release
