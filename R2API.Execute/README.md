# R2API.Execute

## About

R2API.Execute is a submodule assembly for R2API that allows mod creators to add custom sources of Execute that are able to stack with each other.

## Use Cases / Features

R2API.Execute can be used for mod creators to add custom sources of Execute to the game via the ExecuteAPI class.

Execute sources added via this module will not stack with Freeze and Guillotines, as the goal is to avoid changing Vanilla functionality.

Execute sources added via R2API.Execute stack additively with diminishing returns, using the following formula: `1 - (1/(1 + executeFractionAdd))`

To add an execute source:
* `ExecuteAPI.CalculateExecuteThreshold` allows mod creators to increment the execute fraction.
* `ExecuteAPI.CalculateExecuteThresholdForViewer` is run after the above, and will factor in the attacker that is viewing the target's healthbar. (similar to Old Guillotine)

## Changelog

### '1.0.1'
* Fixed `ExecuteAPI.CalculateExecuteThresholdForViewer` not being invoked in TryExecuteServer.
* Fixed execute healthbar visuals not updating due to the HealthBarValues struct not being passed properly.

### '1.0.0'
* Release
