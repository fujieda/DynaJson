using System;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using DynaJson = Codeplex.Data.DynamicJson;
#endif
    [TestClass]
    public class ParseAndOperateTest
    {
        [TestMethod]
        public void PrimitiveValues()
        {
            var @null = DynaJson.Parse("null");
            Assert.AreEqual(null, @null);

            var @bool = DynaJson.Parse("true");
            Assert.AreEqual(typeof(bool), @bool.GetType());

            var number = DynaJson.Parse("0");
            Assert.AreEqual(typeof(double), number.GetType());

            var @string = DynaJson.Parse(@"""a""");
            Assert.AreEqual(typeof(string), @string.GetType());
        }

        [TestMethod]
        public void GetArrayElement()
        {
            var array = DynaJson.Parse("[0,1]");
            Assert.AreEqual(1, array[1]);
        }

        [TestMethod]
        public void GetObjectProperty()
        {
            var obj = DynaJson.Parse(@"{""a"":0}");
            Assert.AreEqual(0, obj.a);
        }

        [TestMethod]
        public void GetObjectPropertyByGetIndex()
        {
            var obj = DynaJson.Parse(@"{""a"":0}");
            Assert.AreEqual(0, obj["a"]);
        }

        [TestMethod]
        public void GetObjectPropertyOfDuplicateKey()
        {
            var obj = DynaJson.Parse(@"{""a"":true,""a"":false}");
            Assert.IsTrue(obj.a);
        }

        [TestMethod]
        public void GetNestedObjectProperty()
        {
            var obj = DynaJson.Parse(@"{""a"":{""b"":0}}");
            Assert.AreEqual(0, obj.a.b);
        }

        [TestMethod]
        public void GetNestedObjectPropertyByGetIndex()
        {
            var obj = DynaJson.Parse(@"{""a"":{""b"":0}}");
            Assert.AreEqual(0, obj["a"]["b"]);
        }

        [TestMethod]
        public void CheckObjectProperty()
        {
            var obj = DynaJson.Parse(@"{""a"":0}");
            Assert.IsTrue(obj.a());
            Assert.IsTrue(obj.IsDefined("a"));
        }

        [TestMethod]
        public void CheckObjectPropertyOfEmptyObject()
        {
            var obj = DynaJson.Parse("{}");
            Assert.IsFalse(obj.a());
            Assert.IsFalse(obj.IsDefined("a"));
        }

        [TestMethod]
        public void CheckArrayBoundary()
        {
            var array = DynaJson.Parse("[0]");
            Assert.IsTrue(array.IsDefined(0));
            Assert.IsFalse(array.IsDefined(1));
        }

#if !DynamicJson
        [TestMethod]
        public void GetArrayLength()
        {
            var array = DynaJson.Parse("[0]");
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(1, array.Count);
        }
#endif

        [TestMethod]
        public void SetObjectProperty()
        {
            var obj = DynaJson.Parse("{}");
            obj.a = "a";
            Assert.AreEqual(obj.a, "a");
        }

        [TestMethod]
        public void SetObjectPropertyWithArray()
        {
            var obj = DynaJson.Parse("{}");
            obj.bar = new[] {0, 1};
            Assert.AreEqual(1, obj.bar[1]);
        }

        [TestMethod]
        public void SetArrayElementBySetIndex()
        {
            var array = DynaJson.Parse("[0]");
            array[0] = 1;
            Assert.AreEqual(1, array[0]);
        }

        [TestMethod]
        public void SetIndexOutOfBounds()
        {
            var array = DynaJson.Parse("[]");
            array[1] = 0; // append element
            Assert.AreEqual(0, array[0]);
            array[3] = 1;
            Assert.AreEqual(1, array[1]);
        }

        [TestMethod]
        public void SetObjectPropertyBySetIndex()
        {
            var obj = DynaJson.Parse("{}");
            obj["a"] = "a";
            Assert.AreEqual(obj.a, "a");
        }

        [TestMethod]
        public void DeleteProperty()
        {
            var obj = DynaJson.Parse(@"{""a"":0}");
            Assert.IsTrue(obj.Delete("a"));
            Assert.IsFalse(obj.Delete("a"));
        }

        [TestMethod]
        public void DeletePropertyByInvoke()
        {
            var obj = DynaJson.Parse(@"{""a"":""0""}");
            Assert.IsTrue(obj("a"));
            Assert.IsFalse(obj("a"));
        }

        [TestMethod]
        public void DeletePropertyByInvokeMember()
        {
            var obj = DynaJson.Parse(@"{""a"":{""b"":0}}");
            Assert.IsTrue(obj.a("b"));
            Assert.IsFalse(obj.a("b"));
        }

        [TestMethod]
        public void DeleteArrayElement()
        {
            var array = DynaJson.Parse("[0,1]");
            Assert.IsTrue(array.Delete(0));
            Assert.AreEqual(1, array[0]);
        }

        [TestMethod]
        public void DeleteArrayElementOutOfLength()
        {
            var array = DynaJson.Parse("[]");
            Assert.IsFalse(array.Delete(0));
        }

        [TestMethod]
        public void DeletePropertyAgainstArray()
        {
            var array = DynaJson.Parse("[]");
            Assert.IsFalse(array.Delete("a"));
        }

        [TestMethod]
        public void DeleteArrayElementByInvoke()
        {
            var array = DynaJson.Parse("[0,1]");
            Assert.IsTrue(array(0));
            Assert.AreEqual(1, array[0]);
        }

        [TestClass]
        public class ErrorTest
        {
            [TestMethod]
            public void GetIndexOutOfRangeError()
            {
                var array = DynaJson.Parse("[]");
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
                dynamic obj = new DynaJson();
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    var unused = obj.a;
                });
            }

#if !DynamicJson
            [TestMethod]
            public void GetLengthOfObjectError()
            {
                dynamic obj = new DynaJson();
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    var unused = obj.Length;
                }, "'DynaJson.JsonObject' does not contain a definition for 'Length'");
            }
#endif

            [TestMethod]
            public void SetIndexToPrimitiveError()
            {
                var array = DynaJson.Parse("[true]");
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    array[0][0] = 0;
                });
            }

            [TestMethod]
            public void GetPropertyOfArrayError()
            {
                var array = DynaJson.Parse("[]");
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
                var array = DynaJson.Parse("[]");
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