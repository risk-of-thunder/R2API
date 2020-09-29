using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using BepInEx.Logging;
using R2API.Utils;
using R2API.ItemDropAPITools;
using UnityEngine;
using UnityEngine.Events;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class InteractablesAPI {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }
        private static bool _loaded;

        private static bool defaultInteractablesLoaded = false;
        public static Dictionary<string, InteractableSpawnCard> defaultInteractables = new Dictionary<string, InteractableSpawnCard>();

        private static Dictionary<string, DirectorCardCategorySelection.Category> _categoriesToAdd = new Dictionary<string, DirectorCardCategorySelection.Category>() {
        };

        static public Dictionary<string, DirectorCardCategorySelection.Category> categoriesToAdd {
            get { return _categoriesToAdd; }
            private set { _categoriesToAdd = value; }
        }

        private static List<string> _categoriesToRemove = new List<string>() {
        };

        static public List<string> categoriesToRemove {
            get { return _categoriesToRemove; }
            private set { _categoriesToRemove = value; }
        }

        private static Dictionary<string, Dictionary<string, InteractableSpawnCard>> _interactablesToAdd = new Dictionary<string, Dictionary<string, InteractableSpawnCard>>() {
        };

        static public Dictionary<string, Dictionary<string, InteractableSpawnCard>> interactablesToAdd {
            get { return _interactablesToAdd; }
            private set { _interactablesToAdd = value; }
        }

        private static Dictionary<string, Dictionary<string, int>> interactablesToAddWeight = new Dictionary<string, Dictionary<string, int>>() {
        };

        private static Dictionary<string, List<string>> _interactablesToRemove = new Dictionary<string, List<string>>() {
        };

        static public Dictionary<string, List<string>> interactablesToRemove {
            get { return _interactablesToRemove; }
            private set { _interactablesToRemove = value; }
        }



        private static Dictionary<string, float> _categoryWeight = new Dictionary<string, float>() {
        };

        static public Dictionary<string, float> categoryWeight {
            get { return _categoryWeight; }
            private set { _categoryWeight = value; }
        }

        private static Dictionary<string, Dictionary<string, int>> _interactableWeight = new Dictionary<string, Dictionary<string, int>>() {
        };

        static public Dictionary<string, Dictionary<string, int>> interactableWeight {
            get { return _interactableWeight; }
            private set { _interactableWeight = value; }
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            GetDefaultInteractables();
            On.RoR2.SceneDirector.PopulateScene += PopulateScene;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.SceneDirector.PopulateScene -= PopulateScene;
        }

        public static void LogAllCategories() {
            if (ClassicStageInfo.instance != null) {
                R2API.print("----");
                List<string> categoryNames = new List<string>();
                foreach (DirectorCardCategorySelection.Category category in ClassicStageInfo.instance.interactableCategories.categories) {
                    categoryNames.Add(category.name);
                    R2API.print(category.name);
                    foreach (DirectorCard directorCard in category.cards) {
                        R2API.print(directorCard.spawnCard.name);
                    }
                    R2API.print("-");
                }
                R2API.print("----");
            }
        }

        private static void GetDefaultInteractables() {
            if (!defaultInteractablesLoaded) {
                RoR2.InteractableSpawnCard[] allInteractables = UnityEngine.Resources.LoadAll<RoR2.InteractableSpawnCard>("SpawnCards/InteractableSpawnCard");
                foreach (RoR2.InteractableSpawnCard spawnCard in allInteractables) {
                    if (!defaultInteractables.ContainsKey(spawnCard.name)) {
                        defaultInteractables.Add(spawnCard.name, spawnCard);
                    }
                }
                defaultInteractablesLoaded = true;
            }
        }

        static private void PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector sceneDirector) {
            Dictionary<string, DirectorCardCategorySelection.Category> categories = new Dictionary<string, DirectorCardCategorySelection.Category>();
            foreach (DirectorCardCategorySelection.Category category in ClassicStageInfo.instance.interactableCategories.categories) {
                if (!categories.ContainsKey(category.name)) {
                    categories.Add(category.name, category);
                }
            }
            foreach (string categoryName in categoriesToAdd.Keys) {
                if (!categories.ContainsKey(categoryName)) {
                    categories.Add(categoryName, categoriesToAdd[categoryName]);
                }
            }
            foreach (string categoryName in categoriesToRemove) {
                if (categories.ContainsKey(categoryName)) {
                    categories.Remove(categoryName);
                }
            }
            foreach (string categoryName in interactablesToAdd.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    Dictionary<string, DirectorCard> directorCards = new Dictionary<string, DirectorCard>();
                    foreach (DirectorCard directorCard in categories[categoryName].cards) {
                        if (!directorCards.ContainsKey(directorCard.spawnCard.name)) {
                            directorCards.Add(directorCard.spawnCard.name, directorCard);
                        }
                    }
                    foreach (string interactableName in interactablesToAdd[categoryName].Keys) {
                        if (!directorCards.ContainsKey(interactableName)) {
                            DirectorCard directorCard = new DirectorCard();
                            directorCard.spawnCard = interactablesToAdd[categoryName][interactableName];
                            directorCard.selectionWeight = interactablesToAddWeight[categoryName][interactableName];
                            directorCards.Add(interactableName, directorCard);
                        }
                    }

                    int cardIndex = 0;
                    DirectorCard[] directorCardsArray = new DirectorCard[directorCards.Count];
                    foreach (DirectorCard directorCard in directorCards.Values) {
                        directorCardsArray[cardIndex] = directorCard;
                        cardIndex += 1;
                    }
                    DirectorCardCategorySelection.Category category = categories[categoryName];
                    category.cards = directorCardsArray;
                    categories[categoryName] = category;
                }
            }
            foreach (string categoryName in interactablesToRemove.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    List<DirectorCard> directorCards = new List<DirectorCard>();
                    foreach (DirectorCard directorCard in categories[categoryName].cards) {
                        if (!interactablesToRemove[categoryName].Contains(directorCard.spawnCard.name)) {
                            directorCards.Add(directorCard);
                        }
                    }
                    int cardIndex = 0;
                    DirectorCard[] directorCardsArray = new DirectorCard[directorCards.Count];
                    foreach (DirectorCard directorCard in directorCards) {
                        directorCardsArray[cardIndex] = directorCard;
                        cardIndex += 1;
                    }
                    DirectorCardCategorySelection.Category category = categories[categoryName];
                    category.cards = directorCardsArray;
                    categories[categoryName] = category;
                }
            }
            foreach (string categoryName in categoryWeight.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    DirectorCardCategorySelection.Category category = categories[categoryName];
                    category.selectionWeight = categoryWeight[categoryName];
                    categories[categoryName] = category;
                }
            }
            foreach (string categoryName in interactableWeight.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    for (int cardIndex = 0; cardIndex < categories[categoryName].cards.Length; cardIndex++) {
                        if (interactableWeight[categoryName].ContainsKey(categories[categoryName].cards[cardIndex].spawnCard.name)) {
                            categories[categoryName].cards[cardIndex].selectionWeight = interactableWeight[categoryName][categories[categoryName].cards[cardIndex].spawnCard.name];
                        }
                    }
                }
            }
            int categoryIndex = 0;
            DirectorCardCategorySelection.Category[] categoriesArray = new DirectorCardCategorySelection.Category[categories.Keys.Count];
            foreach (DirectorCardCategorySelection.Category category in categories.Values) {
                categoriesArray[categoryIndex] = category;
                categoryIndex += 1;
            }
            ClassicStageInfo.instance.interactableCategories.categories = categoriesArray;
            orig(sceneDirector);
        }

        public static void SetCategoryWeight(string category, float weight) {
            if (weight < 0) {
                SetCategoryWeightToDefault(category);
            } else {
                if (!categoryWeight.ContainsKey(category)) {
                    categoryWeight.Add(category, 0);
                }
                categoryWeight[category] = weight;
            }
        }

        public static void SetCategoryWeightToDefault(string category) {
            if (categoryWeight.ContainsKey(category)) {
                categoryWeight.Remove(category);
            }
        }

        public static void SetInteractableWeight(string category, string interactable, int weight) {
            if (weight < 0) {
                SetInteractableWeightToDefault(category, interactable);
            } else {
                if (!interactableWeight.ContainsKey(category)) {
                    interactableWeight.Add(category, new Dictionary<string, int>());
                }
                if (!interactableWeight[category].ContainsKey(interactable)) {
                    interactableWeight[category].Add(interactable, 0);
                }
                interactableWeight[category][interactable] = weight;
            }
        }

        public static void SetInteractableWeightToDefault(string category, string interactable) {
            if (interactableWeight.ContainsKey(category) && interactableWeight[category].ContainsKey(interactable)) {
                interactableWeight[category].Remove(interactable);
                if (interactableWeight[category].Keys.Count == 0) {
                    interactableWeight.Remove(category);
                }
            }
        }

        public static void AddCategory(DirectorCardCategorySelection.Category category) {
            if (!categoriesToAdd.ContainsKey(category.name)) {
                categoriesToAdd.Add(category.name, new DirectorCardCategorySelection.Category());
            }
            categoriesToAdd[category.name] = category;
        }

        public static void UnaddCategory(string categoryName) {
            if (categoriesToAdd.ContainsKey(categoryName)) {
                categoriesToAdd.Remove(categoryName);
            }
        }

        public static void RemoveCategory(string category) {
            if (!categoriesToRemove.Contains(category)) {
                categoriesToRemove.Add(category);
            }
        }

        public static void UnremoveCategory(string category) {
            if (categoriesToRemove.Contains(category)) {
                categoriesToRemove.Remove(category);
            }
        }

        public static void AddInteractable(string category, InteractableSpawnCard interactable, int weight) {
            if (!interactablesToAdd.ContainsKey(category)) {
                interactablesToAdd.Add(category, new Dictionary<string, InteractableSpawnCard>());
                interactablesToAddWeight.Add(category, new Dictionary<string, int>());
            }
            if (!interactablesToAdd[category].ContainsKey(interactable.name)) {
                interactablesToAdd[category].Add(interactable.name, null);
                interactablesToAddWeight[category].Add(interactable.name, 0);
            }
            interactablesToAdd[category][interactable.name] = interactable;
            interactablesToAddWeight[category][interactable.name] = weight;
        }

        public static void UnaddInteractable(string category, string interactable) {
            if (interactablesToAdd.ContainsKey(category)) {
                if (interactablesToAdd[category].ContainsKey(interactable)) {
                    interactablesToAdd[category].Remove(interactable);
                    interactablesToAddWeight[category].Remove(interactable);
                    if (interactablesToAdd[category].Keys.Count == 0) {
                        interactablesToAdd.Remove(category);
                        interactablesToAddWeight.Remove(category);
                    }
                }
            }
        }

        public static void RemoveInteractable(string category, string interactable) {
            if (!interactablesToRemove.ContainsKey(category)) {
                interactablesToRemove.Add(category, new List<string>());
            }
            if (!interactablesToRemove[category].Contains(interactable)) {
                interactablesToRemove[category].Add(interactable);
            }
        }

        public static void UnremoveInteractable(string category, string interactable) {
            if (interactablesToRemove.ContainsKey(category)) {
                if (interactablesToRemove[category].Contains(interactable)) {
                    interactablesToRemove[category].Remove(interactable);
                    if (interactablesToRemove[category].Count == 0) {
                        interactablesToRemove.Remove(category);
                    }
                }
            }
        }
    }
}
