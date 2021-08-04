using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Events;
using R2API.ScriptableObjects;

namespace R2API {

    /// <summary>
    /// API for adding custom artifact codes to the game.
    /// </summary>
    [R2APISubmodule]
    public static class ArtifactCodeAPI {

        private static readonly List<(ArtifactDef, Sha256HashAsset)> ArtifactsCodes = new List<(ArtifactDef, Sha256HashAsset)>();

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        #region Hooks
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.PortalDialerController.Awake += AddCodes;
            On.RoR2.PortalDialerController.PerformActionServer += PrintSha256HashCode;
        }
        /// <summary>
        /// Prints the Artifact Code that the player inputs in the dialer. Useful for mod creators.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private static bool PrintSha256HashCode(On.RoR2.PortalDialerController.orig_PerformActionServer orig, PortalDialerController self, byte[] sequence) {
            var result = self.GetResult(sequence);
            R2API.Logger.LogInfo("Inputted Artifact Code:\n_00_07: " + result._00_07 + "\n_08_15: " + result._08_15 + "\n_16_23: " + result._16_23 + "\n_24_31: " + result._24_31);
            return orig(self, sequence);
        }

        /// <summary>
        /// Adds custom ArtifactCodes to the portal dialer controller instance found in sky meadow.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void AddCodes(On.RoR2.PortalDialerController.orig_Awake orig, PortalDialerController self) {
            foreach ((ArtifactDef artifactDef, Sha256HashAsset artifactCode) in ArtifactsCodes) {

                if(!ArtifactCatalog.GetArtifactDef(artifactDef.artifactIndex)) {
                    R2API.Logger.LogWarning($"ArtifactDef of name {artifactDef.cachedName} is not in the ArtifactCatalog. ignoring entry.");
                    continue;
                }
                if(CheckIfCodeIsUsed(artifactCode, self.actions)) {
                    R2API.Logger.LogWarning($"A code with the values of {artifactCode.name} has already been added to the portal dialer controller. ignoring entry.");
                    continue;
                }
                void Wrapper() => self.OpenArtifactPortalServer(artifactDef);
                
                PortalDialerController.DialedAction dialedAction = new PortalDialerController.DialedAction();
                dialedAction.hashAsset = artifactCode;
                dialedAction.action = new UnityEvent();
                dialedAction.action.AddListener(Wrapper);

                HG.ArrayUtils.ArrayAppend(ref self.actions, dialedAction);
                R2API.Logger.LogInfo("Added code for " + artifactDef.cachedName);
            }
            orig(self);
        }

        private static bool CheckIfCodeIsUsed(Sha256HashAsset hashAsset, PortalDialerController.DialedAction[] dialedActions) {
            Sha256Hash hash = hashAsset.value;
            return dialedActions.Any(dialedAction => dialedAction.hashAsset.value.Equals(hash));
        }
        #endregion Hooks


        #region Methods

        /// <summary>
        /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
        /// The artifactDef must exist within the initialized ArtifactCatalog for it to properly added to the portal dialer.
        /// </summary>
        /// <param name="artifactDef">The artifactDef tied to the artifact code.</param>
        /// <param name="sha256HashAsset">The artifact code.</param>
        public static void Add(ArtifactDef? artifactDef, Sha256HashAsset? sha256HashAsset) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactCodeAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactCodeAPI)})]");
            }
            ArtifactsCodes.Add((artifactDef, sha256HashAsset));
        }

        /// <summary>
        /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
        /// The artifactDef must exist within the initialized ArtifactCatalog for it to properly added to the portal dialer.
        /// </summary>
        /// <param name="artifactDef">The artifactDef tied to the artifact code.</param>
        /// <param name="artifactCode">The artifactCode written in the ArtifactCodeScriptableObject.</param>
        public static void Add(ArtifactDef? artifactDef, ArtifactCode? artifactCode) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactCodeAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactCodeAPI)})]");
            }
            artifactCode.Start();
            ArtifactsCodes.Add((artifactDef, artifactCode.hashAsset));
        }

        /// <summary>
        /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
        /// The artifactDef must exist within the initialized Artifactcatalog for it to be properly added to the portal dialer.
        /// </summary>
        /// <param name="artifactDef">The artifactDef tied to the artifact code.</param>
        /// <param name="code_00_07">The values printed by R2API when a code is inputted.</param>
        /// <param name="code_08_15">The values printed by R2API when a code is inputted.</param>
        /// <param name="code_16_23">The values printed by R2API when a code is inputted.</param>
        /// <param name="code_24_31">The values printed by R2API when a code is inputted.</param>
        public static void Add(ArtifactDef? artifactDef, ulong code_00_07, ulong code_08_15, ulong code_16_23, ulong code_24_31) {

            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactCodeAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactCodeAPI)})]");
            }
            Sha256HashAsset hashAsset = ScriptableObject.CreateInstance<Sha256HashAsset>();
            hashAsset.value._00_07 = code_00_07;
            hashAsset.value._08_15 = code_08_15;
            hashAsset.value._16_23 = code_16_23;
            hashAsset.value._24_31 = code_24_31;

            ArtifactsCodes.Add((artifactDef, hashAsset));
        }
        /// <summary>
        /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
        /// The artifactDef must exist within the initialized ArtifactCatalog for it to be properly added to the portal dialer.
        /// </summary>
        /// <param name="artifactDef">The artifactDef tied to the artifact code.</param>
        /// <param name="CompoundValues">An IEnumerable of type "int" with a size of 9 filled with compound values.  A list of size 9 filled with compound values.</param>
        public static void Add(ArtifactDef? artifactDef, IEnumerable<int> CompoundValues) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactCodeAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactCodeAPI)})]");
            }
            ArtifactCode artifactCode = ScriptableObject.CreateInstance<ArtifactCode>();
            artifactCode.ArtifactCompounds = (List<int>)CompoundValues;
            Add(artifactDef, artifactCode);
        }
        #endregion Methods

        #region Vanilla Compound Values
        /// <summary>
        /// Contains the values of Vanilla risk of rain 2 Artifact Compounds.
        /// </summary>
        public static class CompoundValues {
            /// <summary>
            /// Value of the Empty compound.
            /// </summary>
            public const int Empty = 11;
            /// <summary>
            /// Value of the Square compound.
            /// </summary>
            public const int Square = 7;
            /// <summary>
            /// Value of the Circle compound.
            /// </summary>
            public const int Circle = 1;
            /// <summary>
            /// Value for the Triangle compound.
            /// </summary>
            public const int Triangle = 3;
            /// <summary>
            /// Value for the Diamond compound.
            /// </summary>
            public const int Diamond = 5;
        }
        #endregion Vanilla Compound Values
    }
}
