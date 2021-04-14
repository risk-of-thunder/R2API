using R2API.ItemDropAPITools;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace R2API.ItemDrop {

    public class DropOdds : MonoBehaviour {
        /*
            There are many sources of dropped items.
            These sources will often first choose a particular subset of items
                and only then choose the final item, from within that subset.
            Typically the items within a subset have equal odds of dropping.
            But the subsets themselves have differing odds of being picked.

            This class has functions to deal with the different sources of item drops that choose between multiple subsets.
            These functions are called for each interactable as it is spawned into the scene.
            These functions will nullify the odds of subsets being selected if that susbset is unpopulated.

            SIDE NOTE:
            I wrote this class a long time ago.
            At the beginning of each function the original odds for each subset for each interactable is backed up.
            Then the odds for each subset for each interactable are restored.
            Then the odds for the relevant subsets are nullified.
        */

        //  These are the sources of item drops that utilize the ChestBehavior class and have pick between subsets.
        private static readonly List<string> ChestInteractables = new List<string> {
            "Chest1",
            "Chest2",
            "CategoryChestDamage",
            "CategoryChestHealing",
            "CategoryChestUtility",
            "Chest1Stealthed",
            "Lockbox",
            "ScavBackpack"
        };

        private static readonly Dictionary<string, List<float>> ChestTierOdds = new Dictionary<string, List<float>>();

        /*
            This will update the subset odds for the interactables that utilize ChestBehavior.
        */
        public static void UpdateChestTierOdds(SpawnCard spawnCard, string interactableName) {
            if (ChestInteractables.Contains(interactableName)) {
                ChestBehavior chestBehavior = spawnCard.prefab.GetComponent<ChestBehavior>();
                if (!ChestTierOdds.ContainsKey(interactableName)) {
                    ChestTierOdds.Add(interactableName, new List<float>());
                    ChestTierOdds[interactableName].Add(chestBehavior.tier1Chance);
                    ChestTierOdds[interactableName].Add(chestBehavior.tier2Chance);
                    ChestTierOdds[interactableName].Add(chestBehavior.tier3Chance);
                }

                if (ChestTierOdds.ContainsKey(interactableName)) {
                    chestBehavior.tier1Chance = ChestTierOdds[interactableName][0];
                    chestBehavior.tier2Chance = ChestTierOdds[interactableName][1];
                    chestBehavior.tier3Chance = ChestTierOdds[interactableName][2];
                }
                
                if (ItemDropAPI.PlayerInteractables.SubsetTiersPresent.ContainsKey(interactableName)) {
                    //  This is for damage, healing and utility chests
                    if (!ItemDropAPI.PlayerInteractables.SubsetTiersPresent[interactableName][InteractableCalculator.DropType.tier1]) {
                        chestBehavior.tier1Chance = 0;
                    }
                    if (!ItemDropAPI.PlayerInteractables.SubsetTiersPresent[interactableName][InteractableCalculator.DropType.tier2]) {
                        chestBehavior.tier2Chance = 0;
                    }
                    if (!ItemDropAPI.PlayerInteractables.SubsetTiersPresent[interactableName][InteractableCalculator.DropType.tier3]) {
                        chestBehavior.tier3Chance = 0;
                    }
                }
                else {
                    //  This is for everything else
                    if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier1]) {
                        chestBehavior.tier1Chance = 0;
                    }
                    if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier2]) {
                        chestBehavior.tier2Chance = 0;
                    }
                    if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier3]) {
                        chestBehavior.tier3Chance = 0;
                    }
                }
            }
        }

        //  These are the sources of item drops that utilize the ShrineChanceBehavior class and have pick between subsets.
        private static readonly List<string> ShrineInteractables = new List<string> {
            "ShrineChance"
        };

        private static readonly Dictionary<string, List<float>> ShrineTierOdds = new Dictionary<string, List<float>>();


        /*
            This will update the subset odds for the interactables that utilize ShrineChanceBehavior.
        */
        public static void UpdateShrineTierOdds(DirectorCard directorCard, string interactableName) {
            if (ShrineInteractables.Contains(interactableName)) {
                ShrineChanceBehavior shrineBehavior = directorCard.spawnCard.prefab.GetComponent<ShrineChanceBehavior>();
                if (!ShrineTierOdds.ContainsKey(interactableName)) {
                    ShrineTierOdds.Add(interactableName, new List<float>());
                    ShrineTierOdds[interactableName].Add(shrineBehavior.tier1Weight);
                    ShrineTierOdds[interactableName].Add(shrineBehavior.tier2Weight);
                    ShrineTierOdds[interactableName].Add(shrineBehavior.tier3Weight);
                    ShrineTierOdds[interactableName].Add(shrineBehavior.equipmentWeight);
                }

                if (ShrineTierOdds.ContainsKey(interactableName)) {
                    shrineBehavior.tier1Weight = ShrineTierOdds[interactableName][0];
                    shrineBehavior.tier2Weight = ShrineTierOdds[interactableName][1];
                    shrineBehavior.tier3Weight = ShrineTierOdds[interactableName][2];
                    shrineBehavior.equipmentWeight = ShrineTierOdds[interactableName][3];
                }

                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier1]) {
                    shrineBehavior.tier1Weight = 0;
                }
                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier2]) {
                    shrineBehavior.tier2Weight = 0;
                }
                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier3]) {
                    shrineBehavior.tier3Weight = 0;
                }
                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.equipment]) {
                    shrineBehavior.equipmentWeight = 0;
                }
            }
        }

        //  These are the sources of item drops that utilize the RouletteChestController class and have pick between subsets.
        private static readonly List<string> DropTableInteractables = new List<string> {
            "CasinoChest"
        };

        private static readonly Dictionary<string, List<float>> DropTableTierOdds = new Dictionary<string, List<float>>();

        /*
            This will update the subset odds for the interactables that utilize RouletteChestController.
        */
        public static void UpdateDropTableTierOdds(SpawnCard spawnCard, string interactableName) {
            if (DropTableInteractables.Contains(interactableName)) {
                BasicPickupDropTable dropTable = spawnCard.prefab.GetComponent<RouletteChestController>().dropTable as BasicPickupDropTable;
                if (!DropTableTierOdds.ContainsKey(interactableName)) {
                    DropTableTierOdds.Add(interactableName, new List<float>());
                    DropTableTierOdds[interactableName].Add(dropTable.tier1Weight);
                    DropTableTierOdds[interactableName].Add(dropTable.tier2Weight);
                    DropTableTierOdds[interactableName].Add(dropTable.tier3Weight);
                    DropTableTierOdds[interactableName].Add(dropTable.equipmentWeight);
                }
                if (DropTableTierOdds.ContainsKey(interactableName)) {
                    dropTable.tier1Weight = DropTableTierOdds[interactableName][0];
                    dropTable.tier2Weight = DropTableTierOdds[interactableName][1];
                    dropTable.tier3Weight = DropTableTierOdds[interactableName][2];
                    dropTable.equipmentWeight = DropTableTierOdds[interactableName][3];
                }
                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier1]) {
                    dropTable.tier1Weight = 0;
                }
                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier2]) {
                    dropTable.tier2Weight = 0;
                }
                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier3]) {
                    dropTable.tier3Weight = 0;
                }
                if (!ItemDropAPI.PlayerInteractables.TiersPresent[InteractableCalculator.DropType.equipment]) {
                    dropTable.equipmentWeight = 0;
                }

                dropTable.GenerateWeightedSelection(Run.instance);
            }
        }

        //  These are the sources of item drops that utilize the ExplicitPickupDropTable class and have pick between specific boss items.
        private static readonly List<string> DropTableItemInteractables = new List<string> {
            "ShrineCleanse"
        };

        private static readonly Dictionary<string, List<float>> DropTableItemOdds = new Dictionary<string, List<float>>();

        /*
            This will update the items odds for the interactables that utilize ExplicitPickupDropTable for specific boss items.
        */
        public static void UpdateDropTableItemOdds(DropList dropList, ExplicitPickupDropTable dropTable, string interactableName) {
            if (DropTableItemInteractables.Contains(interactableName)) {
                if (!DropTableItemOdds.ContainsKey(interactableName)) {
                    DropTableItemOdds.Add(interactableName, new List<float>());
                    foreach (ExplicitPickupDropTable.Entry entry in dropTable.entries) {
                        DropTableItemOdds[interactableName].Add(entry.pickupWeight);
                    }
                }

                if (DropTableItemOdds.ContainsKey(interactableName)) {
                    for (int entryIndex = 0; entryIndex < dropTable.entries.Length; entryIndex++) {
                        dropTable.entries[entryIndex].pickupWeight = DropTableItemOdds[interactableName][entryIndex];
                    }
                }
                for (int entryIndex = 0; entryIndex < dropTable.entries.Length; entryIndex++) {
                    if (!dropList.AvailableBossDropList.Contains(PickupCatalog.FindPickupIndex(dropTable.entries[entryIndex].pickupName))) {
                        dropTable.entries[entryIndex].pickupWeight = 0;
                    }
                }

                //dropTable.GenerateWeightedSelection();
            }
        }
    }
}
