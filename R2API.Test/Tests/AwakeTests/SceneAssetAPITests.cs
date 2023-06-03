using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Xunit;

namespace R2API.Test.Tests.AwakeTests;

public class SceneAssetAPITests
{
    public SceneAssetAPITests()
    {
    }

    [Fact]
    public void Test()
    {
        SceneAssetAPI.AddAssetRequest("moon", (objs) =>
        {
            if (objs != null)
                R2APITest.Logger.LogWarning("hello there: " + objs.Length);
            else
                R2APITest.Logger.LogWarning("hello there: objs are null");
            foreach (var item in objs)
            {
                if (item)
                    R2APITest.Logger.LogWarning("hello there: " + item.name);
            }
        });

        //Assert.True(Application.isPlaying);
    }
}
