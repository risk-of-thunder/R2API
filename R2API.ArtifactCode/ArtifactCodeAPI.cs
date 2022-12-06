using System;
using System.Collections.Generic;
using System.Linq;
using R2API.ScriptableObjects;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Events;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API;

// ReSharper disable once InconsistentNaming
public static class ArtifactCodeAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".artifactcode";
    public const string PluginName = R2API.PluginName + ".ArtifactCode";
    public const string PluginVersion = "0.0.1";

    private static readonly List<(ArtifactDef, Sha256HashAsset)> artifactCodes = new List<(ArtifactDef, Sha256HashAsset)>();
    private static readonly List<ArtifactCompoundDef> artifactCompounds = new List<ArtifactCompoundDef>();

    [Obsolete(R2APISubmoduleDependency.propertyObsolete)]
    public static bool Loaded => true;

    private static bool _hooksEnabled = false;

    #region Hooks
    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.PortalDialerButtonController.OnStartClient += AddCompounds;
        On.RoR2.PortalDialerController.Awake += AddCodes;
        On.RoR2.PortalDialerController.PerformActionServer += Print;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.PortalDialerButtonController.OnStartClient -= AddCompounds;
        On.RoR2.PortalDialerController.Awake -= AddCodes;
        On.RoR2.PortalDialerController.PerformActionServer -= Print;

        _hooksEnabled = false;
    }

    private static void AddCompounds(On.RoR2.PortalDialerButtonController.orig_OnStartClient orig, PortalDialerButtonController self)
    {
        foreach (ArtifactCompoundDef compoundDef in artifactCompounds)
        {
            if (CheckForDuplicateCompoundValue(compoundDef, self.digitDefs))
            {
                R2API.Logger.LogWarning($"A compound with the value of {compoundDef.value} has already been added to the portal dialer button controller. Ignoring entry.");
                continue;
            }
            HG.ArrayUtils.ArrayAppend(ref self.digitDefs, compoundDef);
            R2API.Logger.LogInfo($"Added compound to portal dialer button with value of {compoundDef.value}");
            orig(self);
        }
    }

    private static void AddCodes(On.RoR2.PortalDialerController.orig_Awake orig, PortalDialerController self)
    {
        foreach ((ArtifactDef artifactDef, Sha256HashAsset artifactCode) in artifactCodes)
        {
            if (artifactDef.artifactIndex == ArtifactIndex.None)
            {
                R2API.Logger.LogWarning($"ArtifactDef of name {artifactDef.cachedName} has an index of -1! ignoring entry.");
                continue;
            }
            if (CheckIfCodeIsUsed(artifactCode, self.actions))
            {
                R2API.Logger.LogWarning($"A code with the values of {artifactCode.name} has already been added to the portal dialer controller. ignoring entry.");
                continue;
            }

            void Wrapper() => self.OpenArtifactPortalServer(artifactDef);

            PortalDialerController.DialedAction dialedAction = new PortalDialerController.DialedAction();
            dialedAction.hashAsset = artifactCode;
            dialedAction.action = new UnityEvent();
            dialedAction.action.AddListener(Wrapper);

            HG.ArrayUtils.ArrayAppend(ref self.actions, dialedAction);
            R2API.Logger.LogInfo($"Added code for {artifactDef.cachedName}");
        }
        orig(self);
    }
    private static bool Print(On.RoR2.PortalDialerController.orig_PerformActionServer orig, PortalDialerController self, byte[] sequence)
    {
        var result = self.GetResult(sequence);
        R2API.Logger.LogInfo("Inputted Artifact Code:\n_00_07: " + result._00_07 + "\n_08_15: " + result._08_15 + "\n_16_23: " + result._16_23 + "\n_24_31: " + result._24_31);
        return orig(self, sequence);
    }
    #endregion

    #region Internal Helper Methods
    private static bool CheckIfCodeIsUsed(Sha256HashAsset hashAsset, PortalDialerController.DialedAction[] dialedActions)
    {
        Sha256Hash hash = hashAsset.value;
        return dialedActions.Any(dialedAction => dialedAction.hashAsset.value.Equals(hash));
    }

    private static bool CheckForDuplicateCompoundValue(ArtifactCompoundDef compoundDef, ArtifactCompoundDef[] compoundDefs)
    {
        return compoundDefs.Any(compound => compound.value == compoundDef.value);
    }
    #endregion

    #region Public ArtifactCode Methods
    /// <summary>
    /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
    /// The artifactDef must exist within the initialized ArtifactCatalog for it to properly added to the portal dialer.
    /// </summary>
    /// <param name="artifactDef">The artifactDef tied to the artifact code.</param>
    /// <param name="sha256HashAsset">The artifact code.</param>
    public static void AddCode(ArtifactDef? artifactDef, Sha256HashAsset? sha256HashAsset)
    {
        ArtifactCodeAPI.SetHooks();
        artifactCodes.Add((artifactDef, sha256HashAsset));
    }

    /// <summary>
    /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
    /// The artifactDef must exist within the initialized ArtifactCatalog for it to properly added to the portal dialer.
    /// </summary>
    /// <param name="artifactDef">The artifactDef tied to the artifact code.</param>
    /// <param name="artifactCode">The artifactCode written in the ArtifactCodeScriptableObject.</param>
    public static void AddCode(ArtifactDef? artifactDef, ArtifactCode? artifactCode)
    {
        ArtifactCodeAPI.SetHooks();
        artifactCode.Start();
        artifactCodes.Add((artifactDef, artifactCode.hashAsset));
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
    public static void AddCode(ArtifactDef? artifactDef, ulong code_00_07, ulong code_08_15, ulong code_16_23, ulong code_24_31)
    {
        ArtifactCodeAPI.SetHooks();
        Sha256HashAsset hashAsset = ScriptableObject.CreateInstance<Sha256HashAsset>();
        hashAsset.value._00_07 = code_00_07;
        hashAsset.value._08_15 = code_08_15;
        hashAsset.value._16_23 = code_16_23;
        hashAsset.value._24_31 = code_24_31;

        artifactCodes.Add((artifactDef, hashAsset));
    }

    /// <summary>
    /// Add a custom Artifact code to the SkyMeadow Artifact portal dialer.
    /// The artifactDef must exist within the initialized ArtifactCatalog for it to be properly added to the portal dialer.
    /// </summary>
    /// <param name="artifactDef">The artifactDef tied to the artifact code.</param>
    /// <param name="CompoundValues">An IEnumerable of type "int" with a size of 9 filled with compound values.</param>
    public static void AddCode(ArtifactDef? artifactDef, IEnumerable<int> CompoundValues)
    {
        ArtifactCodeAPI.SetHooks();
        ArtifactCode artifactCode = ScriptableObject.CreateInstance<ArtifactCode>();
        artifactCode.ArtifactCompounds = (List<int>)CompoundValues;
        AddCode(artifactDef, artifactCode);
    }
    #endregion

    #region Public Compound Methods
    /// <summary>
    /// Add a custom Artifact Compound to the SkyMeadow's Artifact Buttons.
    /// </summary>
    /// <param name="artifactCompoundDef">The Artifact Compound to add. The value in the def must be unique, otherwise if a duplicate is found, it doesnt add the compound.</param>
    /// <returns>True if added to the button prefab, false otherwise.</returns>
    public static bool AddCompound(ArtifactCompoundDef artifactCompoundDef)
    {
        ArtifactCodeAPI.SetHooks();
        artifactCompounds.Add(artifactCompoundDef);
        return true;
    }

    /// <summary>
    /// Add a custom Artifact Compound to the SkyMeadow's Artifact Buttons
    /// </summary>
    /// <param name="compoundValue">The Value of the Compound.</param>
    /// <param name="compoundDecalMaterial">The Decal Material of the Compound.</param>
    /// <param name="compoundModelPrefab">The Model Prefab of the Compound.</param>
    /// <returns></returns>
    public static bool AddCompound(int compoundValue, Material compoundDecalMaterial, GameObject compoundModelPrefab)
    {
        ArtifactCodeAPI.SetHooks();
        ArtifactCompoundDef compoundDef = ScriptableObject.CreateInstance<ArtifactCompoundDef>();
        compoundDef.value = compoundValue;
        compoundDef.decalMaterial = compoundDecalMaterial;
        compoundDef.modelPrefab = compoundModelPrefab;

        return AddCompound(compoundDef);
    }
    #endregion

    /// <summary>
    /// Contains the values of Vanilla risk of rain 2 Artifact Compounds.
    /// </summary>
    public static class CompoundValues
    {

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
}
