using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class SurvivorAPI {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            set => _loaded = value;
        }

        private static bool _loaded;

        private static bool survivorsAlreadyAdded = false;

        public static ObservableCollection<SurvivorDef> SurvivorDefinitions = new ObservableCollection<SurvivorDef>();
        /// <summary>
        /// Add a SurvivorDef to the list of available survivors.
        /// This must be called before the SurvivorCatalog inits, so before plugin.Start()
        /// Value for SurvivorIndex is ignored by game code so can be left blank
        /// If this is called after the SurvivorCatalog inits then this will return false and ignore the survivor
        /// Can optionally specify a name for the survivor that will be logged with the mod info.
        /// The survivor prefab must be non-null
        /// </summary>
        /// <param name="survivor">The survivor to add.</param>
        /// <returns>true if survivor will be added</returns>
        public static bool AddSurvivor(SurvivorDef survivor) {
            if (!Loaded) {
                R2API.Logger.LogError("SurvivorAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]");
            }

            if (survivorsAlreadyAdded) {
                R2API.Logger.LogError($"Tried to add survivor: {survivor.displayNameToken} after survivor list was created");
                return false;
            }

            if (!survivor.bodyPrefab) {
                R2API.Logger.LogError($"No prefab defined for survivor: {survivor.displayNameToken}");
                return false;
            }

            SurvivorDefinitions.Add(survivor);

            return true;
        }


        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            SurvivorCatalog.getAdditionalSurvivorDefs += AddSurvivorAction;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            SurvivorCatalog.getAdditionalSurvivorDefs -= AddSurvivorAction;
        }

        private static void AddSurvivorAction(List<SurvivorDef> survivorDefinitions) {
            // Set this to true so no more survivors can be added to the list while this is happening, or afterwards
            survivorsAlreadyAdded = true;

            // Get the count of the new survivors added, and the number of vanilla survivors
            var newSurvivorCount = SurvivorDefinitions.Count;
            var existingSurvivorCount = SurvivorCatalog.idealSurvivorOrder.Length;

            // Increase the size of the order array to accomodate the added survivors
            Array.Resize(ref SurvivorCatalog.idealSurvivorOrder, existingSurvivorCount + newSurvivorCount);

            // Increase the max survivor count to ensure there is enough space on the char select bar
            SurvivorCatalog.survivorMaxCount += newSurvivorCount;


            foreach (var survivor in SurvivorDefinitions) {

                //Check if the current survivor has been registered in bodycatalog. Log if it has not, but still add the survivor
                if (BodyCatalog.FindBodyIndex(survivor.bodyPrefab) == -1 || BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex(survivor.bodyPrefab)) != survivor.bodyPrefab) {

                    R2API.Logger.LogWarning($"Survivor: {survivor.displayNameToken} is not properly registered in {nameof(BodyCatalog)}");
                }

                R2API.Logger.LogInfo($"Survivor: {survivor.displayNameToken} added");

                survivorDefinitions.Add(survivor);

                // Add that new survivor to the order array so the game knows where to put it in character select
                SurvivorCatalog.idealSurvivorOrder[existingSurvivorCount++] = (SurvivorIndex)existingSurvivorCount;
            }
        }

        // Add a safety check for SurvivorDef that are lacking
        // a Environement.NewLine in their descriptionToken
        // The game code use IndexOf, assuming there is always one in there.
        internal static void SafetyCheck() {
            IL.RoR2.CharacterSelectBarController.Build += il => {
                var c = new ILCursor(il);
                int locSurvivorDef2 = 0;
                int locDescriptionToken = 0;

                // ReSharper disable once InconsistentNaming
                void ILFailMessage() {
                    R2API.Logger.LogError(
                        $"{nameof(SurvivorAPI)}: Safety Check: Could not find IL Instructions. " +
                        "Aborting. Instabilities related to custom Survivors may, or may not happens.");
                }

                // Retrieve the loc index for local variable survivorDef2
                var findLocSurvivorDef = new Func<Instruction, bool>[]
                {
                    i => i.MatchLdloc(out _),
                    i => i.MatchCallOrCallvirt(typeof(SurvivorCatalog).GetMethodCached("GetSurvivorDef")),
                    i => i.MatchStloc(out locSurvivorDef2)
                };
                if (!c.TryGotoNext(MoveType.After,
                    findLocSurvivorDef
                )) {
                    ILFailMessage();
                    return;
                }
                if (!c.TryGotoNext(MoveType.After,
                    findLocSurvivorDef
                )) {
                    ILFailMessage();
                    return;
                }

                // Safety check for the descriptionToken NewLine
                if (c.TryGotoNext(MoveType.After,
                        i => i.MatchLdloc(out locDescriptionToken),
                        i => i.MatchCallOrCallvirt(typeof(Environment).GetMethodCached("get_NewLine")),
                        i => i.MatchCallOrCallvirt(typeof(string).GetMethodCached("IndexOf", new []{ typeof(string) })),
                        i => i.MatchStloc(out _)
                    )) {
                    c.Index--;

                    // orig : length = text.IndexOf(Environment.NewLine);
                    // modified : length = text.IndexOf(Environment.NewLine); if length == -1 then length = text.Length;
                    c.Emit(OpCodes.Ldloc, locSurvivorDef2);
                    c.Emit(OpCodes.Ldloc, locDescriptionToken);
                    c.EmitDelegate<Func<int, SurvivorDef, string, int>>((indexOf, survivorDef, descriptionToken) => {
                        if (indexOf == -1) {
                            R2API.Logger.LogWarning(
                                $"A Custom Survivor called {survivorDef.displayNameToken} doesn't have a Environement.NewLine " +
                                "like its supposed to have in its descriptionToken for separating the preview sentence to the tips underneath it");
                            return descriptionToken.Length;
                        }

                        return indexOf;
                    });
                }
                else {
                    ILFailMessage();
                }
            };
        }
    }
}
