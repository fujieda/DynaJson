using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using DynaJson = Codeplex.Data.DynamicJson;
#endif
    [TestClass]
    public class SerializeTest
    {
        [TestMethod]
        public void RoundTrip()
        {
            const string input = @"[{""a"":{""b"":""string"",""c"":false},""d"":[true,[0,1],{""e"":null}]},""end""]";
            var obj = DynaJson.Parse(input);

            var tos = obj.ToString();
            Assert.AreEqual(input, tos, "by ToString");
#if !DynamicJson
            var sw = new StringWriter();
            obj.Serialize(sw);
            Assert.AreEqual(input, sw.ToString(), "by Serialize(TextWriter)");

            // the same as ToString for JsonObject
            var ser = DynaJson.Serialize(obj);
            Assert.AreEqual(input, ser, "by DynaJson.Serialize");
#endif
        }

        [TestMethod]
        public void SerializeObject()
        {
            var json = DynaJson.Serialize(new {a = 0, b = false});
            Assert.AreEqual(@"{""a"":0,""b"":false}", json);
        }

#if !DynamicJson
        [TestMethod]
        public void SerializeObjectToTextWriter()
        {
            var sw = new StringWriter();
            DynaJson.Serialize(new {a = 0, b = false}, sw);
            Assert.AreEqual(@"{""a"":0,""b"":false}", sw.ToString());
        }
#endif

        [TestMethod]
        public void SerializeEmptyObject()
        {
            var json = DynaJson.Serialize(new { });
            Assert.AreEqual("{}", json);
        }

        [TestMethod]
        public void SerializeNull()
        {
            var json = DynaJson.Serialize(new object[] {null, DBNull.Value});
            Assert.AreEqual(@"[null,null]", json);
        }

        [TestMethod]
        public void SerializeArray()
        {
            var json = DynaJson.Serialize(new[] {0, 1});
            Assert.AreEqual(@"[0,1]", json);
        }

        [TestMethod]
        public void SerializeEmptyArray()
        {
            var json = DynaJson.Serialize(new int[0]);
            Assert.AreEqual("[]", json);
        }

        [TestMethod]
        public void SerializeArrayOfObject()
        {
            var json = DynaJson.Serialize(new[] {new {a = 0}, new {a = 1}});
            Assert.AreEqual(@"[{""a"":0},{""a"":1}]", json);
        }

        [TestMethod]
        public void SerializeEscapeCharacters()
        {
            var json = DynaJson.Serialize("\\\"/\b\t\n\f\r\u0001大");
            Assert.AreEqual(@"""\\\""\/\b\t\n\f\r\u0001大""", json);
        }

        [TestMethod]
        public void SerializeEmptyString()
        {
            var json = DynaJson.Serialize("");
            Assert.AreEqual(@"""""", json);
        }

        [TestMethod]
        public void SerializeLongString()
        {
            var str = new string(Enumerable.Repeat(' ', 4096).ToArray());
            var json = DynaJson.Serialize(str);
            Assert.AreEqual('"' + str + '"', json);
        }

        [TestMethod]
        public void CreateJsonObjectAndSerialize()
        {
            dynamic obj = new DynaJson();
            obj.a = "b";
            Assert.AreEqual(@"{""a"":""b""}", obj.ToString());
        }

#if !DynamicJson
        [TestMethod]
        public void CreateJsonObjectWithEscapedKeyAndSerialize()
        {
            dynamic obj = new DynaJson();
            obj[@""""] = "b";
            var json = obj.ToString();
            Assert.AreEqual(@"{""\"""":""b""}", json);
        }

        [TestMethod]
        public void CreateJsonObjectFromObjectAndSerialize()
        {
            dynamic obj = new DynaJson(new {a = "b"});
            obj.b = 1;
            var json = obj.ToString();
            Assert.AreEqual(@"{""a"":""b"",""b"":1}", json);
        }
#endif
    }
}