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
            Assert.True(objs != null, "obj null");
            Assert.True(objs.Length > 3, $"obj count {objs.Length}");

            foreach (var item in objs)
            {
                if (item)
                    R2APITest.Logger.LogWarning("hello there: " + item.name);
            }
        });
    }
}
