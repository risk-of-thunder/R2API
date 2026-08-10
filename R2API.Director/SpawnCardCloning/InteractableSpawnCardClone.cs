using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.SpawnCardCloning;
[CreateAssetMenu(fileName = "New InteractableSpawnCardClone", menuName = "R2API/DirectorAPI/SpawnCardCloning/InteractableSpawnCardClone")]
public class InteractableSpawnCardClone : BaseSpawnCardClone<InteractableSpawnCard>
{
    [Header("Cloning InteractableSpawnCard Settings")]
    public bool overrideOrientToFloor;
    [Tooltip("Whether or not to orient the object to the normal of the ground it spawns on.")]
    public bool orientToFloor;
    public bool overrideSlightlyRandomizeOrientation;
    public bool slightlyRandomizeOrientation;
    public bool overrideSkipSpawnWhenSacrificeArtifactEnabled;
    public bool skipSpawnWhenSacrificeArtifactEnabled;
    public float weightScalarWhenSacrificeArtifactEnabledMultiplier = 1f;
    public bool overrideWeightScalarWhenSacrificeArtifactEnabled;
    [Tooltip("When Sacrifice is enabled, this is multiplied by the card's weight")]
    public float weightScalarWhenSacrificeArtifactEnabled = 1f;
    public bool overrideSkipSpawnWhenDevotionArtifactEnabled;
    public float maxSpawnsPerStageMultiplier = 1f;
    public bool skipSpawnWhenDevotionArtifactEnabled;
    public bool overrideMaxSpawnsPerStage;
    [Tooltip("Won't spawn more than this many per stage.  If it's negative, there's no cap")]
    public int maxSpawnsPerStage = -1;
    public float prismaticTrialSpawnChanceMultiplier = 1f;
    public bool overridePrismaticTrialSpawnChance;
    [Range(0f, 1f)]
    [Tooltip("When playing Primatic Trials, this interactable will have a required check to see if it is supposed to spawn. 0 is will not spawn, 1 will 100% spawn and will be default.")]
    public float prismaticTrialSpawnChance = 1f;
    public override void UpdateValuesForClonedSpawnCard(InteractableSpawnCard originalSpawnCard, InteractableSpawnCard clonedInteractableSpawnCard)
    {
        base.UpdateValuesForClonedSpawnCard(originalSpawnCard, clonedInteractableSpawnCard);
        clonedInteractableSpawnCard.orientToFloor = overrideOrientToFloor ? orientToFloor : originalSpawnCard.orientToFloor;
        clonedInteractableSpawnCard.slightlyRandomizeOrientation = overrideSlightlyRandomizeOrientation ? slightlyRandomizeOrientation : originalSpawnCard.slightlyRandomizeOrientation;
        clonedInteractableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = overrideSkipSpawnWhenSacrificeArtifactEnabled ? skipSpawnWhenSacrificeArtifactEnabled : originalSpawnCard.skipSpawnWhenSacrificeArtifactEnabled;
        clonedInteractableSpawnCard.weightScalarWhenSacrificeArtifactEnabled = overrideWeightScalarWhenSacrificeArtifactEnabled ? weightScalarWhenSacrificeArtifactEnabled : originalSpawnCard.weightScalarWhenSacrificeArtifactEnabled * weightScalarWhenSacrificeArtifactEnabledMultiplier;
        clonedInteractableSpawnCard.skipSpawnWhenDevotionArtifactEnabled = overrideSkipSpawnWhenDevotionArtifactEnabled ? skipSpawnWhenDevotionArtifactEnabled : originalSpawnCard.skipSpawnWhenDevotionArtifactEnabled;
        clonedInteractableSpawnCard.maxSpawnsPerStage = overrideMaxSpawnsPerStage ? maxSpawnsPerStage : (int)(originalSpawnCard.maxSpawnsPerStage * maxSpawnsPerStageMultiplier);
        clonedInteractableSpawnCard.prismaticTrialSpawnChance = overridePrismaticTrialSpawnChance ? prismaticTrialSpawnChanceMultiplier : originalSpawnCard.prismaticTrialSpawnChance * prismaticTrialSpawnChanceMultiplier;
    }
}
