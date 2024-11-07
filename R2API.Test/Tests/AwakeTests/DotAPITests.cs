using R2API.TestingLibrary;
using RoR2;
using UnityEngine;

namespace R2API.Test.Tests.AwakeTests;

public class DotAPITests
{
    private static DotController.DotIndex MyTestDotIndex;

    private static DotAPI.CustomDotBehaviour MyTestCustomDotBehaviour = MyTestCustomDotBehaviorInternal;
    private static void MyTestCustomDotBehaviorInternal(DotController self, DotController.DotStack dotStack)
    {
        Debug.LogError($"[DotAPITests] MyTestCustomDotBehaviorInternal called");

        if (dotStack.dotIndex == MyTestDotIndex)
        {
            Debug.LogError($"[DotAPITests] MyTestCustomDotBehaviorInternal: {self.victimBody.name}");
        }
    }

    private static DotAPI.CustomDotVisual MyTestCustomDotVisual = MyTestCustomDotVisualInternal;
    private static void MyTestCustomDotVisualInternal(DotController self)
    {
        Debug.LogError($"[DotAPITests] MyTestCustomDotVisualInternal called");

        var modelLocator = self.victimObject.GetComponent<ModelLocator>();
        if (modelLocator && modelLocator.modelTransform)
        {
            Debug.LogError($"[DotAPITests] model locator: {self.victimBody.name}");
        }
    }

    [Fact]
    public void Test()
    {
        MyTestDotIndex = DotAPI.RegisterDotDef(new DotController.DotDef
        {
            interval = 0.2f,
            damageCoefficient = 0.1f,
            damageColorIndex = DamageColorIndex.Void,
            associatedBuff = null
        }, MyTestCustomDotBehaviour, MyTestCustomDotVisual);

        Assert.True(MyTestDotIndex != DotController.DotIndex.None);
    }

    [ConCommand(commandName = "inflict_dot_test")]
    private static void InflictDotToEveryMonster(ConCommandArgs args)
    {
        Debug.LogError("inflict_dot_test called");

        var teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Monster);
        for (int i = 0; i < teamMembers.Count; i++)
        {
            var cb = teamMembers[i].GetComponent<CharacterBody>();
            if (cb)
            {
                InflictDotInfo inflictDotInfo = default;
                inflictDotInfo.dotIndex = MyTestDotIndex;
                inflictDotInfo.attackerObject = PlayerCharacterMasterController.instances[0].body.gameObject;
                inflictDotInfo.victimObject = cb.gameObject;
                inflictDotInfo.damageMultiplier = 1f;
                DotController.InflictDot(ref inflictDotInfo);

                Debug.LogError("[DotAPITests] DotController.InflictDot(ref inflictDotInfo);");
            }
        }
    }
}
