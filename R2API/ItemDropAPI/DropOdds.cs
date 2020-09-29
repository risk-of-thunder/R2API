using System;
using System.Collections.Generic;
using UnityEngine;
using RoR2;

namespace R2API {
    namespace ItemDropAPITools {
        public class DropOdds : MonoBehaviour {
            static private List<string> chestInteractables = new List<string>() {
                "Chest1",
                "Chest2",
                "CategoryChestDamage",
                "CategoryChestHealing",
                "CategoryChestUtility",
                "Chest1Stealthed",
                "Lockbox",
                "ScavBackpack",
            };

            static private Dictionary<string, List<float>> chestTierOdds = new Dictionary<string, List<float>>();

            static public void UpdateChestTierOdds(SpawnCard spawnCard, string interactableName) {
                if (chestInteractables.Contains(interactableName)) {
                    ChestBehavior chestBehavior = spawnCard.prefab.GetComponent<ChestBehavior>();
                    if (!chestTierOdds.ContainsKey(interactableName)) {
                        chestTierOdds.Add(interactableName, new List<float>());
                        chestTierOdds[interactableName].Add(chestBehavior.tier1Chance);
                        chestTierOdds[interactableName].Add(chestBehavior.tier2Chance);
                        chestTierOdds[interactableName].Add(chestBehavior.tier3Chance);
                    }

                    if (chestTierOdds.ContainsKey(interactableName)) {
                        chestBehavior.tier1Chance = chestTierOdds[interactableName][0];
                        chestBehavior.tier2Chance = chestTierOdds[interactableName][1];
                        chestBehavior.tier3Chance = chestTierOdds[interactableName][2];
                    }
                    if (ItemDropAPI.playerInteractables.subsetTiersPresent.ContainsKey(interactableName)) {
                        if (!ItemDropAPI.playerInteractables.subsetTiersPresent[interactableName]["tier1"]) {
                            chestBehavior.tier1Chance = 0;
                        }
                        if (!ItemDropAPI.playerInteractables.subsetTiersPresent[interactableName]["tier2"]) {
                            chestBehavior.tier2Chance = 0;
                        }
                        if (!ItemDropAPI.playerInteractables.subsetTiersPresent[interactableName]["tier3"]) {
                            chestBehavior.tier3Chance = 0;
                        }
                    } else {
                        if (!ItemDropAPI.playerInteractables.tiersPresent["tier1"]) {
                            chestBehavior.tier1Chance = 0;
                        }
                        if (!ItemDropAPI.playerInteractables.tiersPresent["tier2"]) {
                            chestBehavior.tier2Chance = 0;
                        }
                        if (!ItemDropAPI.playerInteractables.tiersPresent["tier3"]) {
                            chestBehavior.tier3Chance = 0;
                        }
                    }
                }
            }

            static private List<string> shrineInteractables = new List<string>() {
                "ShrineChance",
            };

            static private Dictionary<string, List<float>> shrineTierOdds = new Dictionary<string, List<float>>();

            static public void UpdateShrineTierOdds(DirectorCard directorCard, string interactableName) {
                if (shrineInteractables.Contains(interactableName)) {
                    ShrineChanceBehavior shrineBehavior = directorCard.spawnCard.prefab.GetComponent<ShrineChanceBehavior>();
                    if (!shrineTierOdds.ContainsKey(interactableName)) {
                        shrineTierOdds.Add(interactableName, new List<float>());
                        shrineTierOdds[interactableName].Add(shrineBehavior.tier1Weight);
                        shrineTierOdds[interactableName].Add(shrineBehavior.tier2Weight);
                        shrineTierOdds[interactableName].Add(shrineBehavior.tier3Weight);
                        shrineTierOdds[interactableName].Add(shrineBehavior.equipmentWeight);
                    }

                    if (shrineTierOdds.ContainsKey(interactableName)) {
                        shrineBehavior.tier1Weight = shrineTierOdds[interactableName][0];
                        shrineBehavior.tier2Weight = shrineTierOdds[interactableName][1];
                        shrineBehavior.tier3Weight = shrineTierOdds[interactableName][2];
                        shrineBehavior.equipmentWeight = shrineTierOdds[interactableName][3];
                    }

                    if (!ItemDropAPI.playerInteractables.tiersPresent["tier1"]) {
                        shrineBehavior.tier1Weight = 0;
                    }
                    if (!ItemDropAPI.playerInteractables.tiersPresent["tier2"]) {
                        shrineBehavior.tier2Weight = 0;
                    }
                    if (!ItemDropAPI.playerInteractables.tiersPresent["tier3"]) {
                        shrineBehavior.tier3Weight = 0;
                    }
                    if (!ItemDropAPI.playerInteractables.tiersPresent["equipment"]) {
                        shrineBehavior.equipmentWeight = 0;
                    }
                }
            }

