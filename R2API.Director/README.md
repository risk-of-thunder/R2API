# R2API.Director - Simplified addition of Enemies, Interactables and more to the Director Systems.

## About

R2API.Director is a submodule assembly for R2API that allows mod creators to add new Entries to the Director, which allows things such as Interactables, Enemies and more to spawn in runs.

## Use Cases / Features

R2API.Director is used for adding new Enemies and Interactables to the Director's SpawnCard pools, the main method of using this is with the events provided by the API, these events are:

* StageSettingsActions (Modify the the stage settings such as the stage's monster and interactable credits.)
* MonsterActions (Modify the stage's Monsters's DCCSPool)
* IntreractableActions (Modify the stage's Interactable's DCCSPool)

Alongside this, R2API.Director also comes bundled with DirectorAPIHelpers, which contains helper methods which greatly simplify interacting with the Events described above.

## Related Pages

## Changelog

### '1.0.0'
* Split from the main R2API.dll into its own submodule.