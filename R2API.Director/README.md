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

### '2.1.0'
* Added Dependency for the Addressables Submodule
* Added AddressableDCCSPool and AddressableDirectorCardCategorySelection, both intended for usage in the Unity Editor.

### '2.0.0'
* Fixed issue where DirectorCardHolder's custom category selection weights where integers instead of floats.

### '1.1.2'
* Make the API safer.

### '1.1.1'
* Fix issue where the ``Stage ParseToInternalStageName(string)`` and ``string ToInternalStageName(Stage stage)`` would return invalid values for The Planetarium and Void Locus respectively.

### '1.1.0'
* Tentative fix a bug where family events could occur unannounced, and earlier than expected (for the Void & Lunar family). Usually, this would only become apparent if multiple runs are completed in a single game session. Thanks to 6thmoon for helping finding the root cause of the bug. If the bug is still not fixed do not hesitate to tell us in the modding discord!
* Add to most `DirectorAPI.Helpers` methods an optional predicate parameter for more granular control on whether or not a monster / interactable should be added / removed to / from a stage. The predicate have a `DirectorCardCategorySelection` parameter that you can inspect (you don't have to, you can do other checks instead, like if a specific `ExpansionDef` is active) to decide whether or not the monster / interactable should be added / removed.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
