using RoR2;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace R2API.Test.Tests.ConCommandTests;

#pragma warning disable IDE0051

public static class DirectorTests
{
    [ConCommand(commandName = "add_remove_monster_test")]
    private static void AddRemoveMonsterTest(ConCommandArgs args)
    {
        string monsterName;

        if (args.Count == 1)
        {
            monsterName = args[0];
        }
        else
        {
            monsterName = "cscBeetle";
        }

        DirectorAPI.Helpers.RemoveExistingMonster(monsterName);
    }

    [ConCommand(commandName = "rem_remove_monster_test")]
    private static void RemoveRemoveMonsterTest(ConCommandArgs args)
    {
        // TODO
    }

    private const string GupCharacterSpawnCard = "RoR2/DLC1/Gup/cscGupBody.asset";
    private static CharacterSpawnCard _myGupSpawnCard;
    private static DirectorCard _myGupDC;

    [ConCommand(commandName = "add_only_gup_test")]
    private static void AddOnlyGupTest(ConCommandArgs args)
    {
        _myGupSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>(GupCharacterSpawnCard).WaitForCompletion();
        _myGupSpawnCard = UnityObject.Instantiate(_myGupSpawnCard);
        _myGupSpawnCard.directorCreditCost = 1;

        _myGupDC = new DirectorCard
        {
            spawnCard = _myGupSpawnCard,
            selectionWeight = 1,
            preventOverhead = false,
            minimumStageCompletions = 0,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Standard
        };

        DirectorAPI.MonsterActions += OnlyGup;
    }

    [ConCommand(commandName = "rem_only_gup_test")]
    private static void RemoveOnlyGupTest(ConCommandArgs args)
    {
        DirectorAPI.MonsterActions -= OnlyGup;
    }

    private static void OnlyGup(
        DccsPool dccsPool,
        List<DirectorAPI.DirectorCardHolder> mixEnemyArtifactsCards,
        DirectorAPI.StageInfo stageInfo)
    {

        var cardHolder = new DirectorAPI.DirectorCardHolder()
        {
            Card = _myGupDC,
            MonsterCategory = DirectorAPI.MonsterCategory.BasicMonsters
        };

        if (dccsPool)
        {
            DirectorAPI.Helpers.ForEachPoolEntryInDccsPool(dccsPool, (poolEntry) =>
            {
                poolEntry.dccs.Clear();
                _ = poolEntry.dccs.AddCard(cardHolder);
            });
        }

        mixEnemyArtifactsCards.Add(cardHolder);
    }

    private const string iscCategoryChest2Damage = "RoR2/DLC1/CategoryChest2/iscCategoryChest2Damage.asset";
    private static InteractableSpawnCard _iscCategoryChest2DamageSpawnCard;
    private static DirectorCard _my_iscCategoryChest2DamageDirectorCard;

    [ConCommand(commandName = "add_interactable_test")]
    private static void AddInteractableTest(ConCommandArgs args)
    {
        _iscCategoryChest2DamageSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(iscCategoryChest2Damage).WaitForCompletion();
        _iscCategoryChest2DamageSpawnCard = UnityObject.Instantiate(_iscCategoryChest2DamageSpawnCard);
        _iscCategoryChest2DamageSpawnCard.directorCreditCost = 10;

        _my_iscCategoryChest2DamageDirectorCard = new DirectorCard
        {
            spawnCard = _iscCategoryChest2DamageSpawnCard,
            selectionWeight = 1,
            preventOverhead = false,
            minimumStageCompletions = 0,
            spawnDistance = DirectorCore.MonsterSpawnDistance.Standard
        };

        DirectorAPI.InteractableActions += CustomInteractables;
        DirectorAPI.StageSettingsActions += CustomStageSettings;
    }

    private static void CustomStageSettings(DirectorAPI.StageSettings stageSettings, DirectorAPI.StageInfo stageInfo)
    {
        stageSettings.SceneDirectorInteractableCredits += 10;
    }

    [ConCommand(commandName = "rem_interactable_test")]
    private static void RemoveInteractableTest(ConCommandArgs args)
    {
        DirectorAPI.InteractableActions -= CustomInteractables;
        DirectorAPI.StageSettingsActions -= CustomStageSettings;
    }

    private static void CustomInteractables(
        DccsPool dccsPool, DirectorAPI.StageInfo stageInfo)
    {

        var cardHolder = new DirectorAPI.DirectorCardHolder()
        {
            Card = _my_iscCategoryChest2DamageDirectorCard,
            InteractableCategory = DirectorAPI.InteractableCategory.Chests
        };

        if (dccsPool)
        {
            DirectorAPI.Helpers.ForEachPoolEntryInDccsPool(dccsPool, (poolEntry) =>
            {
                if (poolEntry.dccs)
                {
                    poolEntry.dccs.Clear();
                    var cardIndex = poolEntry.dccs.AddCard(cardHolder);
                }
            });
        }
    }
}

#pragma warning restore IDE0051
