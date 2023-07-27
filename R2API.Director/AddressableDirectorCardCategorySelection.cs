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
public class AddressableDirectorCardCategorySelection : ScriptableObject
{
    private static HashSet<AddressableDirectorCardCategorySelection> instances;

    [Tooltip("The DirectorCardCategorySelection that will be overridedn with the values stored in this AddressableDirectorCardCategorySelection")]
    public DirectorCardCategorySelection targetCardCategorySelection;

    [Tooltip("The categories for this AddressableDirectorCardCategorySelection")]
    public Category[] categories = Array.Empty<Category>();

    private void Upgrade()
    {
        targetCardCategorySelection.categories = categories.Select(x => x.Upgrade()).ToArray();
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
                instance.Upgrade();
            }
        };
    }

    /// <summary>
    /// Represents a category of spawn cards
    /// </summary>
    public struct Category
    {
        [Tooltip("The name of this category")]
        public string name;
        [Tooltip("The DirectorCards for this category")]
        public AddressableDirectorCard[] cards;
        [Tooltip("The weight of this category relative to the other categories")]
        public float selectionWeight;

        internal DirectorCardCategorySelection.Category Upgrade()
        {
            return new DirectorCardCategorySelection.Category
            {
                name = name,
                cards = cards.Select(x => x.Upgrade()).ToArray(),
                selectionWeight = selectionWeight
            };
        }
    }
}
