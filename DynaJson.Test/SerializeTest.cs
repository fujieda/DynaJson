using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using JsonObject = Codeplex.Data.DynamicJson;
#endif
    [TestClass]
    public class SerializeTest
    {
        [TestMethod]
        public void RoundTrip()
        {
            const string input = @"[{""a"":{""b"":""string"",""c"":false},""d"":[true,[0,1],{""e"":null}]},""end""]";
            var obj = JsonObject.Parse(input);

            var tos = obj.ToString();
            Assert.AreEqual(input, tos, "by ToString");
#if !DynamicJson
            var sw = new StringWriter();
            obj.Serialize(sw);
            Assert.AreEqual(input, sw.ToString(), "by Serialize(TextWriter)");

            // the same as ToString for JsonObject
            var ser = JsonObject.Serialize(obj);
            Assert.AreEqual(input, ser, "by JsonObject.Serialize");
#endif
        }

        [TestMethod]
        public void SerializeObject()
        {
            var json = JsonObject.Serialize(new {a = 0, b = false});
            Assert.AreEqual(@"{""a"":0,""b"":false}", json);
        }

#if !DynamicJson
        [TestMethod]
        public void SerializeObjectToTextWriter()
        {
            var sw = new StringWriter();
            JsonObject.Serialize(new {a = 0, b = false}, sw);
            Assert.AreEqual(@"{""a"":0,""b"":false}", sw.ToString());
        }
#endif

        [TestMethod]
        public void SerializeEmptyObject()
        {
            var json = JsonObject.Serialize(new { });
            Assert.AreEqual("{}", json);
        }

        [TestMethod]
        public void SerializeNull()
        {
            var json = JsonObject.Serialize(new object[] {null, DBNull.Value});
            Assert.AreEqual(@"[null,null]", json);
        }

        [TestMethod]
        public void SerializeArray()
        {
            var json = JsonObject.Serialize(new[] {0, 1});
            Assert.AreEqual(@"[0,1]", json);
        }

        [TestMethod]
        public void SerializeEmptyArray()
        {
            var json = JsonObject.Serialize(new int[0]);
            Assert.AreEqual("[]", json);
        }

        [TestMethod]
        public void SerializeArrayOfObject()
        {
            var json = JsonObject.Serialize(new[] {new {a = 0}, new {a = 1}});
            Assert.AreEqual(@"[{""a"":0},{""a"":1}]", json);
        }

        [TestMethod]
        public void SerializeEscapeCharacters()
        {
            var json = JsonObject.Serialize("\\\"/\b\t\n\f\r\u0001大");
            Assert.AreEqual(@"""\\\""\/\b\t\n\f\r\u0001大""", json);
        }

        [TestMethod]
        public void SerializeEmptyString()
        {
            var json = JsonObject.Serialize("");
            Assert.AreEqual(@"""""", json);
        }

        [TestMethod]
        public void SerializeLongString()
        {
            var str = new string(Enumerable.Repeat(' ', 4096).ToArray());
            var json = JsonObject.Serialize(str);
            Assert.AreEqual('"' + str + '"', json);
        }

        [TestMethod]
        public void CreateJsonObjectAndSerialize()
        {
            dynamic obj = new JsonObject();
            obj.a = "b";
            Assert.AreEqual(@"{""a"":""b""}", obj.ToString());
        }

#if !DynamicJson
        [TestMethod]
        public void CreateJsonObjectWithEscapedKeyAndSerialize()
        {
            dynamic obj = new JsonObject();
            obj[@""""] = "b";
            var json = obj.ToString();
            Assert.AreEqual(@"{""\"""":""b""}", json);
        }

        [TestMethod]
        public void CreateJsonObjectFromObjectAndSerialize()
        {
            dynamic obj = new JsonObject(new {a = "b"});
            obj.b = 1;
            var json = obj.ToString();
            Assert.AreEqual(@"{""a"":""b"",""b"":1}", json);
        }
#endif

        private class A
        {
            public string S { get; set; }
            public A Obj { get; set; }
        }

        [TestMethod]
        public void SerializeObjectHaveNull()
        {
            var a = new A();
            var json = JsonObject.Serialize(a);
            Assert.AreEqual(@"{""S"":null,""Obj"":null}", json);
        }

        [TestMethod]
        public void SerializeDictionary()
        {
            var dict = new Dictionary<string, int> {["a"] = 0};
            var json = JsonObject.Serialize(dict);
            Assert.AreEqual(@"{""a"":0}", json);
        }
    }
}