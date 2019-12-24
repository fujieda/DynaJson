using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using DynaJson = Codeplex.Data.DynamicJson;
    using JsonParserException = Exception;

#endif
    [TestClass]
    public class ParseTest
    {
        [DataTestMethod]
        [DataRow("123", 123d)]
        [DataRow("-123", -123d)]
        [DataRow("123.456", 123.456)]
        [DataRow("1e10", 1e10d)]
        [DataRow("1e+10", 1e10d)]
        [DataRow("1e-10", 1e-10d)]
        [DataRow("1.1e10", 1.1e10d)]
        public void Number(string json, double expected)
        {
            var result = DynaJson.Parse(json);
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow("01", 1)]
        [DataRow("-01", -1)]
        public void NumberWithLeadingZero(string json, double expected)
        {
            var result = DynaJson.Parse(json);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TrailingComma()
        {
            var array = DynaJson.Parse("[0,]");
            Assert.AreEqual(0, array[0]);

            var obj = DynaJson.Parse(@"{""a"":0,}");
            Assert.AreEqual(0, obj.a);
        }

        [DataTestMethod]
        [DataRow(@"""a""", "a", "normal case")]
        [DataRow(@"""\/\""\\\b\f\n\r\t""", "/\"\\\b\f\n\r\t", "escape characters")]
        [DataRow(@"""\u9cf3\u7FD4""", "鳳翔", "unicode escapes")]
        [DataRow(@"""0\t1\u59272""", "0\t1大2", "both of normal and escape characters")]
        public void String(string json, string expected, string message = null)
        {
            var result = (string)DynaJson.Parse(json);
            Assert.AreEqual(expected, result, message);
        }

        [TestMethod]
        public void WhiteSpace()
        {
            var obj = DynaJson.Parse(" {\t\"a\"\r : \n\"b\"  , \"c\" : [ 0 , 1 ] } ");
            Assert.IsTrue(obj.IsObject);
        }

        [TestMethod]
        public void LongJson()
        {
            var json = "[" + string.Join(",", Enumerable.Repeat('0', JsonParser.ReaderBufferSize / 2)) + "]";
            var array = DynaJson.Parse(json);
            Assert.IsTrue(array.IsArray);
        }

        [TestMethod]
        public void LongJsonOnStream()
        {
            var json = "[" + string.Join(",", Enumerable.Repeat('0', JsonParser.ReaderBufferSize / 2)) + "]";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var array = DynaJson.Parse(stream, Encoding.UTF8);
                Assert.IsTrue(array.IsArray);
            }
        }

        [TestMethod]
        public void LongString()
        {
            const int capacity = JsonParser.StringInitialCapacity;
            var sb = new StringBuilder();
            sb.Append('"');
            var zero = new char[capacity + 1];
            Array.Fill(zero, '0');
            sb.Append(zero);
            sb.Append('"');
            var str = (string)DynaJson.Parse(sb.ToString());
            Assert.AreEqual(capacity + 1, str.Length);
        }

        [TestClass]
        public class ErrorTest
        {
            [TestMethod]
            public void Empty()
            {
                Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(""), "Unexpected end at 0");
            }

            [TestMethod]
            public void InvalidChar()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse("a"),
                    "Unexpected character 'a' at 0");
            }

            [DataTestMethod]
            // ReSharper disable StringLiteralTypo
            [DataRow("nula", "Expecting 'l' at 3")]
            [DataRow("fals", "Expecting 'e' at 4")]
            [DataRow("truee", "Unexpected character 'e' at 4")]
            // ReSharper restore StringLiteralTypo
            public void IncorrectToken(string json, string message)
            {
                Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(json), message);
            }

            [TestMethod]
            public void InvalidEscapeCharacter()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse(@"""\a"""),
                    "Invalid escape character 'a' at 2");
            }

            [TestMethod]
            public void InvalidUnicodeEscape()
            {
                Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(@"""\u123g"""));
            }

            [TestMethod]
            public void EscapeAtEnd()
            {
                Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(@"""\"""));
            }

            [TestMethod]
            public void UnexpectedEnd()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse(@"""a"),
                    "Unexpected end at 2");
            }

            [TestMethod]
            public void RawControlCharacter()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse("\"\b\""),
                    "Unexpected character '\b' at 1");
            }

            [DataTestMethod]
            [DataRow("1a", "Unexpected character 'a' at 1")]
            [DataRow("-a", "Expecting digit at 1")]
            [DataRow("0.a", "Expecting digit at 2")]
            [DataRow("1ea", "Expecting digit at 2")]
            [DataRow("1.1a", "Unexpected character 'a' at 3")]
            public void NumberWithGarbage(string json, string message)
            {
                Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(json), message);
            }

            [TestMethod]
            public void MissingColon()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse(@"{""at"",1}"),
                    "Expecting ':' at 5");
            }

            [TestMethod]
            public void DoubleColon()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse(@"{""at""::1}"),
                    "Unexpected character ':' at 6");
            }

            [TestMethod]
            public void MemberNameIsNotString()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse("{a:1}"),
                    "Expecting string at 1");
            }

            [DataTestMethod]
            [DataRow(@"{""a"":1,")]
            public void MissingMember(string json)
            {
#if !DynamicJson
            Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(json),
                "Expecting string at 7");
#endif
            }

            [TestMethod]
            public void MissingCurlyBracket()
            {
#if !DynamicJson
            Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(@"{""a"":1"),
                "Expecting ',' or '}' at 6");
#endif
            }

            [DataTestMethod]
            [DataRow("[1,", "Unexpected end at 3")]
            public void MissingValueInArray(string json, string message)
            {
                Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(json), message);
            }

            [TestMethod]
            public void ColonInsteadOfComma()
            {
                Assert.That.Throws<JsonParserException>(() =>
                        DynaJson.Parse("[1:"),
                    "Expecting ',' or ']' at 2");
            }

            [DataTestMethod]
            [DataRow("[1,,]", "Unexpected character ',' at 3")]
            [DataRow(@"{""a"":0,,}", "Expecting string at 7")]
            public void DoubleComma(string json, string message)
            {
                Assert.That.Throws<JsonParserException>(() =>
                    DynaJson.Parse(json), message);
            }

            [TestMethod]
            public void TooDeepNestedArray()
            {
                Assert.That.Throws<JsonParserException>(() =>
                {
                    var reader = new StringReader("[[]]");
                    var unused = JsonParser.Parse(reader, 1);
                }, "Too deep nesting 2 at 1");
            }

            [TestMethod]
            public void TooDeepNestedObject()
            {
                Assert.That.Throws<JsonParserException>(() =>
                {
                    var reader = new StringReader(@"{""a"":{}}");
                    var unused = JsonParser.Parse(reader, 1);
                }, "Too deep nesting 2 at 5");
            }

            [DataTestMethod]
            [DataRow("]")]
            [DataRow("}")]
            public void UnexpectedCloseBracket(string bracket)
            {
                Assert.That.Throws<JsonParserException>(() =>
                {
                    var unused = DynaJson.Parse(bracket);
                }, $"Unexpected character '{bracket}' at 0");
            }
        }
    }
}