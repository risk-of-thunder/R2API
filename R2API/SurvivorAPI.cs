using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    public static class SurvivorAPI {
        /// <summary>
        /// The complete list of survivors, including vanilla and modded survivors.
        /// </summary>
        public static ObservableCollection<SurvivorDef> SurvivorDefinitions { get; private set; }

        /// <summary>
        /// This event gets triggered when the Survivor Catalog is ready to receive additions/changes/removals.
        /// </summary>
        public static event EventHandler SurvivorCatalogReady;

        private static bool _wasReady;


        internal static void InitHooks() {
            var detour = new NativeDetour(
                typeof(SurvivorCatalog).GetMethodCached("Init"),
                typeof(SurvivorAPI).GetMethodCached(nameof(Init)));

            detour.Apply();

            On.RoR2.SurvivorCatalog.GetSurvivorDef += (orig, index) => GetSurvivorDef(index);

            // TODO: THIS IS A HOTFIX, WHY IS THE FIRST CHARACTERBODY NULL
            On.RoR2.UI.LogBook.LogBookController.BuildCategories += orig => {
                typeof(BodyCatalog).SetFieldValue("bodyPrefabBodyComponents",
                    typeof(BodyCatalog)
                        .GetFieldValue<CharacterBody[]>("bodyPrefabBodyComponents")
                        .Where(x => x != null).ToArray());
                return orig();
            };
        }

        public static SurvivorDef GetSurvivorDef(SurvivorIndex survivorIndex) =>
            SurvivorDefinitions.FirstOrDefault(x => x.survivorIndex == survivorIndex);

        /// <summary>
        /// Add a SurvivorDef to the list of available survivors. Use on SurvivorCatalogReady event.
        /// ATTENTION: SET A VALUE FOR SURVIVORINDEX! DEFAULT IS 0 AND YOU WILL OVERWRITE COMMANDO.
        /// Any value is okay, but note:
        ///
        /// Behaviour of this function differs, depending on the SurvivorIndex specified in the SurvivorDef:
        /// - SurvivorIndex between SurvivorIndex.None and SurvivorIndex.Count
        ///     Function will try to replace an existing Survivor with this index. Use to replace existing survivors.
        ///
        /// - Other SurvivorIndex
        ///     SurvivorIndex will be set as low as possible, but will not replace other default or custom survivors.
        ///
        /// Please use this instead of SurvivorDefinitions.Insert/etc.
        /// </summary>
        /// <param name="survivor">The survivor to add.</param>
        /// <returns>The SurvivorIndex your survivor was assigned.</returns>
        public static SurvivorIndex AddSurvivor(SurvivorDef survivor) {
            if (survivor.survivorIndex < SurvivorIndex.Count
                && survivor.survivorIndex > SurvivorIndex.None
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
            return survivor.survivorIndex;
        }

        /// <summary>
        /// Add a SurvivorDef to the list of available survivors. Will add the survivor on SurvivorCatalogReady event.
        /// ATTENTION: SET A VALUE FOR SURVIVORINDEX! DEFAULT IS 0 AND YOU WILL OVERWRITE COMMANDO.
        /// Any value is okay, but note:
        ///
        /// Behaviour of this function differs, depending on the SurvivorIndex specified in the SurvivorDef:
        /// - SurvivorIndex between SurvivorIndex.None and SurvivorIndex.Count
        ///     Function will try to replace an existing Survivor with this index. Use to replace existing survivors.
        ///
        /// - Other SurvivorIndex
        ///     SurvivorIndex will be set as low as possible, but will not replace other default or custom survivors.
        /// </summary>
        /// <param name="survivor">The survivor to add.</param>
        public static void AddSurvivorOnReady(SurvivorDef survivor) {
            if (_wasReady)
                AddSurvivor(survivor);
            else
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

            _wasReady = true;
            SurvivorCatalogReady?.Invoke(null, null);

            ReconstructSurvivors();

            SurvivorDefinitions.CollectionChanged += (sender, args) => { ReconstructSurvivors(); };
        }

        private static void ReconstructSurvivors() {
            SurvivorDefinitions.GroupBy(x => x.survivorIndex).Where(x => x.Count() > 1).ToList().ForEach(x => {
                R2API.Logger.LogError($"{CenterText("!ERROR!")}");
                R2API.Logger.LogError($"{CenterText($"One of your mods assigns a duplicate SurvivorIndex for \"{x.Key}\"")}");
                R2API.Logger.LogError($"{CenterText("Please ask the author to fix their mod.")}");
            });

            SurvivorCatalog.survivorMaxCount =
                Math.Max((int) SurvivorDefinitions.Select(x => x.survivorIndex).Max() + 1, 10);
            SurvivorCatalog.idealSurvivorOrder = SurvivorDefinitions.Select(x => x.survivorIndex).ToArray();

            // Only contains not null survivors
            typeof(SurvivorCatalog).SetFieldValue("_allSurvivorDefs",
                SurvivorDefinitions
                    .OrderBy(x => x.survivorIndex)
                    .ToArray()
            );

            // Contains null for index with no survivor
            typeof(SurvivorCatalog).SetFieldValue("survivorDefs",
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
                var name = survivor.survivorIndex.ToString();

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

            // Survivors over the builtin limit will be returned as null from SurvivorCatalog.GetSurvivorDef
            // This is a quick check if the MonoMod component is installed correctly.
            var overLimit = SurvivorDefinitions.FirstOrDefault(x => x.survivorIndex >= SurvivorIndex.Count);
            if (overLimit == null || SurvivorCatalog.GetSurvivorDef(overLimit.survivorIndex) != null)
                return;

            R2API.Logger.LogError($"{CenterText("!ERROR!")}");
            R2API.Logger.LogError($"{CenterText("MonoMod component of R2API is not installed correctly!")}");
            R2API.Logger.LogError($"{CenterText("Please copy Assembly-CSharp.R2API.mm.dll to BepInEx/monomod.")}");
        }

        // ReSharper disable FormatStringProblem
        private static string CenterText(string text = "", int width = 80) =>
            string.Format("*{0," + (width / 2 + text.Length / 2) + "}{1," + (width / 2 - text.Length / 2) + "}*", text, " ");
        // ReSharper restore FormatStringProblem
    }
}
