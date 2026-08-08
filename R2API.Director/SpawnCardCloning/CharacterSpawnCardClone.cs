using JetBrains.Annotations;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.SpawnCardCloning;
[CreateAssetMenu(fileName = "New CharacterSpawnCardClone", menuName = "R2API/DirectorAPI/SpawnCardCloning/CharacterSpawnCardClone")]
public class CharacterSpawnCardClone : BaseSpawnCardClone<CharacterSpawnCard>
{
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
    public override void UpdateValuesForClonedSpawnCard(CharacterSpawnCard originalSpawnCard, CharacterSpawnCard clonedSpawnCard)
    {
        base.UpdateValuesForClonedSpawnCard(originalSpawnCard, clonedSpawnCard);
        clonedSpawnCard.forbiddenAsBoss = overrideForbiddenAsBoss ? forbiddenAsBoss : originalSpawnCard.forbiddenAsBoss;
        clonedSpawnCard.noElites = overrideNoElites ? noElites : originalSpawnCard.noElites;
        clonedSpawnCard.equipmentToGrant = overrideEquipmentToGrant ? equipmentToGrant : originalSpawnCard.equipmentToGrant;
        clonedSpawnCard.itemsToGrant = overrideItemsToGrant ? itemsToGrant : originalSpawnCard.itemsToGrant;

    }
}
