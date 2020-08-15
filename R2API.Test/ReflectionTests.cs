using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using Xunit;
using Xunit.Abstractions;

namespace R2API.Test {
    public class ReflectionTests {
        private readonly ITestOutputHelper _helper;

        public ReflectionTests(ITestOutputHelper helper) {
            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "cecil");
            this._helper = helper;
        }

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
            testObject.InvokeMethod("Test2", "testValue");

            var val = testObject.GetFieldValue<string>("PrivateValue1");
            Assert.Same("testValue", val);
        }

        [Fact]
        public void TestReflectionStaticCallVoid() {
            typeof(StaticReflectionTestObject).InvokeMethod("Test2", "testValue");

            var val = typeof(StaticReflectionTestObject).GetFieldValue<string>("PrivateValue");
            Assert.Same("testValue", val);
        }

        [Fact]
        public void TestReflectionStaticCall() {
            var val = typeof(StaticReflectionTestObject).InvokeMethod<string>("Test", "test", "1");
            Assert.Equal("test1", val);
        }

        [Fact]
        public void TestReflectionWrongFieldType() {
            Assert.ThrowsAny<Exception>(() => {
                typeof(StaticReflectionTestObject).GetFieldValue<bool>("PrivateValue");
            });
        }

        [Fact]
        public void TestReflectionWrongArgumentCount() {
            Assert.Throws<Exception>(() => {
                var val = typeof(StaticReflectionTestObject).InvokeMethod<string>("Test", "a");
            });
        }

        [Fact]
        public void TestReflectionGetEnumField() {
            var testObject = new ReflectionTestObject();
            var enumValue = testObject.GetFieldValue<TestEnum>("TestEnum");
            Assert.Equal(TestEnum.Test2, enumValue);
        }

        [Fact]
        public void TestReflectionStructFieldGetAndSet() {
            var i = new MyTestStruct(5);
            var newVal = new MyOtherStruct(3);
            i.SetStructFieldValue("_typeName", newVal);
            i.SetStructFieldValue("privateField", 45);

            var typeNameField = typeof(MyTestStruct).GetFieldCached("_typeName");
            var typeNameActual = (MyOtherStruct)typeNameField.GetValue(i);
            var privateField = i.GetFieldValue<int>("privateField");
            Assert.Equal(45, privateField);

            var typeName = i.GetFieldValue<MyOtherStruct>("_typeName");
            Assert.Equal(typeNameActual.Val, typeName.Val);
            Assert.Equal(3, typeName.Val);
        }

        [Fact]
        public void TestReflectionStructStaticGetAndSet() {
            var i = new MyOtherStruct(5);

            i.SetStructFieldValue("PrivateVal", 3);

            var typeNameField = typeof(MyOtherStruct).GetFieldCached("PrivateVal");
            var privateValActual = (int)typeNameField.GetValue(i);

            var privateVal = i.GetFieldValue<int>("PrivateVal");
            Assert.Equal(privateValActual, privateVal);
            Assert.Equal(3, privateVal);
        }

        [Fact]
        public void TestReflectionStructPrivatePropertyGetAndSet() {
            var i = new MyTestStruct(10);
            const string propertyName = "PublicProperty2";
            var privateProperty1 = i.GetStructPropertyValue<MyTestStruct, string>(propertyName);

            Assert.Equal("nice", privateProperty1);

            i.SetStructPropertyValue(propertyName, "test2");
            var privateProperty2 = i.GetStructPropertyValue<MyTestStruct, string>(propertyName);

            Assert.Equal("test2", privateProperty2);
        }

        [Fact]
        public void TestReflectionStructPrivatePropertyGetAndSet2() {
            var i = new MyTestStruct(10);
            const string propertyName = "PrivateProperty";
            var privateProperty1 = i.GetStructPropertyValue<MyTestStruct, int>(propertyName);

            Assert.Equal(10, privateProperty1);

            i.SetStructPropertyValue(propertyName, 5);
            var privateProperty2 = i.GetStructPropertyValue<MyTestStruct, int>(propertyName);

            Assert.Equal(5, privateProperty2);
        }

        [Fact]
        public void TestReflectionExtraCase1() {
            var mock = new RunMock();

            var dictionary = mock.GetFieldValue<IDictionary>("dict");
            Assert.Equal(5, dictionary["thing"]);

            mock.SetPropertyValue("livingPlayerCount", 10);
            //mock.SetPropertyValue("livingPlayerCount", 0);
            mock.SetPropertyValue("participatingPlayerCount", 5);
            Assert.Equal(10, mock.GetPropertyValue<int>("livingPlayerCount"));
            Assert.Equal(5, mock.GetPropertyValue<int>("participatingPlayerCount"));
        }

        [Fact]
        public void TestReflectionStructPublicPropertyGetAndSet() {
            var i = new MyTestStruct(10);
            var publicProperty1 = i.GetStructPropertyValue<MyTestStruct, int>("PublicProperty");
            Assert.Equal(10, publicProperty1);

            i.SetStructPropertyValue("PublicProperty", 15);
            var publicProperty2 = i.GetStructPropertyValue<MyTestStruct, int>("PublicProperty");

            Assert.Equal(15, publicProperty2);
        }

        [Fact]
        public void TestReflectionItemDropAPI() {
            var method = typeof(PickupIndex).GetMethodCached("get_itemIndex");
            Assert.NotNull(method);


            var nextElementUniform = typeof(Xoroshiro128Plus)
                .GetMethods()
                .First(x => x.Name == "NextElementUniform"
                            && x.GetParameters()[0].ParameterType.GUID == typeof(List<>).GUID);

            Assert.NotNull(nextElementUniform);
        }

        [Fact]
        public void TestGetFieldValueException() {
            var cm = new CharacterMaster();
            Assert.Throws<Exception>(() => cm.GetFieldValue<Inventory>("inventory"));
        }
    }

    public enum TestEnum {
        Test1 = 0,
        Test2 = 1
    }

    public struct MyOtherStruct {
        public MyOtherStruct(int val) {
            Val = val;
            PrivateVal = val;
        }

        public int Val;
        private static int PrivateVal;
    }

    public struct MyTestStruct {
        public MyTestStruct(int val) {
            _typeName = new MyOtherStruct(val);
            privateField = val;
            PrivateProperty = val;
            PublicProperty = val;
            PublicProperty2 = "nice";
        }

        private MyOtherStruct _typeName;

        private int privateField;

        private int PrivateProperty { get; set; }
        public int PublicProperty { get; set; }

        public string PublicProperty2 { get; set; }
    }

    public class ReflectionTestBaseObject {
        private string PrivateValue = "SECRET";
        private string PrivateValueCollide = "SECRET_COLLIDE";

        private int BaseTest(int a, int b) {
            return a + b;
        }
    }

    public class ReflectionTestObject : ReflectionTestBaseObject {
        private string PrivateValue1 = "SECRET1";
        private string PrivateValueCollide = "SECRET_COLLIDE_CORRECT";
        private TestEnum TestEnum = TestEnum.Test2;

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

        private static string Test(string a, string b) {
            return a + b;
        }

        private static void Test2(string privateValue) {
            PrivateValue = privateValue;
        }
    }

    public class RunMock {
        public int livingPlayerCount { get; private set; }
        public int participatingPlayerCount { get; private set; }

        private Dictionary<string, int> dict = new Dictionary<string, int> {
            {"thing", 5}
        };
    }
}
