# R2API.Items - Item, ItemTags, Equipments, ItemDisplays and more 
## About

R2API.Items is a submodule assembly for R2API that allows mod creators to add new Items, Equipments, ItemTags, and manage the Items and Equipment's ItemDisplays.

## Use Cases / Features

R2API.Items is mainly used for adding new ItemDefs and EquipmentDefs to the game, To facilitate the inclusion of Itemdisplays, R2API.Items provides wrapper classes for both Items and Equipments.

    CustomItem: Represents a new ItemDef being added, includes a field for handling the Item's ItemDisplayRules
    CustomEquipment: Represents a new EquipmentDef being added, includes a field for handling the Equipment's ItemDisplayRules.

The ItemDisplayRuleDictionary is a class included with the module, which provides utilities and systems for adding new ItemDisplays to your items, including default display rules if a character does not provide it's own item display for your item.

Finally, ItemAPI includes the ability to add new ItemTags to the game, which can be used for multiple things ingame.

## Related Pages

A detailed tutorial on how to make custom items using ItewmAPI can be found [here](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Items/)

## Changelog

### '1.0.1'
* Fix invalid IDRS checker telling IDRS are not correct when they actually are.

### '1.0.0'
* Split from the main R2API.dll into its own submodule.
