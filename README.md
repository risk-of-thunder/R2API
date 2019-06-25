
# R2API – a modding API for Risk of Rain 2
[![Build Status](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_apis/build/status/Risk%20of%20Rain%202%20Modding-.NET%20Desktop-CI?branchName=master)](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_build/latest?definitionId=1&branchName=master)

## About

R2API is designed to provide an abstraction layer in order to expose and simplify APIs to allow developers to modify
Risk of Rain 2 more easily, while still keeping mod interoperability in mind.

At it's heart, R2API is meant to be base work, which is why it doesnt include any additional in game functionality,
be it for developing or for users in general. **This is by choice.** A game with just R2API should feel and behave the
same as an unmodded game, the `isModded` flag excluded.

## Installation

The latest stable version is *NO LONGER* included in the [BepInEx Pack](https://thunderstore.io/package/bbepis/BepInExPack/) on Thunderstore.

> #### Bleeding Edge
> Latest bleeding edge builds of `master` are hosted on [Azure](https://raegous.visualstudio.com/Risk%20of%20Rain%202%20Modding/_build/latest?definitionId=1&branchName=master),
> and may be downloaded using the `Artifacts` drop down menu.
> The contents of `R2API` should be extracted into the `BepInEx` folder.
