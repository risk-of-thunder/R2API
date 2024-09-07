using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Utils;
using BepInEx.Configuration;
using R2API.TestingLibrary;

namespace R2API.Test.Tests.AwakeTests;

// TODO: Figure which fucking test fucking native crash Unity
/*public class ReflectionTests
{
#pragma warning disable CS0414 // unusued
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0051 // Remove unused private members


    public ReflectionTests()
    {
    }

    [Fact]
    public void TestReflectionFieldGetAndSet()
    {
        var testObject = new ReflectionTestObject();
        testObject.SetFieldValue("PrivateValue", "test");
        var ret = testObject.GetFieldValue<string>("PrivateValue");
        Assert.Same("test", ret);
    }

    [Fact]
    public void TestReflectionFieldGetChildFirst()
    {
        var testObject = new ReflectionTestObject();
        var val = testObject.GetFieldValue<string>("PrivateValueCollide");
        Assert.Same("SECRET_COLLIDE_CORRECT", val);
    }

    [Fact]
    public void TestReflectionStaticFieldGetAndSet()
    {
        typeof(StaticReflectionTestObject).SetFieldValue("PrivateValue", "test");
        var val = typeof(StaticReflectionTestObject).GetFieldValue<string>("PrivateValue");
        Assert.Same("test", val);
    }

    [Fact]
    public void TestReflectionConstFieldGetAndSet()
    {
        var type = typeof(StaticReflectionTestObject);
        var fieldName = "PrivateConstValue";
        var val = type.GetFieldValue<string>(fieldName);
        Assert.Equal("SECRET_CONST", val);
        Assert.Throws<FieldAccessException>(() => type.SetFieldValue(fieldName, "whatever"));
    }

    [Fact]
    public void TestReflectionGetFieldGetDelegate()
    {
        var val1 = typeof(ReflectionTestObject).GetFieldGetDelegate<object>("PrivateValue1");
        var val2 = typeof(ReflectionTestObject).GetFieldGetDelegate<object>("PrivateValue1");
        var val3 = typeof(ReflectionTestObject).GetFieldGetDelegate<string>("PrivateValue1");
        Assert.Same(val1, val2);
        Assert.NotSame(val1, val3);
    }

    [Fact]
    public void TestReflectionGetFieldSetDelegate()
    {
        var val1 = typeof(ReflectionTestObject).GetFieldSetDelegate<object>("PrivateValue2");
        var val2 = typeof(ReflectionTestObject).GetFieldSetDelegate<object>("PrivateValue2");
        var val3 = typeof(ReflectionTestObject).GetFieldSetDelegate<int>("PrivateValue2");
        Assert.Same(val1, val2);
        Assert.NotSame(val1, val3);
    }

    [Fact]
    public void TestReflectionPropertyGetAndSet()
    {
        var testObject = new ReflectionTestObject();
        var val = testObject.GetPropertyValue<string>("PrivateProperty");
        Assert.Same("Get off my lawn", val);

        testObject.SetPropertyValue("PrivateProperty", "testProp");
        var val2 = testObject.GetPropertyValue<string>("PrivateProperty");
        Assert.Same("testProp", val2);
    }

    [Fact]
    public void TestReflectionStaticPropertyGetAndSet()
    {
        var val = typeof(StaticReflectionTestObject).GetPropertyValue<string>("PrivateProperty");
        Assert.Same("Get off my lawn", val);

        typeof(StaticReflectionTestObject).SetPropertyValue("PrivateProperty", "testProp");
        var val2 = typeof(StaticReflectionTestObject).GetPropertyValue<string>("PrivateProperty");
        Assert.Same("testProp", val2);
    }

    [Fact]
    public void TestReflectionCall()
    {
        var testObject = new ReflectionTestObject();
        var val = testObject.InvokeMethod<string>("Test", "test", "1");
        Assert.Equal("test1", val);
    }

    [Fact]
    public void TestReflectionFastReflectionDelegateCache()
    {
        var val1 = typeof(ReflectionTestObject).GetMethodDelegateCached("Test3", new Type[] { });
        var val2 = typeof(ReflectionTestObject).GetMethodDelegateCached("Test3", new Type[] { });
        Assert.NotNull(val1);
        Assert.Same(val1, val2);
    }

    [Fact]
    public void TestReflectionMethodInfoCache()
    {
        var val1 = typeof(ReflectionTestObject).GetMethodCached("Test3", new Type[] { });
        var val2 = typeof(ReflectionTestObject).GetMethodCached("Test3", new Type[] { });
        Assert.NotNull(val1);
        Assert.Same(val1, val2);
    }

    [Fact]
    public void TestReflectionCombineHashCode()
    {
        var hashcode1 = Reflection.CombineHashCode(new Type[]
            {typeof(string), typeof(int), typeof(string), typeof(int)});
        var hashcode2 = Reflection.CombineHashCode(new Type[]
            {typeof(string), typeof(string), typeof(int), typeof(int)});
        var hashcode3 = Reflection.CombineHashCode(new Type[]
            {typeof(string), typeof(string), typeof(int), typeof(int)});
        Assert.NotEqual(hashcode1, hashcode2);
        Assert.Equal(hashcode2, hashcode3);
    }

    [Fact]
    public void TestReflectionConstructorCache()
    {
        var val1 = typeof(ReflectionTestObject).GetConstructorCached(new Type[] { });
        var val2 = typeof(ReflectionTestObject).GetConstructorCached(new Type[] { });
        Assert.NotNull(val1);
        Assert.Same(val1, val2);
    }

    [Fact]
    public void TestReflectionCallVoid()
    {
        var testObject = new ReflectionTestObject();
        testObject.InvokeMethod("Test2", "testValue");

        var val = testObject.GetFieldValue<string>("PrivateValue1");
        Assert.Same("testValue", val);
    }

    [Fact]
    public void TestReflectionStaticCallVoid()
    {
        typeof(StaticReflectionTestObject).InvokeMethod("Test2", "testValue");

        var val = typeof(StaticReflectionTestObject).GetFieldValue<string>("PrivateValue");
        Assert.Same("testValue", val);
    }

    [Fact]
    public void TestReflectionStaticCall()
    {
        var val = typeof(StaticReflectionTestObject).InvokeMethod<string>("Test", "test", "1");
        Assert.Equal("test1", val);
    }

    [Fact]
    public void TestReflectionWrongFieldType()
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            typeof(StaticReflectionTestObject).GetFieldValue<bool>("PrivateValue");
        });
    }

    [Fact]
    public void TestReflectionWrongArgumentCount()
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            var val = typeof(StaticReflectionTestObject).InvokeMethod<string>("Test", "a");
        });
    }

    [Fact]
    public void TestReflectionGetEnumField()
    {
        var testObject = new ReflectionTestObject();
        var enumValue = testObject.GetFieldValue<TestEnum>("TestEnum");
        Assert.Equal(TestEnum.Test2, enumValue);
    }

    [Fact]
    public void TestReflectionGetFieldValueBoxed()
    {
        var testObject = new ReflectionTestObject();
        var boxedValue = testObject.GetFieldValue<object>("TestEnum");
        Assert.Equal(TestEnum.Test2, boxedValue);
    }

    [Fact]
    public void TestReflectionSetFieldValueBoxed()
    {
        var testObject = new ReflectionTestObject();
        testObject.SetFieldValue("PrivateValue2", 123);
        Assert.Equal(123, testObject.GetFieldValue<object>("PrivateValue2"));

        var testStruct = new MyTestStruct(123);
        testStruct.SetStructFieldValue("privateObjectField", 456);
        Assert.Equal(456, testStruct.GetFieldValue<object>("privateObjectField"));
    }

    [Fact]
    public void TestReflectionGetPropertyValueBoxed()
    {
        var testObject = new ReflectionTestObject();
        var boxedValue = testObject.GetPropertyValue<object>("PrivateIntProperty");
        Assert.Equal(123, boxedValue);
    }

    [Fact]
    public void TestReflectionSetPropertyValueBoxed()
    {
        var testObject = new ReflectionTestObject();
        testObject.SetPropertyValue("PrivateObjectProperty", 123);
        Assert.Equal(123, testObject.GetPropertyValue<object>("PrivateObjectProperty"));
    }

    [Fact]
    public void TestReflectionGetStructPropertyValueBoxed()
    {
        var myTestStruct = new MyTestStruct();
        myTestStruct.SetStructPropertyValue("PrivateProperty", 123);
        var boxedValue = myTestStruct.GetStructPropertyValue<MyTestStruct, object>("PrivateProperty");
        Assert.Equal(123, boxedValue);
    }

    [Fact]
    public void TestReflectionSetStructPropertyValueBoxed()
    {
        var myTestStruct = new MyTestStruct();
        myTestStruct.SetStructPropertyValue("PrivateObjectProperty", 123);
        var boxedValue = myTestStruct.GetStructPropertyValue<MyTestStruct, object>("PrivateObjectProperty");
        Assert.Equal(123, boxedValue);
    }

    [Fact]
    public void TestReflectionStructFieldGetAndSet()
    {
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
    public void TestReflectionStructStaticGetAndSet()
    {
        var i = new MyOtherStruct(5);

        i.SetStructFieldValue("PrivateVal", 3);

        var typeNameField = typeof(MyOtherStruct).GetFieldCached("PrivateVal");
        var privateValActual = (int)typeNameField.GetValue(i);

        var privateVal = i.GetFieldValue<int>("PrivateVal");
        Assert.Equal(privateValActual, privateVal);
        Assert.Equal(3, privateVal);
    }

    [Fact]
    public void TestReflectionStructPrivatePropertyGetAndSet()
    {
        var i = new MyTestStruct(10);
        const string propertyName = "PublicProperty2";
        var privateProperty1 = i.GetStructPropertyValue<MyTestStruct, string>(propertyName);

        Assert.Equal("nice", privateProperty1);

        i.SetStructPropertyValue(propertyName, "test2");
        var privateProperty2 = i.GetStructPropertyValue<MyTestStruct, string>(propertyName);

        Assert.Equal("test2", privateProperty2);
    }

    [Fact]
    public void TestReflectionStructPrivatePropertyGetAndSet2()
    {
        var i = new MyTestStruct(10);
        const string propertyName = "PrivateProperty";
        var privateProperty1 = i.GetStructPropertyValue<MyTestStruct, int>(propertyName);

        Assert.Equal(10, privateProperty1);

        i.SetStructPropertyValue(propertyName, 5);
        var privateProperty2 = i.GetStructPropertyValue<MyTestStruct, int>(propertyName);

        Assert.Equal(5, privateProperty2);
    }

    [Fact]
    public void TestReflectionExtraCase1()
    {
        var mock = new RunMock();

        var dictionary = mock.GetFieldValue<IDictionary>("dict");
        Assert.Equal(5, dictionary["thing"]);

        mock.SetPropertyValue("livingPlayerCount", 10);
        mock.SetPropertyValue("participatingPlayerCount", 5);
        Assert.Equal(10, mock.GetPropertyValue<int>("livingPlayerCount"));
        Assert.Equal(5, mock.GetPropertyValue<int>("participatingPlayerCount"));
    }

    [Fact]
    public void TestReflectionStructPublicPropertyGetAndSet()
    {
        var i = new MyTestStruct(10);
        var publicProperty1 = i.GetStructPropertyValue<MyTestStruct, int>("PublicProperty");
        Assert.Equal(10, publicProperty1);

        i.SetStructPropertyValue("PublicProperty", 15);
        var publicProperty2 = i.GetStructPropertyValue<MyTestStruct, int>("PublicProperty");

        Assert.Equal(15, publicProperty2);
    }

    [Fact]
    public void TestReflectionItemDropAPI()
    {
        var nextElementUniform = typeof(Xoroshiro128Plus).GetMethodWithConstructedGenericParameter("NextElementUniform", typeof(List<>));
        Assert.NotNull(nextElementUniform);

        var nextElementUniformExact = nextElementUniform.MakeGenericMethod(typeof(PickupIndex));
        Assert.NotNull(nextElementUniformExact);
    }

    [Fact]
    public void TestGetFieldValueException()
    {
        var cm = new CharacterMaster();
        Assert.Throws<ArgumentException>(() => cm.GetFieldValue<Inventory>("inventory"));
    }

    [Fact]
    public void TestRetrieveInheritedMember()
    {
        var qsdqsd2 = typeof(TestInheritB);
        var val2 = qsdqsd2.GetFieldValue<int>(nameof(TestInheritB.A));
        Assert.Equal(8, val2);
    }
}

public enum TestEnum
{
    Test1 = 1,
    Test2 = 0
}

public struct MyOtherStruct
{
    public MyOtherStruct(int val)
    {
        Val = val;
        PrivateVal = val;
    }

    public int Val;
    private static int PrivateVal;
}

public struct MyTestStruct
{
    public MyTestStruct(int val)
    {
        _typeName = new MyOtherStruct(val);
        privateField = val;
        privateObjectField = val;
        PrivateProperty = val;
        PrivateObjectProperty = val;
        PublicProperty = val;
        PublicProperty2 = "nice";
    }

    private MyOtherStruct _typeName;

    private int privateField;
    private object privateObjectField;

    private int PrivateProperty { get; set; }
    private object PrivateObjectProperty { get; set; }
    public int PublicProperty { get; set; }

    public string PublicProperty2 { get; set; }
}

public class ReflectionTestBaseObject
{
    private string PrivateValue = "SECRET";
    private string PrivateValueCollide = "SECRET_COLLIDE";

    private int BaseTest(int a, int b)
    {
        return a + b;
    }
}

public class ReflectionTestObject : ReflectionTestBaseObject
{
    private string PrivateValue1 = "SECRET1";
    private object PrivateValue2 = "SECRET2";
    private string PrivateValueCollide = "SECRET_COLLIDE_CORRECT";
    private TestEnum TestEnum = TestEnum.Test2;

    private string PrivateProperty { get; set; } = "Get off my lawn";

    private int PrivateIntProperty { get; set; } = 123;
    private object PrivateObjectProperty { get; set; } = "SECRET3";

    private string Test(string a, string b)
    {
        return a + b;
    }

    private void Test2(string privateValue)
    {
        PrivateValue1 = privateValue;
    }

    private string Test3()
    {
        return "";
    }

    private string Test3(string arg)
    {
        return arg;
    }
}

public static class StaticReflectionTestObject
{
    private const string PrivateConstValue = "SECRET_CONST";
    private static string PrivateValue = "SECRET";
    private static string PrivateProperty { get; set; } = "Get off my lawn";

    private static string Test(string a, string b)
    {
        return a + b;
    }

    private static void Test2(string privateValue)
    {
        PrivateValue = privateValue;
    }
}

public class TestInheritA
{
    public static int A = 4;
}

public class TestInheritB : TestInheritA
{
    public static new int A = 8;
}


public class RunMock
{
    public int livingPlayerCount { get; private set; }
    public int participatingPlayerCount { get; private set; }

    private Dictionary<string, int> dict = new Dictionary<string, int> {
        {"thing", 5}
    };
}
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0414 // unusued
*/
