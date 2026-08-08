using JetBrains.Annotations;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.SpawnCardCloning;
[CreateAssetMenu(fileName = "New MultiCharacterSpawnCardClone", menuName = "R2API/DirectorAPI/SpawnCardCloning/MultiCharacterSpawnCardClone")]
internal class MultiCharacterSpawnCardClone : BaseSpawnCardClone<MultiCharacterSpawnCard>
{
    [Header("Base For Cloning")]
    [Tooltip("Master prefabs to use for finding the spawn card to clone")]
    public AddressReferencedAssets.AddressReferencedPrefab[] targetMasterPrefabs;
    [Tooltip("If set to true, prefab order is respected. Otherwise order doesn't matter")]
    public bool matchOrder;
    [Tooltip("Master prefabs to override the original master prefabs of the spawn card")]
    public AddressReferencedAssets.AddressReferencedPrefab[] masterPrefabs;
    [Header("Cloning CharacterSpawnCard Settings")]
    public bool overrideNoElites;
    public bool noElites;
    public bool overrideForbiddenAsBoss;
    public bool forbiddenAsBoss;
    [CanBeNull]
    [Tooltip("The loadout for any summoned character to use.")]
    public SerializableLoadout loadout;
    public bool overrideEquipmentToGrant;
    [Tooltip("The set of equipment to grant to any summoned character, after inventory copy.")]
    [NotNull]
    public EquipmentDef[] equipmentToGrant = [];
    public bool overrideItemsToGrant;
    [NotNull]
    [Tooltip("The set of items to grant to any summoned character, after inventory copy.")]
    public ItemCountPair[] itemsToGrant = [];
    public override void UpdateValuesForClonedSpawnCard(MultiCharacterSpawnCard originalSpawnCard, MultiCharacterSpawnCard clonedMultiCharacterSpawnCard)
    {
        base.UpdateValuesForClonedSpawnCard(originalSpawnCard, clonedMultiCharacterSpawnCard);
        List<GameObject> gameObjects = [];
        foreach (AddressReferencedAssets.AddressReferencedPrefab addressReferencedPrefab in masterPrefabs) gameObjects.Add(addressReferencedPrefab.Asset);
        clonedMultiCharacterSpawnCard.masterPrefabs = gameObjects.ToArray();
        clonedMultiCharacterSpawnCard.forbiddenAsBoss = overrideForbiddenAsBoss ? forbiddenAsBoss : originalSpawnCard.forbiddenAsBoss;
        clonedMultiCharacterSpawnCard.noElites = overrideNoElites ? noElites : originalSpawnCard.noElites;
        clonedMultiCharacterSpawnCard.equipmentToGrant = overrideEquipmentToGrant ? equipmentToGrant : originalSpawnCard.equipmentToGrant;
        clonedMultiCharacterSpawnCard.itemsToGrant = overrideItemsToGrant ? itemsToGrant : originalSpawnCard.itemsToGrant;
    }
    public override void Register()
    {
        base.Register();
        if (cloningType != CloningType.Prefabs) return;
        GameObject[] gameObjects = GetTargetMasterPrefabs();
        if (gameObjects.Length == 0) return;
        if (SpawnCardCloningAPI.multiCharacterSpawnCardClonesFromPrefab.TryGetValue(gameObjects, matchOrder, out List<object> list) && !list.Contains(this))
        {
            list.Add(this);
        }
        else
        {
            SpawnCardCloningAPI.multiCharacterSpawnCardClonesFromPrefab.Add(gameObjects, [this]);
        }
    }
    public override void Unregister()
    {
        base.Unregister();
        GameObject[] gameObjects = GetTargetMasterPrefabs();
        if (gameObjects.Length == 0) return;
        if (SpawnCardCloningAPI.multiCharacterSpawnCardClonesFromPrefab.TryGetValue(gameObjects, matchOrder, out List<object> list) && list.Contains(this)) list.Remove(this);
    }
    public GameObject[] GetTargetMasterPrefabs()
    {
        List<GameObject> gameObjects = [];
        foreach (AddressReferencedAssets.AddressReferencedPrefab addressReferencedPrefab in targetMasterPrefabs)
        {
            GameObject gameObject = addressReferencedPrefab.Asset;
            gameObjects.Add(gameObject);
        }
        return gameObjects.ToArray();
    }
    public GameObject[] GetMasterPrefabs()
    {
        List<GameObject> gameObjects = [];
        foreach (AddressReferencedAssets.AddressReferencedPrefab addressReferencedPrefab in masterPrefabs)
        {
            GameObject gameObject = addressReferencedPrefab.Asset;
            gameObjects.Add(gameObject);
        }
        return gameObjects.ToArray();
    }
}
