using System;
using R2API.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace R2API {
    /// <summary>
    /// This class provides a wrapper around an AssetBundle for integrating it into the regular Unity Resources library
    /// </summary>
    public sealed class AssetBundleResourcesProvider : IResourceProvider {
        private readonly AssetBundle _bundle;

        /// <summary>
        /// Creates an AssetBundleResourcesProvider with the specified modPrefix.
        /// </summary>
        public AssetBundleResourcesProvider(string? modPrefix, AssetBundle? bundle) {
            ModPrefix = modPrefix;
            _bundle = bundle;
        }

        /// <summary>
        /// The prefix for to access the assets in this provider.
        /// </summary>
        public string ModPrefix { get; }

        /// <summary>
        /// Load an asset out of the included assetbundle.
        /// </summary>
        /// <param name="path">The path to the asset</param>
        /// <param name="type">The type of the asset to find</param>
        /// <returns>object of type <paramref name="type"/></returns>
        public Object Load(string? path, Type? type) {
            return _bundle.LoadAsset(ToBundlePath(path), type);
        }

        /// <summary>
        /// Load an asset out of the included assetbundle asynchronosly.
        /// </summary>
        /// <param name="path">the path to the asset</param>
        /// <param name="type">the type of the asset to find</param>
        /// <returns>object of type <paramref name="type"/></returns>
        public ResourceRequest LoadAsync(string? path, Type? type) {
            var req = new ResourceRequest();
            var bundleReq = _bundle.LoadAssetAsync(ToBundlePath(path), type);
            bundleReq.completed += op => {
                req.SetFieldValue("asset", bundleReq.asset);
                req.SetFieldValue("isDone", bundleReq.isDone);
                req.SetFieldValue("progress", bundleReq.progress);
                req.GetFieldValue<Action<AsyncOperation>>("m_completeCallback").Invoke(req);
            };

            //TODO: Sync progress and config parameters somehow?
            return req;
        }

        /// <summary>
        /// Load all assets in this assetbundle that are assignable from the specified type.
        /// </summary>
        /// <param name="type">The type to match</param>
        /// <returns>Array of the type</returns>
        public Object[] LoadAll(Type? type) {
            return _bundle.LoadAllAssets(type);
        }

        private string ToBundlePath(string path) {
            var split = path.Split(':');
            if (split.Length != 2)
                R2API.Logger.LogError("Modded asset path must be of the format @ModPrefix:Path/To/Resource.ext");

            return split[1];
        }
    }
}
