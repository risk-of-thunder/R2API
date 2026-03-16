using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace R2API.ContentManagement;

internal class R2APIGenericContentPack : IContentPackProvider
{

    internal R2APIGenericContentPack(ContentPack finalizedContentPack)
    {
        contentPack = finalizedContentPack;
    }
    public string identifier => contentPack.identifier;

    private ContentPack contentPack;

    private bool logged = false;

    public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
    {
        LogContentsFromContentPack();
        args.ReportProgress(1f);
        yield break;
    }

    public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
    {
        ContentPack.Copy(contentPack, args.output);
        args.ReportProgress(1f);
        yield break;
    }

    public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
    {
        args.ReportProgress(1f);
        yield break;
    }
    private void LogContentsFromContentPack()
    {
        if (logged)
            return;

        logged = true;
        List<string> log = new List<string>();
        log.Add($"Content added from {contentPack.identifier}:");
        log.AddRange(contentPack.bodyPrefabs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.masterPrefabs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.projectilePrefabs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.gameModePrefabs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.networkedObjectPrefabs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.skillDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.skillFamilies.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.sceneDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.itemDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.itemTierDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.itemRelationshipProviders.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.itemRelationshipTypes.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.equipmentDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.buffDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.eliteDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.unlockableDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.survivorDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.artifactDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.effectDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.surfaceDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.networkSoundEventDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.musicTrackDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.gameEndingDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.entityStateConfigurations.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.entityStateTypes.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.expansionDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.entitlementDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.miscPickupDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        log.AddRange(contentPack.craftableDefs.assetInfos.Select(ai => $"{ai.assetName} ({ai.asset.GetType().Name})"));
        ContentManagementPlugin.Logger.LogDebug(string.Join("\n", log));
    }
}
internal class R2APIContentPackProvider
{
    internal static Action WhenAddingContentPacks;

    internal static void Init()
    {
        ContentManager.collectContentPackProviders += AddCustomContent;
    }

    private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
    {
        if (WhenAddingContentPacks != null)
        {
            foreach (Action @event in WhenAddingContentPacks.GetInvocationList())
            {
                try
                {
                    @event();
                }
                catch (Exception e)
                {
                    ContentManagementPlugin.Logger.LogError(e);
                }
            }
        }

        R2APIContentManager.CreateContentPacks();

        foreach (ManagedReadOnlyContentPack managedReadOnlyContentPack in R2APIContentManager.ManagedContentPacks)
        {
            if (managedReadOnlyContentPack.HasAutoCreatedIContentPackProvider)
            {
                try
                {
                    addContentPackProvider(managedReadOnlyContentPack.contentPackProvider);
                }
                catch (Exception e)
                {
                    ContentManagementPlugin.Logger.LogError(e);
                }
            }
        }
    }
}
