using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using DynaJson = Codeplex.Data.DynamicJson;
#endif
    [TestClass]
    public class ExampleTest
    {
        // ReSharper disable once InconsistentNaming
        private dynamic json;

        [TestInitialize]
        public void Initialize()
        {
            // Parse (from a JSON string to DynaJson)
            json = DynaJson.Parse(@"{
                    ""foo"": ""json"",
                    ""bar"": [100,200],
                    ""nest"": {
                        ""foobar"": true
                    }
                }");
        }

        [TestMethod]
        public void Access()
        {
            // Accessing object properties
            var a1 = json.foo; // "json" - dynamic(string)
            Assert.AreEqual("json", a1);
            var a2 = json.nest.foobar; // true - dynamic(bool)
            Assert.IsTrue(a2);
            var a3 = json["nest"]["foobar"]; // Bracket notation
            Assert.IsTrue(a3);
            // Accessing Array elements
            var a4 = json.bar[0]; // 100.0 - dynamic(double)
            Assert.AreEqual(100.0, a4);
        }

        [TestMethod]
        public void CheckProperty()
        {
            // Check the specified property exists
            var b1 = json.IsDefined("foo"); // true
            Assert.IsTrue(b1);
            var b2 = json.IsDefined("foooo"); // false
            Assert.IsFalse(b2);
            // object.name() works as object.IsDefined("name")
            var b3 = json.foo(); // true
            Assert.IsTrue(b3);
            var b4 = json.foooo(); // false
            Assert.IsFalse(b4);
        }

        [TestMethod]
        public void CheckArrayBoundary()
        {
            // Check array boundary
            var b5 = json.bar.IsDefined(1); // true
            Assert.IsTrue(b5);
            var b6 = json.bar.IsDefined(2); // false - out of bounds
            Assert.IsFalse(b6);
        }

#if !DynamicJson
        [TestMethod]
        public void GetArrayLength()
        {
            // Get array length (DynaJson only)
            var len1 = json.bar.Length; // 2
            Assert.AreEqual(2, len1);
            // The same as above
            var len2 = json.bar.Count; // 2
            Assert.AreEqual(2, len2);
        }
#endif

        [TestMethod]
        public void AddProperty()
        {
            // Add properties with C# objects
            json.Arr = new[] {"aaa", "bbb"}; // Array
            Assert.AreEqual("aaa", json.Arr[0]);
            json.Obj = new {aaa = "abc", bbb = 100}; // Object
            Assert.AreEqual("abc", json.Obj.aaa);
        }

        [TestMethod]
        public void DeleteProperty()
        {
            // Delete the specified property
            var d1 = json.Delete("foo"); // true - success
            Assert.IsTrue(d1);
            var d2 = json.Delete("foo"); // false - failure
            Assert.IsFalse(d2);
            // object("name") works as object.Delete("name")
            var d3 = json("bar"); // true
            Assert.IsTrue(d3);
        }

        [TestMethod]
        public void DeleteArrayElement()
        {
            json.Arr = new[] {"aaa", "bbb"}; // Array

            // Deleting elements
            var e1 = json.Arr.Delete(0); // true
            Assert.IsTrue(e1);
            var e2 = json.Arr[0]; // "bbb"
            Assert.AreEqual("bbb", e2);
            // array(index) works as array.Delete(index)
            var e3 = json.Arr(0); // true
            Assert.IsTrue(e3);
#if !DynamicJson
            var len = json.Arr.Length; // 0
            Assert.AreEqual(0, len);
#endif
        }

        [TestMethod]
        public void ReservedKeyword()
        {
            var reservedJson = DynaJson.Parse("{\"int\": 0, \"string\": \"foo\"}");
            Assert.AreEqual(0.0, reservedJson.@int);
            Assert.AreEqual("foo", reservedJson.@string);
        }

        [TestMethod]
        public void Enumerate()
        {
            var arrayJson = DynaJson.Parse("[1,2,3]");
            var sum = 0;
            foreach (int item in arrayJson)
                sum += item;
            Assert.AreEqual(6, sum);
            Assert.AreEqual(6, ((int[])arrayJson).Sum());

            var objectJson = DynaJson.Parse(@"{""foo"":""json"",""bar"":100}");
            var list = new List<string>();
            foreach (KeyValuePair<string, dynamic> item in objectJson)
                list.Add(item.Key + ":" + item.Value); // ["foo:json", "bar:100"]
            Assert.That.SequenceEqual(new[] {"foo:json", "bar:100"}, list);
        }

        // ReSharper disable all
        public class FooBar
        {
            public string foo { get; set; }
            public int bar { get; set; }

            public override bool Equals(object other)
            {
                if (other is FooBar o)
                {
                    if (foo == o.foo && bar == o.bar)
                        return true;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }
        // ReSharper restore all

        [TestMethod]
        public void ConvertTypes()
        {
            var arrayJson = DynaJson.Parse("[1,2,3]");
            var objectJson = DynaJson.Parse(@"{""foo"":""json"",""bar"":100}");

            var array1 = arrayJson.Deserialize<int[]>(); // dynamic{int[]}
            Assert.AreEqual(typeof(int[]), array1.GetType());
            var array2 = (int[])arrayJson; // int[]
            Assert.AreEqual(typeof(int[]), array2.GetType());
            Assert.AreEqual(6, array2.Sum());
            var array3 = (List<int>)arrayJson;
            Assert.AreEqual(6, array3.Sum());

            var expected = new FooBar
            {
                foo = "json",
                bar = 100
            };
            // mapping by public property name
            var foobar1 = objectJson.Deserialize<FooBar>();
            Assert.AreEqual(expected, foobar1);
            var foobar2 = (FooBar)objectJson;
            Assert.AreEqual(expected, foobar2);
            FooBar foobar3 = objectJson;
            Assert.AreEqual(expected, foobar3);
        }

        [TestMethod]
        public void Serialize()
        {
            var obj = new
            {
                Name = "Foo",
                Age = 30,
                Address = new
                {
                    Country = "Japan",
                    City = "Tokyo"
                },
                Like = new[] {"Microsoft", "XBox"}
            };
            // {"Name":"Foo","Age":30,"Address":{"Country":"Japan","City":"Tokyo"},"Like":["Microsoft","XBox"]}
            var json1 = DynaJson.Serialize(obj);
            Assert.AreEqual(
                @"{""Name"":""Foo"",""Age"":30,""Address"":{""Country"":""Japan"",""City"":""Tokyo""},""Like"":[""Microsoft"",""XBox""]}",
                json1);

            // [{"foo":"fooooo!","bar":1000},{"foo":"orz","bar":10}]
            var foobar = new[]
            {
                new FooBar {foo = "fooooo!", bar = 1000},
                new FooBar {foo = "orz", bar = 10}
            };
            var json2 = DynaJson.Serialize(foobar);
            Assert.AreEqual(@"[{""foo"":""fooooo!"",""bar"":1000},{""foo"":""orz"",""bar"":10}]", json2);
        }
    }
}