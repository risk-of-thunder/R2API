using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using R2API.AddressReferencedAssets;

namespace R2API;

/// <summary>
/// A <see cref="AddressableDirectorCardCategorySelection"/> is a version of a <see cref="DirectorCardCategorySelection"/> that can be used for creating a custom and complex <see cref="DirectorCardCategorySelection"/> for stages, using R2API's <see cref="AddressReferencedAsset"/> system and your own existing spawn cards
/// <para>All the values from this category selection will be added to the <see cref="DirectorCardCategorySelection"/> specified in <see cref="targetCardCategorySelection"/></para>
/// <br>You should also see <see cref="AddressableDCCSPool"/></br>
/// </summary>

[CreateAssetMenu(fileName = "New AddressableDirectorCardCategorySelection", menuName = "R2API/DirectorAPI/AddressableDirectorCardCategorySelection")]
public class AddressableDirectorCardCategorySelection : ScriptableObject
{
    private static HashSet<AddressableDirectorCardCategorySelection> instances = new();

    [Tooltip("The DirectorCardCategorySelection that will be overridedn with the values stored in this AddressableDirectorCardCategorySelection")]
    public DirectorCardCategorySelection targetCardCategorySelection;

    [Tooltip("The categories for this AddressableDirectorCardCategorySelection")]
    public Category[] categories = Array.Empty<Category>();

    private void Upgrade()
    {
        var logger = DirectorPlugin.Logger;
        List<DirectorCardCategorySelection.Category> upgradedCategories = new List<DirectorCardCategorySelection.Category>();
        for (int i = 0; i < categories.Length; i++)
        {
            Category addressableCategory = categories[i];
            var possibleResult = addressableCategory.Upgrade();
            if (!possibleResult.HasValue)
            {
                logger.LogWarning($"{this}'s {i}th category failed to upgrade.");
                continue;
            }

            var result = possibleResult.Value;
            if(result.cards.Length == 0)
            {
                logger.LogWarning($"{this}'s {i}th category failed to upgrade, no cards where computed.");
                continue;
            }

            upgradedCategories.Add(possibleResult.Value);
        }
        targetCardCategorySelection.categories = upgradedCategories.ToArray();
        categories = null;
    }

    private void Awake() => instances.Add(this);

    private void OnDestroy() => instances.Remove(this);

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

    /// <summary>
    /// Represents a category of spawn cards
    /// </summary>
    [Serializable]
    public struct Category
    {
        [Tooltip("The name of this category")]
        public string name;
        [Tooltip("The DirectorCards for this category")]
        public AddressableDirectorCard[] cards;
        [Tooltip("The weight of this category relative to the other categories")]
        public float selectionWeight;

        internal DirectorCardCategorySelection.Category? Upgrade()
        {
            DirectorCardCategorySelection.Category result = new DirectorCardCategorySelection.Category()
            {
                name = name,
                selectionWeight = selectionWeight
            };
            List<DirectorCard> resultCards = new List<DirectorCard>();

            if (cards == null || cards.Length == 0)
            {
                DirectorPlugin.Logger.LogWarning($"Director card category {name} cannot upgrade as there are no AddressableDirectorCards. See below for more information.");
                return null;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                AddressableDirectorCard card = cards[i];
                var realCard = card?.Upgrade();

                if (realCard == null)
                    continue;

                if(!realCard.spawnCard)
                {
                    DirectorPlugin.Logger.LogWarning($"AddressableDCCS.Category with name {name} has an invalid spawn card at index {i}");
                    continue;
                }

                resultCards.Add(realCard);
            }

            result.cards = resultCards.ToArray();
            return result;
        }
    }
}
