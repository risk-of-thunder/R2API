using System;
using R2API.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace R2API {
    /// <summary>
    /// This class provides a wrapper around an AssetBundle for integrating it into the regular Unity Resources library
    /// </summary>
    public sealed class AssetBundleResourcesProvider : IResourcesProvider {
        private readonly AssetBundle _bundle;

        public AssetBundleResourcesProvider(string modPrefix, AssetBundle bundle) {
            ModPrefix = modPrefix;
            _bundle = bundle;
        }
        public string ModPrefix { get; }

        public Object Load(string path, Type type) {
            return _bundle.LoadAsset(ToBundlePath(path), type);
        }

        public ResourceRequest LoadAsync(string path, Type type) {
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

        public Object[] LoadAll(Type type) {
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
