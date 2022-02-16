using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace R2API {

    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    [Obsolete($"The {nameof(SurvivorAPI)} is obsolete, please add your SurvivorDefs, BodyPrefabs and MasterPrefabs via R2API.ContentManagment.R2APIContentManager.AddContent()")]
    public static class SurvivorAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;
        [Obsolete($"This observable collection is obsolete, if you want to look at the survivorDefs added by R2API, look at R2API.ContentManagement.R2APIContentManager.ManagedContentPacks and do a SelectMany on the SurvivorDefs.")]
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
        [Obsolete($"AddSurvivor is obsolete, please add your SurvivorDefs via R2API.ContentManagement.ContentAdditionHelpers.AddSurvivorDef()\nYou can also add Your CharacterBody and MasterBody with AddBody() & AddMaster() respectively.")]
        public static bool AddSurvivor(SurvivorDef? survivor) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(SurvivorAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(SurvivorAPI)})]");
            }

            if (!CatalogBlockers.GetAvailability<SurvivorDef>()) {
                R2API.Logger.LogError($"Tried to add survivor: {survivor.displayNameToken} After the SurvivorCatalog has initialized!");
                return false;
            }

            if (!survivor.bodyPrefab) {
                R2API.Logger.LogError($"No prefab defined for survivor: {survivor.displayNameToken}");
                return false;
            }

            SurvivorDefinitions.Add(survivor);

            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), survivor.bodyPrefab);
            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), survivor);

            return true;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.CharacterSelectBarController.Build += DescriptionTokenSafetyCheck;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.CharacterSelectBarController.Build -= DescriptionTokenSafetyCheck;
        }

        // Add a safety check for SurvivorDef that are lacking
        // a Environement.NewLine in their descriptionToken
        // The game code use IndexOf, assuming there is always one in there.
        private static void DescriptionTokenSafetyCheck(ILContext il) {
            var c = new ILCursor(il);
            int locSurvivorDef2 = 0;
            int locDescriptionToken = 0;

            // ReSharper disable once InconsistentNaming
            static void ILFailMessage(int index) {
                R2API.Logger.LogError(
                    $"{nameof(SurvivorAPI)}: Safety Check: Could not find IL Instructions ({index}). " +
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
                ILFailMessage(1);
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
                ILFailMessage(3);
            }
        }
    }
}
