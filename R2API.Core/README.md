# R2API.Core - Core Module of R2API

## About

R2API.Core is the main module of R2API, and contains the backbone and framework for all the other submodule assemblies of R2API, providing utilities and tools for developing new API's.

By itself it doesnt change how the game behaves in any way shape or form.

## Use Cases / Features

R2API.Core is used in all the submodule assemblies of R2API, as such, it is necessary for most if not all other submodules to function properly.

## Related Pages

A lot of documentation is in the included xmldocs, and further information may be on the dedicated [R2API wiki](https://github.com/risk-of-thunder/R2API/wiki).

Do not hestiate to ask in [the modding discord](https://discord.gg/5MbXZvd) too!

## Changelog

### '5.0.4'
* Added SystemInitializerInjector class to the Utils namespace

### '5.0.3'
* Also move NetworkCompatibility initialization back to the Core module.

### '5.0.2'
* Move NetworkCompatibility back to the Core module for back compatibility reasons.

### '5.0.1'
* Add back an utility class that was removed by mistake.

### '5.0.0'
* Split from the main R2API.dll into its own submodule.
