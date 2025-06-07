using R2API.AddressReferencedAssets;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// Represents a <see cref="DirectorCard"/> that can be created from using <see cref="AddressReferencedSpawnCard"/>
/// </summary>
[Serializable]
public class AddressableDirectorCard
{
    [Tooltip("The spawn card for this DirectorCard")]
    public AddressReferencedSpawnCard spawnCard;
    [Tooltip("The weight of this director card relative to other cards")]
    public int selectionWeight;
    [Tooltip("The distance used for spawning this card, used for monsters")]
    public DirectorCore.MonsterSpawnDistance spawnDistance;
    public bool preventOverhead;
    [Tooltip("The minimum amount of stages that need to be completed before this Card can be spawned")]
    public int minimumStageCompletions;
    [Tooltip("This unlockableDef must be unlocked for this Card to spawn")]
    public AddressReferencedUnlockableDef requiredUnlockableDef;
    [Tooltip("This unlockableDef cannot be unlocked for this Card to spawn")]
    public AddressReferencedUnlockableDef forbiddenUnlockableDef;
    internal DirectorCard Upgrade()
    {
        DirectorCard returnVal = new DirectorCard();
        returnVal.spawnCard = spawnCard;
        returnVal.selectionWeight = selectionWeight;
        returnVal.spawnDistance = spawnDistance;
        returnVal.preventOverhead = preventOverhead;
        returnVal.minimumStageCompletions = minimumStageCompletions;
        returnVal.requiredUnlockableDef = requiredUnlockableDef;
        returnVal.forbiddenUnlockableDef = forbiddenUnlockableDef;
        return returnVal;
    }
}
