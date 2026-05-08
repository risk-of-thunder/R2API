# R2API.Execute

## About

R2API.Execute is a submodule assembly for R2API that allows mod creators to add custom sources of Execute that are able to stack with each other.

## Use Cases / Features

R2API.Execute can be used for mod creators to add custom sources of Execute to the game via the ExecuteAPI class.

Execute sources added via this module will not stack with Freeze and Guillotines, as the goal is to avoid changing Vanilla functionality.

To add a non-stacking execute source that behaves like Vanilla:
* `ExecuteAPI.CalculateExecuteThreshold` keeps track of the current execute fraction with a float `highestExecuteThreshold`. To match Vanilla behavior, override the value of this when appropriate.
* `ExecuteAPI.CalculateExecuteThresholdForViewer` is similar, but factors in the Attacker/Person viewing the healthbar.

To add a stackable execute source:
* `ExecuteAPI.CalculateAdditiveExecuteThreshold` allows mod creators to increment the execute fraction additively with diminishing returns with the following formula: `1 - (1/(1 + executeFractionAdd))`
* `ExecuteAPI.CalculateAdditiveExecuteThresholdForViewer` is similar, but factors in the Attacker/Person viewing the healthbar.

## Changelog

### '1.1.2'

* Fixed Execute bar showing for bodies with the ImmuneToExecute flag.

### '1.1.1'

* Internal refactoring to reduce code duplication.

### '1.1.0'

* `ExecuteAPI.CalculateExecuteThreshold` and `ExecuteAPI.CalculateExecuteThresholdForViewer` now targets flat execute threshold modification to be in-line with Vanilla.
* Additive Execute Threshold modification has been moved to `ExecuteAPI.CalculateAdditiveExecuteThreshold` and `ExecuteAPI.CalculateAdditiveExecuteThresholdForViewer`
* Fixed `ExecuteAPI.CalculateExecuteThresholdForViewer` not being invoked in TryExecuteServer.
* Fixed execute healthbar visuals not updating due to the HealthBarValues struct not being passed properly.

### '1.0.0'
* Release
