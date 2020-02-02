using System;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using JsonObject = Codeplex.Data.DynamicJson;
#endif
    [TestClass]
    public class ParseAndOperateTest
    {
        [TestMethod]
        public void PrimitiveValues()
        {
            var @null = JsonObject.Parse("null");
            Assert.AreEqual(null, @null);

            var @bool = JsonObject.Parse("true");
            Assert.AreEqual(typeof(bool), @bool.GetType());

            var number = JsonObject.Parse("0");
            Assert.AreEqual(typeof(double), number.GetType());

            var @string = JsonObject.Parse(@"""a""");
            Assert.AreEqual(typeof(string), @string.GetType());
        }

        [TestMethod]
        public void GetArrayElement()
        {
            var array = JsonObject.Parse("[0,1]");
            Assert.AreEqual(1, array[1]);
        }

        [TestMethod]
        public void GetObjectProperty()
        {
            var obj = JsonObject.Parse(@"{""a"":0}");
            Assert.AreEqual(0, obj.a);
        }

        [TestMethod]
        public void GetObjectPropertyByGetIndex()
        {
            var obj = JsonObject.Parse(@"{""a"":0}");
            Assert.AreEqual(0, obj["a"]);
        }

        [TestMethod]
        public void GetObjectPropertyOfDuplicateKey()
        {
            var obj = JsonObject.Parse(@"{""a"":true,""a"":false}");
            Assert.IsTrue(obj.a);
        }

        [TestMethod]
        public void GetNestedObjectProperty()
        {
            var obj = JsonObject.Parse(@"{""a"":{""b"":0}}");
            Assert.AreEqual(0, obj.a.b);
        }

        [TestMethod]
        public void GetNestedObjectPropertyByGetIndex()
        {
            var obj = JsonObject.Parse(@"{""a"":{""b"":0}}");
            Assert.AreEqual(0, obj["a"]["b"]);
        }

        [TestMethod]
        public void CheckObjectProperty()
        {
            var obj = JsonObject.Parse(@"{""a"":0}");
            Assert.IsTrue(obj.a());
            Assert.IsTrue(obj.IsDefined("a"));
        }

        [TestMethod]
        public void CheckObjectPropertyOfEmptyObject()
        {
            var obj = JsonObject.Parse("{}");
            Assert.IsFalse(obj.a());
            Assert.IsFalse(obj.IsDefined("a"));
        }

        [TestMethod]
        public void CheckArrayBoundary()
        {
            var array = JsonObject.Parse("[0]");
            Assert.IsTrue(array.IsDefined(0));
            Assert.IsFalse(array.IsDefined(1));
        }

#if !DynamicJson
        [TestMethod]
        public void GetArrayLength()
        {
            var array = JsonObject.Parse("[0]");
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(1, array.Count);
        }
#endif

        [TestMethod]
        public void SetObjectProperty()
        {
            var obj = JsonObject.Parse("{}");
            obj.a = "a";
            Assert.AreEqual(obj.a, "a");
        }

        [TestMethod]
        public void SetObjectPropertyWithArray()
        {
            var obj = JsonObject.Parse("{}");
            obj.bar = new[] {0, 1};
            Assert.AreEqual(1, obj.bar[1]);
        }

        [TestMethod]
        public void SetArrayElementBySetIndex()
        {
            var array = JsonObject.Parse("[0]");
            array[0] = 1;
            Assert.AreEqual(1, array[0]);
        }

        [TestMethod]
        public void SetIndexOutOfBounds()
        {
            var array = JsonObject.Parse("[]");
            array[1] = 0; // append element
            Assert.AreEqual(0, array[0]);
            array[3] = 1;
            Assert.AreEqual(1, array[1]);
        }

        [TestMethod]
        public void SetObjectPropertyBySetIndex()
        {
            var obj = JsonObject.Parse("{}");
            obj["a"] = "a";
            Assert.AreEqual(obj.a, "a");
        }

        [TestMethod]
        public void DeleteProperty()
        {
            var obj = JsonObject.Parse(@"{""a"":0}");
            Assert.IsTrue(obj.Delete("a"));
            Assert.IsFalse(obj.Delete("a"));
        }

        [TestMethod]
        public void DeletePropertyByInvoke()
        {
            var obj = JsonObject.Parse(@"{""a"":""0""}");
            Assert.IsTrue(obj("a"));
            Assert.IsFalse(obj("a"));
        }

        [TestMethod]
        public void DeletePropertyByInvokeMember()
        {
            var obj = JsonObject.Parse(@"{""a"":{""b"":0}}");
            Assert.IsTrue(obj.a("b"));
            Assert.IsFalse(obj.a("b"));
        }

        [TestMethod]
        public void DeleteArrayElement()
        {
            var array = JsonObject.Parse("[0,1]");
            Assert.IsTrue(array.Delete(0));
            Assert.AreEqual(1, array[0]);
        }

        [TestMethod]
        public void DeleteArrayElementOutOfLength()
        {
            var array = JsonObject.Parse("[]");
            Assert.IsFalse(array.Delete(0));
        }

        [TestMethod]
        public void DeletePropertyAgainstArray()
        {
            var array = JsonObject.Parse("[]");
            Assert.IsFalse(array.Delete("a"));
        }

        [TestMethod]
        public void DeleteArrayElementByInvoke()
        {
            var array = JsonObject.Parse("[0,1]");
            Assert.IsTrue(array(0));
            Assert.AreEqual(1, array[0]);
        }

        [TestClass]
        public class ErrorTest
        {
            [TestMethod]
            public void GetIndexOutOfRangeError()
            {
                var array = JsonObject.Parse("[]");
                Assert.That.Throws<
#if !DynamicJson
                    IndexOutOfRangeException
#else
                    RuntimeBinderException
#endif
                >(() =>
                {
                    var unused = array[1];
                });
            }

            [TestMethod]
            public void GetMissingPropertyError()
            {
                dynamic obj = new JsonObject();
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    var unused = obj.a;
                });
            }

#if !DynamicJson
            [TestMethod]
            public void GetLengthOfObjectError()
            {
                dynamic obj = new JsonObject();
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    var unused = obj.Length;
                }, "'JsonObject' does not contain a definition for 'Length'");
            }
#endif

            [TestMethod]
            public void SetIndexToPrimitiveError()
            {
                var array = JsonObject.Parse("[true]");
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    array[0][0] = 0;
                });
            }

            [TestMethod]
            public void GetPropertyOfArrayError()
            {
                var array = JsonObject.Parse("[]");
                Assert.That.Throws<
#if DynamicJson
                    FormatException
#else
                    RuntimeBinderException
#endif
                >(() =>
                {
                    var unused = array.a;
                });
            }

            [TestMethod]
            public void SetPropertyOfArrayError()
            {
                var array = JsonObject.Parse("[]");
                Assert.That.Throws<
#if DynamicJson
                    FormatException
#else
                    RuntimeBinderException
#endif
                >(() =>
                {
                    array.a = 0;
                });
            }
        }
    }
}