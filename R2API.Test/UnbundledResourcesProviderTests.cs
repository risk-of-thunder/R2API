using System;
using System.Collections.Generic;
using R2API.Utils;
using UnityEngine;
using Xunit;

namespace R2API.Test {
    public class UnbundledResourcesProviderTests : IClassFixture<UnbundledResourcesProviderFixture>, IDisposable {
        private readonly UnbundledResourcesProvider _provider;

        public UnbundledResourcesProviderTests(UnbundledResourcesProviderFixture fixture) {
            this._provider = fixture.Provider;
        }

        public void Dispose() {
            this._provider.InvokeMethod("Clear");
        }

        [Fact]
        public void TestStoringAndLoadingAsset() {
            var inputTexture = new Texture2D(0, 0);
            var path = this._provider.Store("test", inputTexture);
            var outputTexure = this._provider.Load(path, typeof(Texture2D));

            Assert.Same(inputTexture, outputTexure);
        }

        [Fact]
        public void TestStoringAndLoadingAssetsWithSamePathButDifferentType() {
            var inputTexture2D = new Texture2D(0, 0);
            var inputTexture3D = new Texture3D(0, 0, 0, TextureFormat.R8, false);
            var texture2DPath = this._provider.Store("test", inputTexture2D);
            var texture3DPath = this._provider.Store("test", inputTexture3D);
            var outputTexture2D = this._provider.Load(texture2DPath, typeof(Texture2D));
            var outputTexture3D = this._provider.Load(texture3DPath, typeof(Texture3D));
            Assert.Equal(texture2DPath, texture3DPath);
            Assert.Same(inputTexture2D, outputTexture2D);
            Assert.Same(inputTexture3D, outputTexture3D);
        }

        [Fact]
        public void TestStoringAndLoadingAssetsFromResourcesAPI() {
            var inputTexture = new Texture2D(0, 0);
            var path = this._provider.Store("test", inputTexture);
            var outputTexure = typeof(ResourcesAPI).InvokeMethod<Texture2D>("ModResourcesLoad", path, typeof(Texture2D));

            Assert.Same(inputTexture, outputTexure);
        }
    }

    public class UnbundledResourcesProviderFixture {
        public UnbundledResourcesProvider Provider { get; }

        public UnbundledResourcesProviderFixture() {
            // Need reflection because this is internal
            // Simulates a plugin with a submodule dependency for ResourcesAPI
            _ = typeof(ResourcesAPI).GetProperty(nameof(ResourcesAPI.Loaded)).GetSetMethod(true).Invoke(null, new object[] { true });

            this.Provider = new UnbundledResourcesProvider("@test");
            ResourcesAPI.AddProvider(this.Provider);
        }

    }
}
