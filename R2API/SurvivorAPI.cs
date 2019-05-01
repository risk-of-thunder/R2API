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
                //by never doing orig(), the original method is never executed whenever it's called, effectively being replaced
                return SurvivorDefinitions.FirstOrDefault(x => x.survivorIndex == survivorIndex);
            };
        }


        /// <summary>
        /// Add a SurvivorDef to the list of available survivors. Use on SurvivorCatalogReady event.
        /// ATTENTION: SET A VALUE FOR SURVIVORINDEX! DEFAULT IS 0 AND YOU WILL OVERWRITE COMMANDO.
        /// Any value is okay, but note:
        ///
        /// Behaviour of this function differs, depending on the SurvivorIndex specified in the SurvivorDef:
        /// - SurvivorIndex smaller than SurvivorIndex.Count
        ///     Function will try to replace an existing Survivor with this index. Use to replace existing survivors.
        /// - SurvivorIndex larger or equal to SurvivorIndex.Count
        ///     Function handles parameter as a custom survivor, and will set SurvivorIndex as low as possible, but
        ///     will not replace other default or custom survivors.
        ///
        /// Please use this instead of SurvivorDefinitions.Insert/etc.
        /// </summary>
        /// <param name="survivor">The survivor to add.</param>
        public static void AddSurvivor(SurvivorDef survivor) {
            if (survivor.survivorIndex < SurvivorIndex.Count
                && SurvivorDefinitions.Any(x => x.survivorIndex == survivor.survivorIndex)
            ) {
                var toRemove = SurvivorDefinitions.Where(x => x.survivorIndex == survivor.survivorIndex).ToList();

                toRemove.ForEach(x => SurvivorDefinitions.Remove(x));
            }
            else {
                survivor.survivorIndex = SurvivorIndex.Count;
                while (SurvivorDefinitions.Any(x => x.survivorIndex == survivor.survivorIndex))
                    survivor.survivorIndex += 1;
            }

            SurvivorDefinitions.Add(survivor);
        }

        /// <summary>
        /// Add a SurvivorDef to the list of available survivors. Will add the survivor on SurvivorCatalogReady event.
        /// ATTENTION: SET A VALUE FOR SURVIVORINDEX! DEFAULT IS 0 AND YOU WILL OVERWRITE COMMANDO.
        /// Any value is okay, but note:
        ///
        /// Behaviour of this function differs, depending on the SurvivorIndex specified in the SurvivorDef:
        /// - SurvivorIndex smaller than SurvivorIndex.Count
        ///     Function will try to replace an existing Survivor with this index. Use to replace existing survivors.
        /// - SurvivorIndex larger or equal to SurvivorIndex.Count
        ///     Function handles parameter as a custom survivor, and will set SurvivorIndex as low as possible, but
        ///     will not replace other default or custom survivors.
        /// </summary>
        /// <param name="survivor">The survivor to add.</param>
        public static void AddSurvivorOnReady(SurvivorDef survivor) {
            SurvivorCatalogReady += (sender, args) => { AddSurvivor(survivor); };
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
            SurvivorCatalog.survivorMaxCount =
                Math.Max((int) SurvivorDefinitions.Select(x => x.survivorIndex).Max(), 10);
            SurvivorCatalog.idealSurvivorOrder = SurvivorDefinitions.Select(x => x.survivorIndex).ToArray();

            // Only contains not null survivors
            allSurvivorDefs.SetValue(null, SurvivorDefinitions
                .OrderBy(x => x.survivorIndex)
                .ToArray()
            );

            // Contains null for index with no survivor
            survivorDefs.SetValue(null,
                Enumerable.Range(0, SurvivorCatalog.survivorMaxCount)
                    .Select(i => SurvivorDefinitions.FirstOrDefault(x => x.survivorIndex == (SurvivorIndex) i)
                                 ?? new SurvivorDef {survivorIndex = (SurvivorIndex) i})
                    .OrderBy(x => x.survivorIndex)
                    .Select(x => x.bodyPrefab == null ? null : x)
                    .ToArray()
            );

            var parent = ViewablesCatalog.FindNode("/Survivors/")
                         ?? new ViewablesCatalog.Node("Survivors", true);

            if (parent.parent == null)
                ViewablesCatalog.AddNodeToRoot(parent);

            foreach (var survivor in SurvivorDefinitions) {
                var name = survivor.survivorIndex < SurvivorIndex.Count
                    ? survivor.survivorIndex.ToString()
                    // TODO: change to different way for getting custom survivor node names?
                    : survivor.displayNameToken;

                var child =
                    ViewablesCatalog.FindNode(parent.fullName + name)
                    ?? new ViewablesCatalog.Node(name, false, parent);

                child.shouldShowUnviewed = userProfile =>
                        !string.IsNullOrEmpty(survivor.unlockableName)
                        && survivor.survivorIndex < SurvivorIndex.Count
                        && userProfile.HasSurvivorUnlocked(survivor.survivorIndex)
                        && !userProfile.HasViewedViewable(child.fullName)
                    ;
            }

            Debug.Log("Re-setting all survivor nodes, duplicates may occur. This is no problem.");
            ViewablesCatalog.AddNodeToRoot(parent);
            Debug.Log("Re-setting survivor nodes complete.");
        }
    }
}
