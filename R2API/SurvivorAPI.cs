using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace R2API {

    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class SurvivorAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static readonly int VanillaSurvivorCount = SurvivorCatalog.idealSurvivorOrder.Length;
        private static readonly int VanillaSurvivorMaxCount = SurvivorCatalog.survivorMaxCount;

        private static bool _survivorsAlreadyAdded;

        public static ObservableCollection<SurvivorDef?>? SurvivorDefinitions = new ObservableCollection<SurvivorDef?>();

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
        public static bool AddSurvivor(SurvivorDef? survivor) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(SurvivorAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(SurvivorAPI)})]");
            }

            if (_survivorsAlreadyAdded) {
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
            On.RoR2.SurvivorCatalog.Init += FixCharacterSelectIcons;
            IL.RoR2.CharacterSelectBarController.Build += DescriptionTokenSafetyCheck;
        }

        private static void FixCharacterSelectIcons(On.RoR2.SurvivorCatalog.orig_Init orig) {
            orig();

            var survivorDefs = typeof(SurvivorCatalog).GetFieldValue<SurvivorDef[]>("survivorDefs");

            // The size of the vanilla array doesnt match the number of registered vanilla survivors
            var allSurvivorCount = survivorDefs.Count(s => s != null);
            var customSurvivorCount = allSurvivorCount - VanillaSurvivorCount;

            // Increase the size of the order array to accomodate the added survivors
            Array.Resize(ref SurvivorCatalog.idealSurvivorOrder, allSurvivorCount);

            // Add the new survivors to the order array so the game knows where to put them in character select
            for (int i = VanillaSurvivorCount, j = 0; i < allSurvivorCount; i++, j++) {
                var customSurvivorDef = survivorDefs[(int)SurvivorIndex.Count + j];
                SurvivorCatalog.idealSurvivorOrder[i] = customSurvivorDef.survivorIndex;
                R2API.Logger.LogInfo($"Survivor: {customSurvivorDef.displayNameToken} added");
            }

            // Increase the max survivor count variable to keep the vanilla WIP icon for future vanilla survivors.
            SurvivorCatalog.survivorMaxCount = VanillaSurvivorMaxCount + customSurvivorCount;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            SurvivorCatalog.getAdditionalSurvivorDefs -= AddSurvivorAction;
            On.RoR2.SurvivorCatalog.Init -= FixCharacterSelectIcons;
            IL.RoR2.CharacterSelectBarController.Build -= DescriptionTokenSafetyCheck;
        }

        private static void AddSurvivorAction(List<SurvivorDef> survivorDefinitions) {
            // Set this to true so no more survivors can be added to the list while this is happening, or afterwards
            _survivorsAlreadyAdded = true;

            foreach (var survivor in SurvivorDefinitions) {
                //Check if the current survivor has been registered in bodycatalog. Log if it has not, but still add the survivor
                if (BodyCatalog.FindBodyIndex(survivor.bodyPrefab) == -1 || BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex(survivor.bodyPrefab)) != survivor.bodyPrefab) {
                    R2API.Logger.LogWarning($"Survivor: {survivor.displayNameToken} is not properly registered in {nameof(BodyCatalog)}");
                }

                survivorDefinitions.Add(survivor);
            }
        }

        // Add a safety check for SurvivorDef that are lacking
        // a Environement.NewLine in their descriptionToken
        // The game code use IndexOf, assuming there is always one in there.
        private static void DescriptionTokenSafetyCheck(ILContext il) {
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
                    i => i.MatchCallOrCallvirt(typeof(string).GetMethodCached("IndexOf", new[] { typeof(string) })),
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
        }
    }
}
