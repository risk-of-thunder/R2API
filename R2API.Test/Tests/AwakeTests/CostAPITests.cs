using System;
using R2API;
using R2API.TestingLibrary;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace R2API.Test;

public class CostAPITests {
    // tests reservation and registration
    [Fact]
    public static void Test() {
        // reserve a cost type
        AegisCostType = CostAPI.ReserveCostType(new CostTypeDef() {
            buildCostString = delegate (CostTypeDef def, CostTypeDef.BuildCostStringContext context) {
                context.stringBuilder.Append($"{context.cost} {(context.cost > 1 ? "Aegises" : "Aegis")}");
            },

            isAffordable = delegate (CostTypeDef def, CostTypeDef.IsAffordableContext context) {
                if (!context.activator || !context.activator.TryGetComponent<CharacterBody>(out CharacterBody body)) {
                    return false;
                }

                return body.master && body.master.inventory.GetItemCount(RoR2Content.Items.BarrierOnOverHeal) >= context.cost;
            },
            payCost = delegate (CostTypeDef def, CostTypeDef.PayCostContext context) {
                Inventory inv = context.activatorMaster.inventory;
                inv.RemoveItem(RoR2Content.Items.BarrierOnOverHeal, context.cost);
                context.results.itemsTaken.Add(RoR2Content.Items.BarrierOnOverHeal.itemIndex);
            },
            colorIndex = ColorCatalog.ColorIndex.Tier3Item
        }, (index) => { // use our new costtypeindex to change small chests to cost 25 aegises
            Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1/Chest1.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>().costType = index;
        });
    }

    [SystemInitializer(typeof(CostTypeCatalog))]
    public static void OnInit() {
        // register a cost type after catalog init
        YourSoulIndex = CostAPI.RegisterCostType( new() {
            buildCostString = delegate (CostTypeDef def, CostTypeDef.BuildCostStringContext context) {
                context.stringBuilder.Append($"Your life.");
            },

            isAffordable = delegate (CostTypeDef def, CostTypeDef.IsAffordableContext context) {
                if (!context.activator || !context.activator.TryGetComponent<CharacterBody>(out CharacterBody body)) {
                    return false;
                }

                return true;
            },
            payCost = delegate (CostTypeDef def, CostTypeDef.PayCostContext context) {
                context.activatorBody.healthComponent.Suicide();
            },
            colorIndex = ColorCatalog.ColorIndex.Blood
        });

        Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest2/Chest2.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>().costType = YourSoulIndex;

        Assert.True(YourSoulIndex != (CostTypeIndex)(-1));
        Debug.Log("asserting available");
        Assert.True(AegisCostType.Available);
        Debug.Log("asserting ageis cost type");
        Assert.True(AegisCostType.CostTypeIndex != (CostTypeIndex)(-1));
    }

    public static CostTypeHolder AegisCostType;
    public static CostTypeIndex YourSoulIndex;
}