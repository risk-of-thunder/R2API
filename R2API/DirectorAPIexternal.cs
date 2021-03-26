using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for modifying the monster and scene directors.
    /// </summary>
    public static partial class DirectorAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        /// <summary>
        /// Event used to edit stage settings.
        /// </summary>
        public static event Action<StageSettings, StageInfo>? StageSettingsActions;

        /// <summary>
        /// Event used to edit/add/remove the monsters spawned on a stage.
        /// </summary>
        public static event Action<List<DirectorCardHolder>, StageInfo>? MonsterActions;

        /// <summary>
        /// Event used to edit/add/remove interactables spawned on a stage.
        /// </summary>
        public static event Action<List<DirectorCardHolder>, StageInfo>? InteractableActions;

        /// <summary>
        /// Event used to edit/add/remove monster families on a stage.
        /// </summary>
        public static event Action<List<MonsterFamilyHolder>, StageInfo>? FamilyActions;

        /// <summary>
        /// If this is called then DirectorAPI will hook ClassicStageInfo.Awake and use the events to make changes
        /// </summary>

        /// <summary>
        /// The three categories for monsters. Support for custom categories will come later.
        /// </summary>
        public enum MonsterCategory {

            /// <summary>
            /// An invalid default value. Anything with this value is ignored when dealing with monsters.
            /// </summary>
            None = 0,

            /// <summary>
            /// Small enemies like Lemurians and Beetles.
            /// </summary>
            BasicMonsters = 1,

            /// <summary>
            /// Medium enemies like Golems and Beetle Guards.
            /// </summary>
            Minibosses = 2,

            /// <summary>
            /// Bosses like Vagrants and Titans.
            /// </summary>
            Champions = 3
        }

        /// <summary>
        /// The categories for interactables. Support for custom categories will come later.
        /// </summary>
        public enum InteractableCategory {

            /// <summary>
            /// An invalid default value. Anything with this value is ignored when dealing with interactables.
            /// </summary>
            None = 0,

            /// <summary>
            /// Chests, such as basic chests, large chests, shops, equipment barrels, lunar pods, and category chests. NOT legendary chests or cloaked chests.
            /// </summary>
            Chests = 1,

            /// <summary>
            /// Barrels.
            /// </summary>
            Barrels = 2,

            /// <summary>
            /// Chance shrines, blood shrines, combat shrines, order shrines, mountain shrines, shrine of the woods. NOT shrine of gold.
            /// </summary>
            Shrines = 3,

            /// <summary>
            /// All types of drones such as TC-280, equipment drones, gunner drones, healing drones, and incinerator drones. NOT gunner turrets.
            /// </summary>
            Drones = 4,

            /// <summary>
            /// Gunner turrets only.
            /// </summary>
            Misc = 5,

            /// <summary>
            /// Legendary chests, cloaked chests, shrine of gold, and radio scanners.
            /// </summary>
            Rare = 6,

            /// <summary>
            /// All three tiers of printers.
            /// </summary>
            Duplicator = 7
        }

        /// <summary>
        /// A flags enum for the vanilla stages. Custom stages are handled with a string in StageInfo.
        /// </summary>
        [Flags]
        public enum Stage {

            /// <summary>
            /// When this is set to custom, check the string in StageInfo
            /// </summary>
            Custom = 1,

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            TitanicPlains = 2,
            DistantRoost = 4,
            WetlandAspect = 8,
            AbandonedAqueduct = 16,
            RallypointDelta = 32,
            ScorchedAcres = 64,
            AbyssalDepths = 128,
            SirensCall = 256,
            GildedCoast = 512,
            MomentFractured = 1024,
            Bazaar = 2048,
            VoidCell = 4096,
            MomentWhole = 8192,
            SkyMeadow = 16384,
            ArtifactReliquary = 32768
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }

        /// <summary>
        /// Struct for holding information about the stage.
        /// </summary>
        public struct StageInfo {

            /// <summary>
            /// The current stage. If set to custom, check customStageName.
            /// </summary>
            public Stage stage;

            /// <summary>
            /// This is set to the name of the custom stage. Is left blank for vanilla stages.
            /// </summary>
            public string CustomStageName;

            /// <summary>
            /// Returns true if the current stage matches any of the stages you specify.
            /// To match a custom stage, include Stage.Custom in your stage input and specify names in customStageNames.
            /// </summary>
            /// <param name="stage">The stages to match with</param>
            /// <param name="customStageNames">Names of the custom stages to match. Leave blank to match all custom stages</param>
            /// <returns></returns>
            public bool CheckStage(Stage stage, params string[] customStageNames) {
                if (!stage.HasFlag(this.stage)) return false;
                return this.stage != Stage.Custom || customStageNames.Length == 0 || customStageNames.Contains(CustomStageName);
            }
        }

        /// <summary>
        /// A class passed to everything subscribed to stageSettingsActions that contains various settings for a stage.
        /// All mods will be working off the same settings, so operators like *=,+=,-=, and /= are preferred over directly setting values.
        /// </summary>
        public class StageSettings {

            /// <summary>
            /// How many credits the scene director has for monsters at the start of a stage.
            /// This scales with difficulty, and thus will always be zero on the first stage.
            /// </summary>
            public int SceneDirectorMonsterCredits;

            /// <summary>
            /// How many credits the scene director has for interactables at the start of a stage.
            /// </summary>
            public int SceneDirectorInteractableCredits;

            /// <summary>
            /// If the GameObject key of the dictionary is enabled, then the scene director gains the value in extra interactable credits
            /// Used for things like the door in Abyssal Depths.
            /// </summary>
            public Dictionary<GameObject, int>? BonusCreditObjects;

            /// <summary>
            /// The weights for each monster category on this stage.
            /// </summary>
            public Dictionary<MonsterCategory, float>? MonsterCategoryWeights;

            /// <summary>
            /// The weights for each interactable category on this stage.
            /// </summary>
            public Dictionary<InteractableCategory, float>? InteractableCategoryWeights;
        }

        /// <summary>
        /// A wrapper class for DirectorCards. A list of these is passed to everything subscribed to monsterActions and interactableActions.
        /// </summary>
        public class DirectorCardHolder {

            /// <summary>
            /// The director card. This contains the majority of the information for an interactable or monster, including the prefab.
            /// </summary>
            public DirectorCard? Card;

            /// <summary>
            /// The monster category the card belongs to. Will be set to None for interactables.
            /// </summary>
            public MonsterCategory MonsterCategory;

            /// <summary>
            /// The interactable category the card belongs to. Will be set to none for monsters.
            /// </summary>
            public InteractableCategory InteractableCategory;
        }

        /// <summary>
        /// A wrapper class for Monster Families. A list of these is passed to everything subscribed to familyActions.
        /// </summary>
        public class MonsterFamilyHolder {

            /// <summary>
            /// List of all basic monsters that can spawn during this family event.
            /// </summary>
            public List<DirectorCard>? FamilyBasicMonsters;

            /// <summary>
            /// List of all minibosses that can spawn during this family event.
            /// </summary>
            public List<DirectorCard>? FamilyMinibosses;

            /// <summary>
            /// List of all champions that can spawn during this family event.
            /// </summary>
            public List<DirectorCard>? FamilyChampions;

            /// <summary>
            /// The selection weight for basic monsters during the family event.
            /// </summary>
            public float FamilyBasicMonsterWeight;

            /// <summary>
            /// The selection weight for minibosses during the family event.
            /// </summary>
            public float FamilyMinibossWeight;

            /// <summary>
            /// The selection weight for champions during the family event.
            /// </summary>
            public float FamilyChampionWeight;

            /// <summary>
            /// The minimum number of stages completed for this family event to occur.
            /// </summary>
            public int MinStageCompletion;

            /// <summary>
            /// The maximum number of stages for this family event to occur.
            /// </summary>
            public int MaxStageCompletion;

            /// <summary>
            /// The weight of this monster family relative to other monster families.
            /// Does NOT increase the chances of a family event occuring, just the chance that this will be chosen when one does occur.
            /// Support for modifying the chance of family events overall will come later (and will be in StageSettings)
            /// </summary>
            public float FamilySelectionWeight;

            /// <summary>
            /// The message sent to chat when this family is selected.
            /// </summary>
            public string? SelectionChatString;
        }
    }
}
