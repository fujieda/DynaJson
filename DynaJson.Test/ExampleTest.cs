using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using JsonObject = Codeplex.Data.DynamicJson;
#endif
    [TestClass]
    public class ExampleTest
    {
        // ReSharper disable once InconsistentNaming
        private dynamic json;

        [TestInitialize]
        public void Initialize()
        {
            // Parse (from a JSON string to JsonObject)
            json = JsonObject.Parse(@"{
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

        // ReSharper disable all
        public class FooBar
        {
            public string foo { get; set; }
            public int bar;

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
        public void ConvertTo()
        {
            // a JSON objects to a C# object
            var jsonObject = JsonObject.Parse(@"{""foo"":""json"",""bar"":100}");
            var expected = new FooBar
            {
                foo = "json",
                bar = 100
            };
            var foobar = (FooBar)jsonObject; // FooBar
            Assert.AreEqual(expected, foobar);
            var c1 = foobar.bar; // 100
            Assert.AreEqual(100, c1);

            // to a C# dictionary
            var dict1 = (Dictionary<string, dynamic>)jsonObject;
            var c2 = dict1["bar"];
            Assert.AreEqual(100, c2);

            // a JSON array to a C# array
            var jsonArray = JsonObject.Parse("[1,2,3]");
            var array = (int[])jsonArray; // int[]
            Assert.AreEqual(typeof(int[]), array.GetType());
            Assert.AreEqual(6, array.Sum());

            // to a C# list
            var list = (List<int>)jsonArray;
            var sum2 = list.Sum();
            Assert.AreEqual(6, sum2);
        }

        [TestMethod]
        public void Deserialize()
        {
            // a JSON objects to a C# object
            var jsonObject = JsonObject.Parse(@"{""foo"":""json"",""bar"":100}");
            var expected = new FooBar
            {
                foo = "json",
                bar = 100
            };
            var foobar1 = jsonObject.Deserialize<FooBar>(); // dynamic{FooBar}
            Assert.AreEqual(100, foobar1.bar);
            Assert.AreEqual(expected, foobar1);
            var foobar2 = (FooBar)jsonObject; // FooBar
            Assert.AreEqual(expected, foobar2);
            var c1 = foobar1.bar; // 100
            Assert.AreEqual(100, c1);
            // to a C# dictionary
            var dict1 = (Dictionary<string, dynamic>)jsonObject;
            var c2 = dict1["bar"];
            Assert.AreEqual(100, c2);

            // a JSON array to a C# array
            var jsonArray = JsonObject.Parse("[1,2,3]");
            var array1 = jsonArray.Deserialize<int[]>(); // dynamic{int[]}
            Assert.AreEqual(typeof(int[]), array1.GetType());
            var c3 = array1[0]; // 1
            Assert.AreEqual(1, c3);
            var array2 = (int[])jsonArray; // int[]
            Assert.AreEqual(typeof(int[]), array2.GetType());
            Assert.AreEqual(6, array2.Sum());
            // to a C# list
            var list1 = (List<int>)jsonArray;
            var sum2 = list1.Sum();
            Assert.AreEqual(6, sum2);

        }

        [TestMethod]
        public void Serialize()
        {
            var foobar = new[]
            {
                new FooBar {foo = "fooooo!", bar = 1000},
                new FooBar {foo = "orz", bar = 10}
            };
            var json1 = JsonObject.Serialize(foobar); // [{"foo":"fooooo!","bar":1000},{"foo":"orz","bar":10}]
            Assert.AreEqual(@"[{""foo"":""fooooo!"",""bar"":1000},{""foo"":""orz"",""bar"":10}]", json1);

            // Serialize a dictionary
            var dict = new Dictionary<string, int>
            {
                {"aaa", 1},
                {"bbb", 2}
            };
            var json2 = JsonObject.Serialize(dict); // {"aaa":1,"bbb":2}
            Assert.AreEqual(@"{""aaa"":1,""bbb"":2}", json2);

            // Serialize an object created dynamically
            dynamic jsonObject = new JsonObject();
            jsonObject.str = "aaa";
            jsonObject.obj = new {foo = "bar"};
            var json3 = jsonObject.ToString(); // {"str":"aaa","obj":{"foo":"bar"}}
            Assert.AreEqual(@"{""str"":""aaa"",""obj"":{""foo"":""bar""}}", json3);
        }

        [TestMethod]
        public void ModifyJsonObjects()
        {
            dynamic jsonObject = new JsonObject();
            //dynamic jsonObject = JsonObject.Parse("{}");
            // Add properties
            jsonObject.str = "aaa"; // string
            jsonObject.obj = new {foo = "bar"}; // an object
            jsonObject.arr = new[] {"aaa", "bbb"}; // an array
            // Assign a new value
            jsonObject.str = "bbb";
            // Delete the specified property
            var d1 = jsonObject.Delete("str"); // true for success
            Assert.IsTrue(d1);
            var d2 = jsonObject.Delete("str"); // false for failure
            Assert.IsFalse(d2);
            // object("name") works as object.Delete("name")
            var d3 = jsonObject("obj"); // true
            Assert.IsTrue(d3);
        }

        [TestMethod]
        public void ModifyJsonArray()
        {
            //dynamic jsonArray = new JsonObject(new[] {"aaa", "bbb"});
            dynamic jsonArray = JsonObject.Parse(@"[""aaa"",""bbb""]");
            // Assign a new value
            jsonArray[0] = "ccc";
            Assert.AreEqual("ccc", jsonArray[0]);
            // Delete elements
            var e1 = jsonArray.Delete(0); // true for success
            Assert.IsTrue(e1);
            var e2 = jsonArray[0]; // "bbb"
            Assert.AreEqual("bbb", e2);
            // array(index) works as array.Delete(index)
            var e3 = jsonArray(0); // true
            Assert.IsTrue(e3);
#if !DynamicJson
            var len = jsonArray.Length; // 0 (DynaJson only)
            Assert.AreEqual(0, len);
#endif
        }

        [TestMethod]
        public void CreateNewObject()
        {
            dynamic jsonObject1 = new JsonObject();
            jsonObject1.str = "aaa";
            jsonObject1.obj = new {foo = "bar"};
            var json = jsonObject1.ToString(); // {"str":"aaa","obj":{"foo":"bar"}}
            Assert.AreEqual(@"{""str"":""aaa"",""obj"":{""foo"":""bar""}}", json);

#if !DynamicJson
            dynamic jsonObject2 = new JsonObject(new {str = "aaa"});
            jsonObject2.obj = new {foo = "bar"};
            var json2 = jsonObject1.ToString(); // {"str":"aaa","obj":{"foo":"bar"}}
            Assert.AreEqual(json, json2);
#endif
        }

        [TestMethod]
        public void ReservedKeyword()
        {
            var reservedJson = JsonObject.Parse("{\"int\": 0, \"string\": \"foo\"}");
            Assert.AreEqual(0.0, reservedJson.@int);
            Assert.AreEqual("foo", reservedJson.@string);
        }

        [TestMethod]
        public void Enumerate()
        {
            var arrayJson = JsonObject.Parse("[1,2,3]");
            var sum = 0;
            foreach (int item in arrayJson)
                sum += item;
            // sum = 6
            Assert.AreEqual(6, sum);
            Assert.AreEqual(6, ((int[])arrayJson).Sum());

            var objectJson = JsonObject.Parse(@"{""foo"":""json"",""bar"":100}");
            var list = new List<string>();
            foreach (KeyValuePair<string, dynamic> item in objectJson)
                list.Add(item.Key + ":" + item.Value);
            // list = ["foo:json", "bar:100"]
            Assert.That.SequenceEqual(new[] {"foo:json", "bar:100"}, list);
        }

        [TestMethod]
        public void Dictionary()
        {
            const string original = @"{""foo"":""json"",""bar"":100}";
            // To dictionary
            var dict = (Dictionary<string, dynamic>)JsonObject.Parse(original);
            Assert.AreEqual("json", dict["foo"]);
            // To JSON
            var result = JsonObject.Serialize(dict);
            Assert.AreEqual(original, result);
        }
    }
}