using R2API.ItemDropAPITools;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace R2API.ItemDrop {

    public class DropOdds : MonoBehaviour {

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

        private static readonly List<string> ShrineInteractables = new List<string> {
            "ShrineChance"
        };

        private static readonly Dictionary<string, List<float>> ShrineTierOdds = new Dictionary<string, List<float>>();

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

        private static readonly List<string> DropTableInteractables = new List<string> {
            "CasinoChest"
        };

        private static readonly Dictionary<string, List<float>> DropTableTierOdds = new Dictionary<string, List<float>>();

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

        private static readonly List<string> DropTableItemInteractables = new List<string> {
            "ShrineCleanse"
        };

        private static readonly Dictionary<string, List<float>> DropTableItemOdds = new Dictionary<string, List<float>>();

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
