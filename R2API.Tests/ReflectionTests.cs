using Microsoft.VisualStudio.TestTools.UnitTesting;
using R2API.Utils;

namespace R2API.Tests {
    [TestClass]
    public class ReflectionTests {
        [TestMethod]
        public void TestReflectionFieldGetAndSet() {
            var testObject = new ReflectionTestObject();
            testObject.SetFieldValue("PrivateValue", "test");
            var ret = testObject.GetFieldValue<string>("PrivateValue");
            Assert.AreEqual("test", ret);
        }

        [TestMethod]
        public void TestReflectionFieldGetChildFirst() {
            var testObject = new ReflectionTestObject();
            var val = testObject.GetFieldValue<string>("PrivateValueCollide");
            Assert.AreEqual("SECRET_COLLIDE_CORRECT", val);
        }

        [TestMethod]
        public void TestReflectionStaticFieldGetAndSet() {
            typeof(StaticReflectionTestObject).SetFieldValue("PrivateValue", "test");
            var val = typeof(StaticReflectionTestObject).GetFieldValue<string>("PrivateValue");
            Assert.AreEqual("test", val);
        }
    }

    public class ReflectionTestBaseObject {
        private string PrivateValue = "SECRET";
        private string PrivateValueCollide = "SECRET_COLLIDE";
    }

    public class ReflectionTestObject : ReflectionTestBaseObject {
        private string PrivateValue1 = "SECRET1";
        private string PrivateValueCollide = "SECRET_COLLIDE_CORRECT";
    }

    public static class StaticReflectionTestObject {
        private static string PrivateValue = "SECRET";
    }
}
