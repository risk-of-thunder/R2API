using R2API.AddressReferencedAssets;
using RoR2;
using RoR2.ExpansionManagement;
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
                try
                {
                    instance.Upgrade();
                }
                catch(Exception ex)
                {
                    DirectorPlugin.Logger.LogError($"{instance} failed to upgrade.\n{ex}");
                }
            }
        };
    }

    private void Upgrade()
    {
        List<DccsPool.Category> upgradedCategories = new List<DccsPool.Category>();
        for (int i = 0; i < poolCategories.Length; i++)
        {
            Category cat = poolCategories[i];
            var result = cat?.Upgrade();

            if(result == null)
            {
                DirectorPlugin.Logger.LogWarning($"{this}'s {i}th index of categories failed to upgrade.");
                continue;
            }

            upgradedCategories.Add(result);
        }
        targetPool.poolCategories = upgradedCategories.ToArray();
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
                dccs = GetDirectorCardCategorySelection(),
                weight = weight
            };
        }

        protected DirectorCardCategorySelection GetDirectorCardCategorySelection()
        {
            var familyDccsResult = familyDccs.Asset;

            if (familyDccsResult)
                return familyDccsResult;

            return dccs.targetCardCategorySelection;
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
            var result = new DccsPool.ConditionalPoolEntry
            {
                dccs = GetDirectorCardCategorySelection(),
                weight = weight
            };

            var expansionDefs = new List<ExpansionDef>();

            foreach (var def in requiredExpansions)
            {
                var asset = def.Asset;
                if (!asset)
                    continue;

                expansionDefs.Add(asset);
            }
            result.requiredExpansions = expansionDefs.ToArray();

            return result;
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
            var resultCategory = new DccsPool.Category
            {
                name = name,
                categoryWeight = categoryWeight,
            };

            List<DccsPool.PoolEntry> upgradedPoolEntries = new List<DccsPool.PoolEntry>();
            List<DccsPool.ConditionalPoolEntry> upgradedConditionalPoolEntries = new List<DccsPool.ConditionalPoolEntry>();

            foreach (var alwaysIncluded in alwaysIncluded)
            {
                var result = alwaysIncluded.Upgrade();
                if (result == null)
                    continue;

                if (!result.dccs)
                    continue;

                upgradedPoolEntries.Add(result);
            }
            resultCategory.alwaysIncluded = upgradedPoolEntries.ToArray();

            foreach (var conditionallyIncluded in includedIfConditionsMet)
            {
                var result = conditionallyIncluded.Upgrade();

                if (result == null)
                    continue;

                if (result is not DccsPool.ConditionalPoolEntry _conditionalPoolEntry)
                    continue;

                if (!_conditionalPoolEntry.dccs)
                    continue;

                if (_conditionalPoolEntry.requiredExpansions == null || _conditionalPoolEntry.requiredExpansions.Length == 0 || !_conditionalPoolEntry.requiredExpansions.Any())
                    continue;

                upgradedConditionalPoolEntries.Add(_conditionalPoolEntry);
            }
            resultCategory.includedIfConditionsMet = upgradedConditionalPoolEntries.ToArray();

            upgradedPoolEntries.Clear();
            foreach (var noConditionsMet in includedIfNoConditionsMet)
            {
                var result = noConditionsMet.Upgrade();

                if (result == null)
                    continue;

                if (!result.dccs)
                    continue;

                upgradedPoolEntries.Add(result);
            }
            resultCategory.includedIfNoConditionsMet = upgradedPoolEntries.ToArray();

            return resultCategory;
        }
    }
}
