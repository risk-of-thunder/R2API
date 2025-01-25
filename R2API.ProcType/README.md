# R2API.ProcType - Reserve new ProcTypes

## About

R2API.ProcType is a submodule for R2API that allows mod creators to reserve new ProcTypes and add them to proc chain masks.

## Use Cases / Features

Custom ProcTypes can be reserved through ProcTypeAPI.

Use the provided RoR2.ProcChainMask extensions to add, remove, and check for modded Proc Types.

R2API.ProcType patches RoR2.ProcChainMask to store additional data and this data is exposed by the API. ProcTypeAPI provides the necessary methods to manipulate this data; mod creators should not need to interact with it directly.

## Changelog

### '1.0.2'
* Changed bounds check and minimum proc type value to make it easier to notice when using unregistered proc type.

### '1.0.1'
* Fix NuGet package

### '1.0.0'
* Initial Release