            static private List<string> dropTableInteractables = new List<string>() {
                "CasinoChest",
            };

            static private Dictionary<string, List<float>> dropTableTierOdds = new Dictionary<string, List<float>>();

            static public void UpdateDropTableTierOdds(SpawnCard spawnCard, string interactableName) {
                if (dropTableInteractables.Contains(interactableName)) {
                    BasicPickupDropTable dropTable = spawnCard.prefab.GetComponent<RouletteChestController>().dropTable as BasicPickupDropTable;
                    if (!dropTableTierOdds.ContainsKey(interactableName)) {
                        dropTableTierOdds.Add(interactableName, new List<float>());
                        dropTableTierOdds[interactableName].Add(dropTable.tier1Weight);
                        dropTableTierOdds[interactableName].Add(dropTable.tier2Weight);
                        dropTableTierOdds[interactableName].Add(dropTable.tier3Weight);
                        dropTableTierOdds[interactableName].Add(dropTable.equipmentWeight);
                    }
                    if (dropTableTierOdds.ContainsKey(interactableName)) {
                        dropTable.tier1Weight = dropTableTierOdds[interactableName][0];
                        dropTable.tier2Weight = dropTableTierOdds[interactableName][1];
                        dropTable.tier3Weight = dropTableTierOdds[interactableName][2];
                        dropTable.equipmentWeight = dropTableTierOdds[interactableName][3];
                    }
                    if (!ItemDropAPI.playerInteractables.tiersPresent["tier1"]) {
                        dropTable.tier1Weight = 0;
                    }
                    if (!ItemDropAPI.playerInteractables.tiersPresent["tier2"]) {
                        dropTable.tier2Weight = 0;
                    }
                    if (!ItemDropAPI.playerInteractables.tiersPresent["tier3"]) {
                        dropTable.tier3Weight = 0;
                    }
                    if (!ItemDropAPI.playerInteractables.tiersPresent["equipment"]) {
                        dropTable.equipmentWeight = 0;
                    }
                    System.Type type = typeof(RoR2.BasicPickupDropTable);
                    System.Reflection.MethodInfo method = type.GetMethod("GenerateWeightedSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method.Invoke(dropTable, new object[] { Run.instance });
                }
            }

            static private List<string> dropTableItemInteractables = new List<string>() {
                "ShrineCleanse",
            };

            static private Dictionary<string, List<float>> dropTableItemOdds = new Dictionary<string, List<float>>();

            static public void UpdateDropTableItemOdds(DropList dropList, ExplicitPickupDropTable dropTable, string interactableName) {
                if (dropTableItemInteractables.Contains(interactableName)) {
                    if (!dropTableItemOdds.ContainsKey(interactableName)) {
                        dropTableItemOdds.Add(interactableName, new List<float>());
                        foreach (ExplicitPickupDropTable.Entry entry in dropTable.entries) {
                            dropTableItemOdds[interactableName].Add(entry.pickupWeight);
                        }
                    }

                    if (dropTableItemOdds.ContainsKey(interactableName)) {
                        for (int entryIndex = 0; entryIndex < dropTable.entries.Length; entryIndex++) {
                            dropTable.entries[entryIndex].pickupWeight = dropTableItemOdds[interactableName][entryIndex];
                        }
                    }
                    for (int entryIndex = 0; entryIndex < dropTable.entries.Length; entryIndex++) {
                        if (!dropList.availableBossDropList.Contains(PickupCatalog.FindPickupIndex(dropTable.entries[entryIndex].pickupName))) {
                            dropTable.entries[entryIndex].pickupWeight = 0;
                        }
                    }
                    //System.Type type = typeof(RoR2.ExplicitPickupDropTable);
                    //System.Reflection.MethodInfo method = type.GetMethod("GenerateWeightedSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    //method.Invoke(dropTable, new object[0]);
                }
            }
        }
	}
}
