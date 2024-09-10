using RoR2;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace R2API.Test.Tests.ConCommandTests;

public static class DeployableAPITests
{
    private static DeployableSlot myDeployableSlotIndex = DeployableSlot.None;
    private static bool registerOnce = true;

    [ConCommand(commandName = "add_deployable_test")]
    private static void AddDeployableTest(ConCommandArgs args)
    {
        if (registerOnce)
        {
            myDeployableSlotIndex = DeployableAPI.RegisterDeployableSlot((characterMaster, oldLimit) =>
            {
                return 3;
            });

            registerOnce = false;
        }

        Debug.LogError("[DeployableAPITests] myDeployableSlotIndex: " + myDeployableSlotIndex);

        var position = PlayerCharacterMasterController.instances[0].body.transform.position;
        var body = PlayerCharacterMasterController.instances[0].body;
        var summoner = PlayerCharacterMasterController.instances[0].body.gameObject;

        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Golem/cscGolemSnowy.asset").WaitForCompletion(), new DirectorPlacementRule
        {
            placementMode = DirectorPlacementRule.PlacementMode.Direct,
            minDistance = 0f,
            maxDistance = 0f,
            position = position
        }, RoR2Application.rng);
        directorSpawnRequest.summonerBodyObject = summoner;
        GameObject gameObject = DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
        if (gameObject)
        {
            CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
            Inventory component2 = gameObject.GetComponent<Inventory>();
            component2.SetEquipmentIndex(body.inventory.currentEquipmentIndex);
            if (body.inventory.GetItemCount(RoR2Content.Items.Ghost) > 0)
            {
                component2.GiveItem(RoR2Content.Items.Ghost, 1);
                component2.GiveItem(RoR2Content.Items.HealthDecay, 30);
                component2.GiveItem(RoR2Content.Items.BoostDamage, 150);
            }
            Deployable deployable = gameObject.AddComponent<Deployable>();
            deployable.onUndeploy = new UnityEvent();
            deployable.onUndeploy.AddListener(new UnityAction(component.TrueKill));
            body.master.AddDeployable(deployable, myDeployableSlotIndex);
        }
    }
}
