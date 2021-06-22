using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace R2API {

    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class InteractablesAPI {
        public const string InteractableSpawnCardPath = "SpawnCards/InteractableSpawnCard";

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static Dictionary<string, InteractableSpawnCard> _defaultInteractables =
            new Dictionary<string, InteractableSpawnCard>();

        public static Dictionary<string, InteractableSpawnCard> DefaultInteractables {
            get {
                if (_defaultInteractables == null) {
                    _defaultInteractables = new Dictionary<string, InteractableSpawnCard>();
                    var allInteractables = Resources.LoadAll<InteractableSpawnCard>(InteractableSpawnCardPath);
                    foreach (var spawnCard in allInteractables) {
                        if (!_defaultInteractables.ContainsKey(spawnCard.name)) {
                            _defaultInteractables.Add(spawnCard.name, spawnCard);
                        }
                    }
                }

                return _defaultInteractables;
            }
        }

        public static Dictionary<string, DirectorCardCategorySelection.Category> CategoriesToAdd { get; } =
            new Dictionary<string, DirectorCardCategorySelection.Category>();

        public static Dictionary<string, Dictionary<string, InteractableSpawnCard>> InteractablesToAdd { get; } =
            new Dictionary<string, Dictionary<string, InteractableSpawnCard>>();

        private static readonly Dictionary<string, Dictionary<string, int>> InteractablesToAddWeight =
            new Dictionary<string, Dictionary<string, int>>();

        public static List<string> CategoriesToRemove { get; } = new List<string>();

        public static Dictionary<string, List<string>> InteractablesToRemove { get; } =
            new Dictionary<string, List<string>>();

        public static Dictionary<string, float> CategoryWeight { get; } = new Dictionary<string, float>();

        public static Dictionary<string, Dictionary<string, int>> InteractableWeight { get; } =
            new Dictionary<string, Dictionary<string, int>>();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Run.Awake += RunAwake;
            On.RoR2.SceneDirector.PopulateScene += OnPopulateScene;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Run.Awake -= RunAwake;
            On.RoR2.SceneDirector.PopulateScene -= OnPopulateScene;
        }

        public static void LogAllCategories() {
            if (ClassicStageInfo.instance != null) {
                R2API.Logger.LogInfo("----");
                foreach (var category in ClassicStageInfo.instance.interactableCategories.categories) {
                    R2API.Logger.LogInfo(category.name);
                    foreach (var directorCard in category.cards) {
                        R2API.Logger.LogInfo(directorCard.spawnCard.name);
                    }
                    R2API.Logger.LogInfo("-");
                }
                R2API.Logger.LogInfo("----");
            }
        }

        private static void RunAwake(On.RoR2.Run.orig_Awake orig, Run run) {
            CategoriesToAdd.Clear();
            InteractablesToAdd.Clear();
            InteractablesToAddWeight.Clear();
            CategoriesToRemove.Clear();
            InteractablesToRemove.Clear();
            CategoryWeight.Clear();
            InteractableWeight.Clear();
            orig(run);
        }

        private static void OnPopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector sceneDirector) {
            var categories = new Dictionary<string, DirectorCardCategorySelection.Category>();
            foreach (var category in ClassicStageInfo.instance.interactableCategories.categories) {
                if (!categories.ContainsKey(category.name)) {
                    categories.Add(category.name, category);
                }
            }
            foreach (var categoryName in CategoriesToAdd.Keys) {
                if (!categories.ContainsKey(categoryName)) {
                    categories.Add(categoryName, CategoriesToAdd[categoryName]);
                }
            }
            foreach (var categoryName in CategoriesToRemove) {
                if (categories.ContainsKey(categoryName)) {
                    categories.Remove(categoryName);
                }
            }
            foreach (var categoryName in InteractablesToAdd.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    var directorCards = new Dictionary<string, DirectorCard>();
                    foreach (var directorCard in categories[categoryName].cards) {
                        if (!directorCards.ContainsKey(directorCard.spawnCard.name)) {
                            directorCards.Add(directorCard.spawnCard.name, directorCard);
                        }
                    }
                    foreach (var interactableName in InteractablesToAdd[categoryName].Keys) {
                        if (!directorCards.ContainsKey(interactableName)) {
                            var directorCard = new DirectorCard {
                                spawnCard = InteractablesToAdd[categoryName][interactableName],
                                selectionWeight = InteractablesToAddWeight[categoryName][interactableName]
                            };
                            directorCards.Add(interactableName, directorCard);
                        }
                    }

                    var cardIndex = 0;
                    var directorCardsArray = new DirectorCard[directorCards.Count];
                    foreach (var directorCard in directorCards.Values) {
                        directorCardsArray[cardIndex] = directorCard;
                        cardIndex += 1;
                    }
                    var category = categories[categoryName];
                    category.cards = directorCardsArray;
                    categories[categoryName] = category;
                }
            }
            foreach (var categoryName in InteractablesToRemove.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    var directorCards = new List<DirectorCard>();
                    foreach (var directorCard in categories[categoryName].cards) {
                        if (!InteractablesToRemove[categoryName].Contains(directorCard.spawnCard.name)) {
                            directorCards.Add(directorCard);
                        }
                    }
                    if (directorCards.Count > 0) {
                        var cardIndex = 0;
                        var directorCardsArray = new DirectorCard[directorCards.Count];
                        foreach (var directorCard in directorCards) {
                            directorCardsArray[cardIndex] = directorCard;
                            cardIndex += 1;
                        }
                        var category = categories[categoryName];
                        category.cards = directorCardsArray;
                        categories[categoryName] = category;
                    }
                    else {
                        categories.Remove(categoryName);
                    }
                }
            }
            foreach (var categoryName in CategoryWeight.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    var category = categories[categoryName];
                    category.selectionWeight = CategoryWeight[categoryName];
                    categories[categoryName] = category;
                }
            }
            foreach (var categoryName in InteractableWeight.Keys) {
                if (categories.ContainsKey(categoryName)) {
                    foreach (var directorCard in categories[categoryName].cards) {
                        if (InteractableWeight[categoryName].ContainsKey(directorCard.spawnCard.name)) {
                            directorCard.selectionWeight = InteractableWeight[categoryName][directorCard.spawnCard.name];
                        }
                    }
                }
            }
            var categoryIndex = 0;
            var categoriesArray = new DirectorCardCategorySelection.Category[categories.Keys.Count];
            foreach (var category in categories.Values) {
                categoriesArray[categoryIndex] = category;
                categoryIndex += 1;
            }
            ClassicStageInfo.instance.interactableCategories.categories = categoriesArray;
            if (categories.Count == 0) {
                sceneDirector.interactableCredit = 0;
            }
            orig(sceneDirector);
        }

        public static void SetCategoryWeight(string category, float weight) {
            if (weight < 0) {
                SetCategoryWeightToDefault(category);
            }
            else {
                if (!CategoryWeight.ContainsKey(category)) {
                    CategoryWeight.Add(category, 0);
                }
                CategoryWeight[category] = weight;
            }
        }

        public static void SetCategoryWeightToDefault(string category) {
            if (CategoryWeight.ContainsKey(category)) {
                CategoryWeight.Remove(category);
            }
        }

        public static void SetInteractableWeight(string category, string interactable, int weight) {
            if (weight < 0) {
                SetInteractableWeightToDefault(category, interactable);
            }
            else {
                if (!InteractableWeight.ContainsKey(category)) {
                    InteractableWeight.Add(category, new Dictionary<string, int>());
                }
                if (!InteractableWeight[category].ContainsKey(interactable)) {
                    InteractableWeight[category].Add(interactable, 0);
                }
                InteractableWeight[category][interactable] = weight;
            }
        }

        public static void SetInteractableWeightToDefault(string category, string interactable) {
            if (InteractableWeight.ContainsKey(category) && InteractableWeight[category].ContainsKey(interactable)) {
                InteractableWeight[category].Remove(interactable);
                if (InteractableWeight[category].Keys.Count == 0) {
                    InteractableWeight.Remove(category);
                }
            }
        }

        public static void AddCategory(DirectorCardCategorySelection.Category category) {
            if (!CategoriesToAdd.ContainsKey(category.name)) {
                CategoriesToAdd.Add(category.name, new DirectorCardCategorySelection.Category());
            }
            CategoriesToAdd[category.name] = category;

            CategoriesToRemove.Remove(category.name);
        }

        public static void RemoveCategory(string category) {
            if (!CategoriesToRemove.Contains(category)) {
                CategoriesToRemove.Add(category);
            }

            CategoriesToAdd.Remove(category);
        }

        public static void AddInteractable(string category, InteractableSpawnCard interactable, int weight) {
            if (!InteractablesToAdd.ContainsKey(category)) {
                InteractablesToAdd.Add(category, new Dictionary<string, InteractableSpawnCard>());
                InteractablesToAddWeight.Add(category, new Dictionary<string, int>());
            }
            if (!InteractablesToAdd[category].ContainsKey(interactable.name)) {
                InteractablesToAdd[category].Add(interactable.name, null);

                InteractablesToAddWeight[category].Add(interactable.name, 0);
            }
            InteractablesToAdd[category][interactable.name] = interactable;
            InteractablesToAddWeight[category][interactable.name] = weight;

            if (InteractablesToRemove.ContainsKey(category)) {
                if (InteractablesToRemove[category].Contains(interactable.name)) {
                    InteractablesToRemove[category].Remove(interactable.name);
                    if (InteractablesToRemove[category].Count == 0) {
                        InteractablesToRemove.Remove(category);
                    }
                }
            }
        }

        public static void RemoveInteractable(string category, string interactable) {
            if (!InteractablesToRemove.ContainsKey(category)) {
                InteractablesToRemove.Add(category, new List<string>());
            }
            if (!InteractablesToRemove[category].Contains(interactable)) {
                InteractablesToRemove[category].Add(interactable);
            }

            if (InteractablesToAdd.ContainsKey(category)) {
                if (InteractablesToAdd[category].ContainsKey(interactable)) {
                    InteractablesToAdd[category].Remove(interactable);
                    InteractablesToAddWeight[category].Remove(interactable);
                    if (InteractablesToAdd[category].Keys.Count == 0) {
                        InteractablesToAdd.Remove(category);
                        InteractablesToAddWeight.Remove(category);
                    }
                }
            }
        }
    }
}
