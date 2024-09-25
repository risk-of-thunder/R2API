using R2API.AddressReferencedAssets;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// A <see cref="AddressableDCCSPool"/> is a version of a <see cref="DccsPool"/> that can be created from the editor itself, it allows you to create complex DccsPools using R2API's <see cref="AddressReferencedAsset"/> system and your own existing spawn cards
/// <br>You should also see <see cref="AddressableDirectorCardCategorySelection"/></br>
/// </summary>
[CreateAssetMenu(fileName = "New AddressableDCCSPool", menuName = "R2API/DirectorAPI/AddressableDCCSPool")]
public class AddressableDCCSPool : ScriptableObject
{
    private static HashSet<AddressableDCCSPool> instances = new HashSet<AddressableDCCSPool>();

    [Tooltip("The DccsPool that will be overwritten with the values stored in this AddressableDCCSPool")]
    public DccsPool targetPool;

    [Tooltip("The categories for this pool")]
    public Category[] poolCategories = Array.Empty<Category>();

    [SystemInitializer]
    private static void SystemInitializer()
    {
        AddressReferencedAsset.OnAddressReferencedAssetsLoaded += () =>
        {
            foreach (var instance in instances)
            {
                instance.Upgrade();
            }
        };
    }

    private void Upgrade()
    {
        DccsPool.Category[] categories = poolCategories.Select(x => x.Upgrade()).ToArray();
        targetPool.poolCategories = categories;
        poolCategories = null;
    }

    private void Awake() => instances.Add(this);

    private void OnDestroy() => instances.Remove(this);

    /// <summary>
    /// Represents a version of <see cref="DccsPool.PoolEntry"/> that can use either a <see cref="AddressableDirectorCardCategorySelection"/> for representing a pool entry, or a <see cref="AddressReferencedFamilyDirectorCardCategorySelection"/> for representing a family entr.
    /// </summary>
    [Serializable]
    public class PoolEntry
    {
        [Tooltip("The DCCS for this pool entry")]
        public AddressableDirectorCardCategorySelection dccs;
        [Tooltip("An address or a Direct reference to an existing Family Director Card Category Selection")]
        public AddressReferencedFamilyDirectorCardCategorySelection familyDccs;
        [Tooltip("The weight of this pool entry relative to the others")]
        public float weight;

        internal virtual DccsPool.PoolEntry Upgrade()
        {
            return new DccsPool.PoolEntry
            {
                dccs = familyDccs.AssetExists ? familyDccs.Asset : dccs.targetCardCategorySelection,
                weight = weight
            };
        }
    }

    /// <summary>
    /// Represents a conditional version of a <see cref="PoolEntry"/>
    /// <br>Contains a <see cref="requiredExpansions"/> array that's populated using <see cref="AddressReferencedExpansionDef"/></br>
    /// </summary>
    [Serializable]
    public class ConditionalPoolEntry : PoolEntry
    {
        [Tooltip("ALL expansions in this list must be enabled for this run for this entry to be considered")]
        public AddressReferencedExpansionDef[] requiredExpansions = Array.Empty<AddressReferencedExpansionDef>();

        internal override DccsPool.PoolEntry Upgrade()
        {
            return new DccsPool.ConditionalPoolEntry
            {
                dccs = dccs.targetCardCategorySelection,
                requiredExpansions = requiredExpansions.Select(x => x.Asset).ToArray(),
                weight = weight
            };
        }
    }

    /// <summary>
    /// Represents a category of DirectorCardCategorySelections for this pool
    /// </summary>
    [Serializable]
    public class Category
    {
        [Tooltip("A name to help identify this category")]
        public string name;
        [Tooltip("The weight of all entries in this category relative to the sibling categories")]
        public float categoryWeight;
        [Tooltip("These entries are always considered")]
        public PoolEntry[] alwaysIncluded = Array.Empty<PoolEntry>();
        [Tooltip("These entries are only considered if their individual conditions are met")]
        public ConditionalPoolEntry[] includedIfConditionsMet = Array.Empty<ConditionalPoolEntry>();
        [Tooltip("These entries are considered only if no entries from 'includedIfConditionsMet' have been includedd")]
        public PoolEntry[] includedIfNoConditionsMet = Array.Empty<PoolEntry>();

        internal DccsPool.Category Upgrade()
        {
            return new DccsPool.Category
            {
                name = name,
                categoryWeight = categoryWeight,
                alwaysIncluded = alwaysIncluded.Select(x => x.Upgrade()).ToArray(),
                includedIfConditionsMet = includedIfConditionsMet.Select(x => x.Upgrade()).Cast<DccsPool.ConditionalPoolEntry>().ToArray(),
                includedIfNoConditionsMet = includedIfNoConditionsMet.Select(x => x.Upgrade()).ToArray()
            };
        }
    }
}
