using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming

namespace R2API {
    [R2APISubmodule]
    public static class ResourcesAPI {
        private static readonly Dictionary<string, IResourceProvider> Providers = new Dictionary<string, IResourceProvider>();

        private static NativeDetour ResourcesLoadDetour;
        private delegate Object d_ResourcesLoad(string path, Type type);
        private static d_ResourcesLoad _origLoad;

        private static NativeDetour ResourcesLoadAsyncDetour;
        private delegate ResourceRequest d_ResourcesAsyncLoad(string path, Type type);
        private static d_ResourcesAsyncLoad _origResourcesLoadAsync;

        private static NativeDetour ResourcesLoadAllDetour;
        private delegate Object[] d_ResourcesLoadAll(string path, Type type);
        private static d_ResourcesLoadAll _origLoadAll;

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void InitHooks() {
            ResourcesLoadDetour = new NativeDetour(
                typeof(Resources).GetMethod("Load", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(Type) }, null),
                typeof(ResourcesAPI).GetMethod(nameof(OnResourcesLoad), BindingFlags.Static | BindingFlags.NonPublic)
            );
            _origLoad = ResourcesLoadDetour.GenerateTrampoline<d_ResourcesLoad>();
            ResourcesLoadDetour.Apply();

            ResourcesLoadAsyncDetour = new NativeDetour(
                typeof(Resources).GetMethod("LoadAsyncInternal", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(Type) }, null),
                typeof(ResourcesAPI).GetMethod(nameof(OnResourcesLoadAsync), BindingFlags.Static | BindingFlags.NonPublic)
            );
            _origResourcesLoadAsync = ResourcesLoadAsyncDetour.GenerateTrampoline<d_ResourcesAsyncLoad>();
            ResourcesLoadAsyncDetour.Apply();

            ResourcesLoadAllDetour = new NativeDetour(
                typeof(Resources).GetMethod("LoadAll", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(Type) }, null),
                typeof(ResourcesAPI).GetMethod(nameof(OnResourcesLoadAll), BindingFlags.Static | BindingFlags.NonPublic)
            );
            _origLoadAll = ResourcesLoadAllDetour.GenerateTrampoline<d_ResourcesLoadAll>();
            ResourcesLoadAllDetour.Apply();
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            ResourcesLoadDetour.Undo();
            ResourcesLoadAsyncDetour.Undo();
            ResourcesLoadAllDetour.Undo();
        }

        public static void AddProvider(IResourceProvider provider) {
            Providers.Add(provider.ModPrefix, provider);
        }

        private static Object OnResourcesLoad(string path, Type type) {
            if (path.StartsWith("@")) {
                return ModResourcesLoad(path, type);
            }

            return _origLoad(path, type);
        }

        private static Object ModResourcesLoad(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                R2API.Logger.LogError($"Modded resource paths must be of the form '@ModName:Path/To/Asset.ext'; provided path was '{path}'");
            }

            return provider?.Load(path, type);
        }

        private static ResourceRequest OnResourcesLoadAsync(string path, Type type) {
            if (path.StartsWith("@")) {
                return ModResourcesLoadAsync(path, type);
            }

            return _origResourcesLoadAsync(path, type);
        }

        private static ResourceRequest ModResourcesLoadAsync(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                R2API.Logger.LogError($"Modded resource paths must be of the form '@ModName:Path/To/Asset.ext'; provided path was '{path}'");
            }

            return provider?.LoadAsync(path, type);
        }

        private static Object[] OnResourcesLoadAll(string path, Type type) {
            if (path.StartsWith("@")) {
                return ModResourcesLoadAll(path, type);
            }

            return _origLoadAll(path, type);
        }

        private static Object[] ModResourcesLoadAll(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                R2API.Logger.LogError($"Modded resource paths must be of the form '@ModName:Path/To/Asset.ext'; provided path was '{path}'");
            }

            return provider?.LoadAll(type);
        }
    }
}
