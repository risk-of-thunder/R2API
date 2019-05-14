using R2API.Utils;
using Xunit;

namespace R2API.Test {
    public class ReflectionTests {
        [Fact]
        public void TestReflectionFieldGetAndSet() {
            var testObject = new ReflectionTestObject();
            testObject.SetFieldValue("PrivateValue", "test");
            var ret = testObject.GetFieldValue<string>("PrivateValue");
            Assert.Same("test", ret);
        }

        [Fact]
        public void TestReflectionFieldGetChildFirst() {
            var testObject = new ReflectionTestObject();
            var val = testObject.GetFieldValue<string>("PrivateValueCollide");
            Assert.Same("SECRET_COLLIDE_CORRECT", val);
        }

        [Fact]
        public void TestReflectionStaticFieldGetAndSet() {
            typeof(StaticReflectionTestObject).SetFieldValue("PrivateValue", "test");
            var val = typeof(StaticReflectionTestObject).GetFieldValue<string>("PrivateValue");
            Assert.Same("test", val);
        }

        [Fact]
        public void TestReflectionPropertyGetAndSet() {
            var testObject = new ReflectionTestObject();
            var val = testObject.GetPropertyValue<string>("PrivateProperty");
            Assert.Same("Get off my lawn", val);

            testObject.SetPropertyValue("PrivateProperty", "testProp");
            var val2 = testObject.GetPropertyValue<string>("PrivateProperty");
            Assert.Same("testProp", val2);
        }

        [Fact]
        public void TestReflectionStaticPropertyGetAndSet() {
            var val = typeof(StaticReflectionTestObject).GetPropertyValue<string>("PrivateProperty");
            Assert.Same("Get off my lawn", val);

            typeof(StaticReflectionTestObject).SetPropertyValue("PrivateProperty", "testProp");
            var val2 = typeof(StaticReflectionTestObject).GetPropertyValue<string>("PrivateProperty");
            Assert.Same("testProp", val2);
        }

        [Fact]
        public void TestReflectionCall() {
            var testObject = new ReflectionTestObject();
            var val = testObject.InvokeMethod<string>("Test", "test", "1");
            Assert.Equal("test1", val);
        }

        [Fact]
        public void TestReflectionCallVoid() {
            var testObject = new ReflectionTestObject();
            testObject.InvokeMethod<string>("Test2", "testValue");

            var val = testObject.GetFieldValue<string>("PrivateValue1");
            Assert.Same("testValue", val);
        }
    }

    public class ReflectionTestBaseObject {
        private string PrivateValue = "SECRET";
        private string PrivateValueCollide = "SECRET_COLLIDE";
    }

    public class ReflectionTestObject : ReflectionTestBaseObject {
        private string PrivateValue1 = "SECRET1";
        private string PrivateValueCollide = "SECRET_COLLIDE_CORRECT";
        private string PrivateProperty { get; set; } = "Get off my lawn";

        private string Test(string a, string b) {
            return a + b;
        }

        private void Test2(string privateValue) {
            PrivateValue1 = privateValue;
        }
    }

    public static class StaticReflectionTestObject {
        private static string PrivateValue = "SECRET";
        private static string PrivateProperty { get; set; } = "Get off my lawn";
    }
}
