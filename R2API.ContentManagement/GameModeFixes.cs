using R2API.ContentManagement;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;


namespace R2API;

/// <summary>
/// Class for adding better modded GameMode support.
/// </summary>
internal static class GameModeFixes
{

    public static void AddModdedGameModeSupport()
    {
        // Fixes vanilla multiplayer UI from not showing modded GameModes
        IL.RoR2.UI.MainMenu.MultiplayerMenuController.BuildGameModeChoices += AddModdedGameModesToMultiplayer;

        // Sorts GameModes so they're displayed in alphabetical order in the multiplayer menu
        On.RoR2.GameModeCatalog.SetGameModes += SortGameModes;

        // Adds GameMode button handler to alternate game mode screen
        On.RoR2.UI.LanguageTextMeshController.Start += AddGameModeButton;
    }

    private static void AddModdedGameModesToMultiplayer(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCallvirt(typeof(System.Collections.Generic.List<string>).GetMethod("Contains"))
            ))
        {
            c.Emit(OpCodes.Pop); // Remove the original result
            c.Emit(OpCodes.Ldc_I4_1); // Return true
        }
        else ContentManagementPlugin.Logger.LogError($"Failed to apply AddModdedGameModesToMultiplayer IL Hook");
    }

    private static void SortGameModes(On.RoR2.GameModeCatalog.orig_SetGameModes orig, Run[] newGameModePrefabComponents)
    {
        Array.Sort(newGameModePrefabComponents, (a, b) => string.CompareOrdinal(a.name, b.name));
        orig(newGameModePrefabComponents);
    }

    private static void AddGameModeButton(On.RoR2.UI.LanguageTextMeshController.orig_Start orig, LanguageTextMeshController self)
    {
        orig(self);
        if (!(self.token == "TITLE_ECLIPSE") || !(bool)self.GetComponent<HGButton>())
            return;
        self.transform.parent.gameObject.AddComponent<ModdedGameModeButtonAdder>();
    }

    internal class ModdedGameModeButton : MonoBehaviour
    {
        public HGButton hgButton;
        public string runName;

        public ModdedGameModeButton Initialize(string runName)
        {
            this.runName = runName;
            return this;
        }

        public void Start()
        {
            this.hgButton = this.GetComponent<HGButton>();
            this.hgButton.onClick = new Button.ButtonClickedEvent();
            this.hgButton.onClick.AddListener(() =>
            {
                Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
                RoR2.Console.instance.SubmitCmd(null, $"transition_command \"gamemode {this.runName}; host 0; \"");
            });
        }
    }

    internal class ModdedGameModeButtonAdder : MonoBehaviour
    {
        public void Start()
        {
            for (GameModeIndex gameModeIndex = (GameModeIndex)0; gameModeIndex < (GameModeIndex)GameModeCatalog.gameModeCount; gameModeIndex++)
            {
                Run gameModePrefabComponent = GameModeCatalog.GetGameModePrefabComponent(gameModeIndex);
                ExpansionRequirementComponent component = gameModePrefabComponent.GetComponent<ExpansionRequirementComponent>();
                if (gameModePrefabComponent != null && gameModePrefabComponent.userPickable && (!component || !component.requiredExpansion || EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(component.requiredExpansion.requiredEntitlement)) && gameModePrefabComponent.name.Substring(0, 1) == "x")
                {
                    GameObject newButton = Instantiate(this.transform.Find("GenericMenuButton (Eclipse)").gameObject, this.transform);

                    string runName = gameModePrefabComponent.name;
                    string runNameToken = gameModePrefabComponent.nameToken;
                    GameModeInfo gameModeInfo = gameModePrefabComponent.GetComponent<GameModeInfo>();

                    newButton.AddComponent<ModdedGameModeButton>().Initialize(runName);
                    newButton.GetComponent<LanguageTextMeshController>().token = runNameToken;
                    
                    if (gameModeInfo != null)
                    {
                        newButton.GetComponent<HGButton>().hoverToken = gameModeInfo.buttonHoverDescription;
                    }
                }
            }
        }
    }
}
