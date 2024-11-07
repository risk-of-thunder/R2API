using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using AssetsTools.NET.Extra;
using BepInEx;
using R2API.AutoVersionGen;
using UnityEngine;

namespace R2API;

/// <summary>
/// API for modifying <see cref="RuntimeAnimatorController" />
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class AnimationsAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".animations";
    public const string PluginName = R2API.PluginName + ".Animations";

    private const string bundleExtension = ".bundle";
    private const string hashExtension = ".hash";
    private const string dummyBundleName = "dummy_controller_bundle";
    private const long dummyAnimatorControllerPathID = -27250459394986890;
    private const string dummyAnimatorControllerPath = "assets/dummycontroller.controller";
    
    private static readonly Dictionary<(string, RuntimeAnimatorController), List<AnimatorModifications>> controllerModifications = [];
    private static readonly Dictionary<RuntimeAnimatorController, List<Animator>> controllerToAnimators = [];
    private static readonly List<UnityEngine.Object> cache = [];

    private static bool _hooksEnabled = false;
    private static bool _requestsDone = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        RoR2.RoR2Application.onLoad += OnLoad;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        RoR2.RoR2Application.onLoad -= OnLoad;

        _hooksEnabled = false;
    }

    private static void OnLoad()
    {
        NativeHelpers.Init();
        ApplyModifications();
    }


    /// <summary>
    /// Add <see cref="RuntimeAnimatorController" /> modifications
    /// </summary>
    /// <param name="sourceBundlePath">Path to a bundle containing <see cref="RuntimeAnimatorController" />. For game bundles you can do System.IO.Path.Combine(Addressables.RuntimePath, "StandaloneWindows64", "bundle_name")</param>
    /// <param name="sourceAnimatorController"><see cref="RuntimeAnimatorController" /> to which modifications would be applied</param>
    /// <param name="modifications">Modifications for <see cref="RuntimeAnimatorController" /></param>
    public static void AddModifications(
        string sourceBundlePath,
        RuntimeAnimatorController sourceAnimatorController,
        AnimatorModifications modifications)
    {
        SetHooks();

        var list = controllerModifications.GetOrAddDefault((sourceBundlePath, sourceAnimatorController), () => []);
        list.Add(modifications);
    }

    /// <summary>
    /// Mapping <see cref="RuntimeAnimatorController" /> to an <see cref="Animator"/>. After modified controller will be created it will be applied to mapped animator. 
    /// </summary>
    /// <param name="animator"><see cref="Animator"/> component from a prefab</param>
    /// <param name="controller"><see cref="RuntimeAnimatorController"/> that will have modifications applied</param>
    public static void AddAnimatorController(Animator animator, RuntimeAnimatorController controller)
    {
        SetHooks();

        var animators = controllerToAnimators.GetOrAddDefault(controller, () => []);
        animators.Add(animator);
    }

    internal static void ApplyModifications()
    {
        var manager = new AssetsManager();

        var dummyPath = Path.Combine(Path.GetDirectoryName(AnimationsPlugin.Instance.Info.Location), dummyBundleName);
        foreach (var ((sourceBundlePath, sourceAnimatorController), modifications) in controllerModifications)
        {
            var sourceAnimatorControllerPathID = NativeHelpers.GetAssetPathID(sourceAnimatorController);
            var modifiedBundlePath = Path.Combine(
                Paths.CachePath,
                "R2API.Animations",
                $"{Path.GetFileName(sourceBundlePath)}_{sourceAnimatorControllerPathID}{bundleExtension}");
            var hashPath = Path.Combine(
                Paths.CachePath,
                "R2API.Animations",
                $"{Path.GetFileName(sourceBundlePath)}_{sourceAnimatorControllerPathID}{hashExtension}");

            var ignoreCache = AnimationsPlugin.IgnoreCache.Value;
            string hash = null;
            if (ignoreCache || !CachedBundleExists(modifiedBundlePath, hashPath, sourceAnimatorControllerPathID, modifications, out hash))
            {
                var bundleFile = manager.LoadBundleFile(sourceBundlePath);
                var assetFile = manager.LoadAssetsFileFromBundle(bundleFile, 0);

                var dummyBundleFile = manager.LoadBundleFile(dummyPath);
                var dummyAssetFile = manager.LoadAssetsFileFromBundle(dummyBundleFile, 0);

                var creator = new ModificationsBundleCreator(
                    manager,
                    assetFile,
                    sourceAnimatorControllerPathID,
                    dummyAssetFile,
                    dummyBundleFile,
                    dummyAnimatorControllerPathID,
                    modifications,
                    modifiedBundlePath
                );

                creator.Run();
                if (!ignoreCache)
                {
                    File.WriteAllText(hashPath, hash);
                }

                manager.UnloadAssetsFile(dummyAssetFile);
                manager.UnloadBundleFile(dummyBundleFile);
            }

            var modifiedBundle = AssetBundle.LoadFromFile(modifiedBundlePath);
            var modifiedAnimatorController = modifiedBundle.LoadAsset<RuntimeAnimatorController>(dummyAnimatorControllerPath);

            if (controllerToAnimators.TryGetValue(sourceAnimatorController, out var animators))
            {
                foreach (var animator in animators)
                {
                    animator.runtimeAnimatorController = modifiedAnimatorController;
                }
            }

            cache.Add(sourceAnimatorController);
            cache.Add(modifiedAnimatorController);
        }

        manager.UnloadAll(true);
        controllerModifications.Clear();
        controllerToAnimators.Clear();
    }

    private static bool CachedBundleExists(string modifiedBundlePath, string hashPath, long sourceAnimatorControllerPathID, List<AnimatorModifications> modifications, out string hash)
    {
        using (var md5 = MD5.Create())
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(RoR2.RoR2Application.buildId);
            writer.Write(sourceAnimatorControllerPathID);
            foreach (var modification in modifications)
            {
                modification.WriteBinary(writer);
            }
            var hashBytes = md5.ComputeHash(stream);
            hash = BitConverter.ToString(hashBytes);
        }

        if (!File.Exists(modifiedBundlePath))
        {
            return false;
        }

        if (!File.Exists(hashPath))
        {
            return false;
        }

        var cachedHash = File.ReadAllText(hashPath);

        return hash == cachedHash;
    }
}
