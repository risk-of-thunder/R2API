using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using RoR2;
using UnityEngine;

namespace R2API {
    public static class SurvivorAPI {
        /// <summary>
        /// The complete list of survivors, including vanilla and modded survivors.
        /// </summary>
        public static ObservableCollection<SurvivorDef> SurvivorDefinitions { get; private set; }

        /// <summary>
        /// This event gets triggered when the Survivor Catalog is ready to receive additions/changes/removals.
        /// </summary>
        public static event EventHandler SurvivorCatalogReady;


        internal static void InitHooks() {
            var detour = new NativeDetour(
                typeof(SurvivorCatalog).GetMethodCached("Init", BindingFlags.NonPublic | BindingFlags.Static),
                typeof(SurvivorAPI).GetMethodCached(nameof(Init), BindingFlags.Public | BindingFlags.Static));

            detour.Apply();

            On.RoR2.SurvivorCatalog.GetSurvivorDef += (orig, survivorIndex) => {
                //orig is the original method and SurvivorIndex is the variable that is given to the original GetSurvivorDef
                return SurvivorDefinitions.FirstOrDefault(x => x.survivorIndex == survivorIndex && x.bodyPrefab != null);
                //by never doing orig(), the original method is never executed whenever it's called, effectively being replaced
            };
        }

        public static void Init() {
            SurvivorDefinitions = new ObservableCollection<SurvivorDef>(new List<SurvivorDef> {
                new SurvivorDef {
                    bodyPrefab = BodyCatalog.FindBodyPrefab("CommandoBody"),
                    displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/CommandoDisplay"),
                    descriptionToken = "COMMANDO_DESCRIPTION",
                    primaryColor = new Color(0.929411769f, 0.5882353f, 0.07058824f),
                    survivorIndex = SurvivorIndex.Commando
                },
                new SurvivorDef {
                    bodyPrefab = BodyCatalog.FindBodyPrefab("ToolbotBody"),
                    displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/ToolbotDisplay"),
                    descriptionToken = "TOOLBOT_DESCRIPTION",
                    primaryColor = new Color(0.827451f, 0.768627465f, 0.3137255f),
                    unlockableName = "Characters.Toolbot",
                    survivorIndex = SurvivorIndex.Toolbot
                },
                new SurvivorDef {
                    bodyPrefab = BodyCatalog.FindBodyPrefab("HuntressBody"),
                    displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/HuntressDisplay"),
                    primaryColor = new Color(0.8352941f, 0.235294119f, 0.235294119f),
                    descriptionToken = "HUNTRESS_DESCRIPTION",
                    unlockableName = "Characters.Huntress",
                    survivorIndex = SurvivorIndex.Huntress
                },
                new SurvivorDef {
                    bodyPrefab = BodyCatalog.FindBodyPrefab("EngiBody"),
                    displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/EngiDisplay"),
                    descriptionToken = "ENGI_DESCRIPTION",
                    primaryColor = new Color(0.372549027f, 0.8862745f, 0.5254902f),
                    unlockableName = "Characters.Engineer",
                    survivorIndex = SurvivorIndex.Engineer
                },
                new SurvivorDef {
                    bodyPrefab = BodyCatalog.FindBodyPrefab("MageBody"),
                    displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/MageDisplay"),
                    descriptionToken = "MAGE_DESCRIPTION",
                    primaryColor = new Color(0.968627453f, 0.75686276f, 0.992156863f),
                    unlockableName = "Characters.Mage",
                    survivorIndex = SurvivorIndex.Mage
                },
                new SurvivorDef {
                    bodyPrefab = BodyCatalog.FindBodyPrefab("MercBody"),
                    displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/MercDisplay"),
                    descriptionToken = "MERC_DESCRIPTION",
                    primaryColor = new Color(0.423529416f, 0.819607854f, 0.917647064f),
                    unlockableName = "Characters.Mercenary",
                    survivorIndex = SurvivorIndex.Merc
                }
            });

            SurvivorCatalogReady?.Invoke(null, null);

            ReconstructSurvivors();

            SurvivorDefinitions.CollectionChanged += (sender, args) => { ReconstructSurvivors(); };
        }

        private static readonly FieldInfo survivorDefs =
            typeof(SurvivorCatalog).GetFieldCached("survivorDefs", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo allSurvivorDefs =
            typeof(SurvivorCatalog).GetFieldCached("_allSurvivorDefs", BindingFlags.Static | BindingFlags.NonPublic);

        public static void ReconstructSurvivors() {
            SurvivorCatalog.survivorMaxCount = Math.Max((int)SurvivorDefinitions.Select(x => x.survivorIndex).Max(), 10);
            SurvivorCatalog.idealSurvivorOrder = SurvivorDefinitions.Select(x => x.survivorIndex).ToArray();

            // Only contains not null survivors
            allSurvivorDefs.SetValue(null, SurvivorDefinitions
                .OrderBy(x => x.survivorIndex)
                .Where(x => x.bodyPrefab != null)
                .ToArray()
            );

            // Contains null for index with no survivor
            foreach (var i in Enumerable.Range(0, SurvivorCatalog.survivorMaxCount)) {
                if (SurvivorDefinitions.All(x => x.survivorIndex != (SurvivorIndex) i))
                    SurvivorDefinitions.Add(new SurvivorDef {survivorIndex = (SurvivorIndex) i});
            }
            survivorDefs.SetValue(null, SurvivorDefinitions
                .OrderBy(x => x.survivorIndex)
                .Select(x => x.bodyPrefab == null ? null : x)
                .ToArray()
            );

            //this essentially deletes an existing node if it exists
            // TODO this does not work, ViewablesCatalog.fullNameToNodeMap does not seem to get updated
            var existingNode = ViewablesCatalog.FindNode("/Survivors/");
            existingNode?.SetParent(new ViewablesCatalog.Node("dummy", true));

            var node = new ViewablesCatalog.Node("Survivors", true);

            foreach (var survivor in SurvivorDefinitions.Where(x => x.bodyPrefab != null)) {
                // TODO: survivor.survivorIndex.ToString() is correct, but doesnt work with custom survivors.
                var survivorEntryNode = new ViewablesCatalog.Node(survivor.survivorIndex.ToString(), false, node);

                survivorEntryNode.shouldShowUnviewed = userProfile =>
                    !userProfile.HasViewedViewable(survivorEntryNode.fullName) &&
                    userProfile.HasSurvivorUnlocked(survivor.survivorIndex) &&
                    !string.IsNullOrEmpty(survivor.unlockableName);
            }

            ViewablesCatalog.AddNodeToRoot(node);
        }
    }
}
