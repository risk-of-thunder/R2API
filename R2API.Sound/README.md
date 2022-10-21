# R2API.Sound - WWise Soundbank loading and MusicTrack addition

## About

R2API.Sound is a submodule assembly for R2API that allows mod creators to easily load SoundBanks from WWise and implement new Music Tracks for the game.

## Use Cases / Features

R2API.Sound works via two main classes, the SoundBanks class and the Music class.

SoundBanks class allows you to load your WWise SoundBanks to the game you can later use the SoundBank's event names in the game's Util.PlaySound method.

The Music class allows you to add new music tracks to the game, these new tracks can later be used for playing music in different stages, overriding music, and more.
The Music class is mainly used for mods that do not have access to properly populating the Game's MusicTrackDef scriptable object, as that scriptable object requires the mod creator to have access to WWise's Unity Integration.

## Related Pages

A guide on how to add your own music can be found [here](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Sounds/WWise/Custom-Music/), the guide uses R2API.Sound's Music class.

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.