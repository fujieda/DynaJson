using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using JsonObject = Codeplex.Data.DynamicJson;
#endif

    [TestClass]
    public class ConvertTest
    {
        [TestMethod]
        public void ConvertToArray()
        {
            var bArray = (bool[])JsonObject.Parse("[false]");
            Assert.IsFalse(bArray[0]);

            var sArray = (string[])JsonObject.Parse(@"[""a""]");
            Assert.AreEqual("a", sArray[0]);
        }

        [TestMethod]
        public void ConvertToList()
        {
            var list = (List<string>)JsonObject.Parse(@"[""a""]");
            Assert.AreEqual("a", list[0]);
        }

        [TestMethod]
        public void ConvertBetweenConvertibleTypes()
        {
            var bArray = (bool[])JsonObject.Parse("[0]");
            Assert.IsFalse(bArray[0]);

            var sArray = (string[])JsonObject.Parse("[0]");
            Assert.AreEqual("0", sArray[0]);

            var dArray = (double[])JsonObject.Parse(@"[""0""]");
            Assert.AreEqual(0, dArray[0]);
        }

        [TestMethod]
        public void ConvertToArrayOfInt()
        {
            var array = (int[])JsonObject.Parse("[0]");
            Assert.AreEqual(0, array[0]);
        }

        [TestMethod]
        public void ConvertToListOfInt()
        {
            var list = (List<int>)JsonObject.Parse("[0]");
            Assert.AreEqual(0, list[0]);
        }

        [TestMethod]
        public void ConvertMixedArrayToArray()
        {
            var array = (double[])JsonObject.Parse("[0,true]");
            Assert.AreEqual(1d, array[1]);
        }

        [TestMethod]
        public void ConvertToArrayOfObject()
        {
            var array = (dynamic[])JsonObject.Parse(@"[{""a"":0},{""a"":1}]");
            Assert.AreEqual(1d, array[1].a);
        }

        [TestMethod]
        public void ConvertToNestedArray()
        {
            var array = (double[][])JsonObject.Parse("[[0,1],[2,3]]");
            Assert.AreEqual(3d, array[1][1]);
        }

        [TestMethod]
        public void ConvertNull()
        {
            var objArray = (object[])JsonObject.Parse("[null]");
            Assert.AreEqual(null, objArray[0]);

            var strArray = (string[])JsonObject.Parse("[null]");
            Assert.AreEqual(null, strArray[0]);

            var nested = (object[][])JsonObject.Parse("[null]");
            Assert.AreEqual(null, nested[0]);
        }

        // 
        // ReSharper disable all
        private class A
        {
            public string S { get; set; }
            public DateTime D { get; set; }
            public double F;
        }

        private class Empty
        {
        }
        // ReSharper enable all

        [TestMethod]
        public void ConvertToUserDefinedType()
        {
            var obj = (A)JsonObject.Parse(@"{""S"":""a""}");
            Assert.AreEqual("a", obj.S);
        }

        [TestMethod]
        public void ConvertToEmptyObject()
        {
            var obj = (Empty)JsonObject.Parse(@"{""S"":""a""}");
            Assert.IsInstanceOfType(obj, typeof(Empty));
        }

        [TestMethod]
        public void ConvertEmptyToUserDefinedType()
        {
            var obj = (A)JsonObject.Parse(@"{}");
            Assert.IsInstanceOfType(obj, typeof(A));
        }

        [TestMethod]
        public void ConverToPublicField()
        {
            var obj = (A)JsonObject.Parse(@"{""F"":1}");
            Assert.AreEqual(1, obj.F);
        }

        [TestMethod]
        public void ConvertByDeserializeMethod()
        {
            var array = JsonObject.Parse("[0,1]");

            Assert.AreEqual(1d, array.Deserialize<double[]>()[1]);
        }

        [TestMethod]
        public void ConvertStringToDateTime()
        {
            var obj = (A)JsonObject.Parse(@"{""D"":""2020-10-10""}");
            Assert.AreEqual(new DateTime(2020, 10, 10), obj.D);
        }

        [TestMethod]
        public void ConvertNumberToString()
        {
            var obj = (A)JsonObject.Parse(@"{""S"":1.1}");
            Assert.AreEqual("1.1", obj.S);
        }

        [TestMethod]
        public void EnumerateArrayByIEnumerable()
        {
            const string json = "[0,1]";
            var expected = new[] {0d, 1d};

            IEnumerable array = JsonObject.Parse(json); // must be implicit conversion
            Assert.That.SequenceEqual(expected, array.Cast<double>());
        }

        [TestMethod]
        public void EnumerateArrayByForEach()
        {
            const string json = "[0,1]";
            var expected = new[] {0d, 1d};

            var list = new List<double>();
            foreach (double num in JsonObject.Parse(json))
                list.Add(num);
            Assert.That.SequenceEqual(expected, list);
        }

        [TestMethod]
        public void EnumeratePropertyByIEnumerable()
        {
            const string json = @"{""a"":0,""b"":""c""}";
            var expected = new[] {"a:0", "b:c"};

            IEnumerable obj = JsonObject.Parse(json); // must be implicit conversion
            var list = obj.Cast<KeyValuePair<string, dynamic>>().Select(entry => entry.Key + ":" + entry.Value);
            Assert.That.SequenceEqual(expected, list);
        }

        [TestMethod]
        public void EnumeratePropertyByForEach()
        {
            const string json = @"{""a"":0,""b"":1}";
            var expected = new[] {0d, 1d};

            var list = new List<double>();
            foreach (KeyValuePair<string, dynamic> entry in JsonObject.Parse(json))
                list.Add((double)entry.Value);
            Assert.That.SequenceEqual(expected, list);
        }

        [TestMethod]
        public void ConvertToDictionary()
        {
            var dict = (Dictionary<string, dynamic>)JsonObject.Parse(@"{""a"":0}");
            Assert.AreEqual(0, dict["a"]);
        }

        [TestClass]
        public class ErrorTest
        {
            [TestMethod]
            public void ConvertArrayToPrimitiveError()
            {
                Assert.That.Throws<
#if DynamicJson
                    IndexOutOfRangeException
#else
                    InvalidCastException
#endif
                >(() =>
                {
                    var unused = (bool)JsonObject.Parse("[]");
                });
            }

            [TestMethod]
            public void ConvertObjectToArrayError()
            {
                Assert.That.Throws<
#if DynamicJson
                    MissingMethodException
#else
                    InvalidCastException
#endif
                >(() =>
                {
                    var unused = (double[])JsonObject.Parse("{}");
                });
            }

            [TestMethod]
            public void ConvertNullError()
            {
                Assert.That.Throws<InvalidCastException>(() =>
                {
                    var unused = (bool[])JsonObject.Parse("[null]");
                });

                Assert.That.Throws<InvalidCastException>(() =>
                {
                    var unused = (double[])JsonObject.Parse("[null]");
                });
            }


            [TestMethod]
            public void ConvertToDbNullError()
            {
#if DynamicJson
                Assert.That.Throws<MissingMethodException>(() =>
                {
                    var unused = (DBNull)JsonObject.Parse("{}");
                });
#endif
            }

            [TestMethod]
            public void ConvertArrayToNestedArrayError()
            {
                Assert.That.Throws<InvalidCastException>(() =>
                {
                    var unused = (double[][])JsonObject.Parse("[0,1]");
                });
            }

            [TestMethod]
            public void ConvertPrimitiveToTypedIEnumerableError()
            {
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    IEnumerable<double> unused = JsonObject.Parse("0");
                });
            }

            [TestMethod]
            public void ConvertPrimitiveToIEnumerableError()
            {
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    IEnumerable unused = JsonObject.Parse("0");
                });
            }
        }
    }
}