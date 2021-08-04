using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Events;

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
        //[R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.PortalDialerController.Awake += AddCodes;
            On.RoR2.PortalDialerController.PerformActionServer += PrintSha256HashCode;
            RoR2.ContentManagement.ContentManager.onContentPacksAssigned += CheckForInvalidEntries;
        }

        private static void CheckForInvalidEntries(HG.ReadOnlyArray<RoR2.ContentManagement.ReadOnlyContentPack> obj) {
            foreach ((ArtifactDef, Sha256HashAsset) entry in ArtifactsCodes) {
                ArtifactDef artifactDef = entry.Item1;
                if((bool)!ArtifactCatalog.GetArtifactDef(artifactDef.artifactIndex)) {
                    R2API.Logger.LogMessage($"Removing {artifactDef.cachedName} from ArtifactCodes since it's not in the Artifact catalog.");
                    ArtifactsCodes.Remove(entry);
                }
            }
        }

        //[R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        /*internal static void UnsetHooks() {
            On.RoR2.PortalDialerController.Awake -= AddCodes;
            On.RoR2.PortalDialerController.PerformActionServer -= PrintSha256HashCode;
        }*/

        /// <summary>
        /// Prints the Artifact Code that the player inputs in the dialer. Useful for mod creators.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private static bool PrintSha256HashCode(On.RoR2.PortalDialerController.orig_PerformActionServer orig, PortalDialerController self, byte[] sequence) {
            var result = self.GetResult(sequence);
            R2API.Logger.LogInfo("Inputted Artifact Code:");
            R2API.Logger.LogInfo("_00_07: " + result._00_07);
            R2API.Logger.LogInfo("_08_15: " + result._08_15);
            R2API.Logger.LogInfo("_16_23: " + result._16_23);
            R2API.Logger.LogInfo("_24_31: " + result._24_31);
            return orig(self, sequence);
        }

        /// <summary>
        /// Adds custom ArtifactCodes to the portal dialer controller instance found in sky meadow.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void AddCodes(On.RoR2.PortalDialerController.orig_Awake orig, PortalDialerController self) {
            foreach ((ArtifactDef, Sha256HashAsset) entry in ArtifactsCodes) {
                var artifactDef = entry.Item1;
                var artifactCode = entry.Item2;

                PortalDialerController.DialedAction dialedAction = new PortalDialerController.DialedAction();
                dialedAction.hashAsset = artifactCode;
                dialedAction.action = new UnityEvent();
                dialedAction.action.AddListener(Wrapper);

                void Wrapper() => self.OpenArtifactPortalServer(artifactDef);
                R2API.Logger.LogInfo("Added code for " + artifactDef.cachedName);
                HG.ArrayUtils.ArrayAppend(ref self.actions, dialedAction);
            }
            orig(self);
        }
        #endregion Hooks

        #region Methods

        /// <summary>
        /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
        /// The artifactDef must exist within the initialized ArtifactCatalog for it to properly added to the portal dialer.
        /// </summary>
        /// <param name="artifactDef">The artifactDef tied to the artifact code,</param>
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
        /// <param name="artifactDef"></param>
        /// <param name="artifactCode"></param>
        public static void Add(ArtifactDef? artifactDef, ArtifactCodeScriptableObject? artifactCode) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ArtifactCodeAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ArtifactCodeAPI)})]");
            }
            ArtifactsCodes.Add((artifactDef, artifactCode.hashAsset));
        }

        /// <summary>
        /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
        /// The artifactDef must exist within the initialized Artifactcatalog for it to be properly added to the portal dialer.
        /// </summary>
        /// <param name="artifactDef">The artifactDef tied to the artifact code,</param>
        /// <param name="code_00_07"></param>
        /// <param name="code_08_15"></param>
        /// <param name="code_16_23"></param>
        /// <param name="code_24_31"></param>
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
        #endregion Methods

        #region CompoundEnum
        /// <summary>
        /// RoR2's Artifact compounds used for ArtifactCodeAPI's ArtifactCode scriptable object.
        /// </summary>
        public enum ArtifactCompound {
            /// <summary>
            /// Value: 11
            /// </summary>
            None,
            /// <summary>
            /// Value: 7
            /// </summary>
            Square,
            /// <summary>
            /// Value: 1
            /// </summary>
            Circle,
            /// <summary>
            /// Value: 3
            /// </summary>
            Triangle,
            /// <summary>
            /// Value: 5
            /// </summary>
            Diamond
        }
        #endregion CompoundEnum
    }
}
