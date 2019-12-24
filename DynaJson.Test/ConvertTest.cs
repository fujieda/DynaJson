using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
#if DynamicJson
    using DynaJson = Codeplex.Data.DynamicJson;
#endif

    [TestClass]
    public class ConvertTest
    {
        [TestMethod]
        public void ConvertToArray()
        {
            var bArray = (bool[])DynaJson.Parse("[false]");
            Assert.IsFalse(bArray[0]);

            var sArray = (string[])DynaJson.Parse(@"[""a""]");
            Assert.AreEqual("a", sArray[0]);
        }

        [TestMethod]
        public void ConvertToList()
        {
            var list = (List<string>)DynaJson.Parse(@"[""a""]");
            Assert.AreEqual("a", list[0]);
        }

        [TestMethod]
        public void ConvertBetweenConvertibleTypes()
        {
            var bArray = (bool[])DynaJson.Parse("[0]");
            Assert.IsFalse(bArray[0]);

            var sArray = (string[])DynaJson.Parse("[0]");
            Assert.AreEqual("0", sArray[0]);

            var dArray = (double[])DynaJson.Parse(@"[""0""]");
            Assert.AreEqual(0, dArray[0]);
        }

        [TestMethod]
        public void ConvertToArrayOfInt()
        {
            var array = (int[])DynaJson.Parse("[0]");
            Assert.AreEqual(0, array[0]);
        }

        [TestMethod]
        public void ConvertToListOfInt()
        {
            var list = (List<int>)DynaJson.Parse("[0]");
            Assert.AreEqual(0, list[0]);
        }

        [TestMethod]
        public void ConvertMixedArrayToArray()
        {
            var array = (double[])DynaJson.Parse("[0,true]");
            Assert.AreEqual(1d, array[1]);
        }

        [TestMethod]
        public void ConvertToArrayOfObject()
        {
            var array = (dynamic[])DynaJson.Parse(@"[{""a"":0},{""a"":1}]");
            Assert.AreEqual(1d, array[1].a);
        }

        [TestMethod]
        public void ConvertToNestedArray()
        {
            var array = (double[][])DynaJson.Parse("[[0,1],[2,3]]");
            Assert.AreEqual(3d, array[1][1]);
        }

        [TestMethod]
        public void ConvertNull()
        {
            var objArray = (object[])DynaJson.Parse("[null]");
            Assert.AreEqual(null, objArray[0]);

            var strArray = (string[])DynaJson.Parse("[null]");
            Assert.AreEqual(null, strArray[0]);

            var nested = (object[][])DynaJson.Parse("[null]");
            Assert.AreEqual(null, nested[0]);
        }

        // 
        // ReSharper disable all
        private class A
        {
            public string S { get; set; }
        }

        private class Empty
        {
        }
        // ReSharper enable all

        [TestMethod]
        public void ConvertToUserDefinedType()
        {
            var obj = (A)DynaJson.Parse(@"{""S"":""a""}");
            Assert.AreEqual("a", obj.S);
        }

        [TestMethod]
        public void ConvertToEmptyObject()
        {
            var obj = (Empty)DynaJson.Parse(@"{""S"":""a""}");
            Assert.IsInstanceOfType(obj, typeof(Empty));
        }

        [TestMethod]
        public void ConvertEmptyToUserDefinedType()
        {
            var obj = (A)DynaJson.Parse(@"{}");
            Assert.IsInstanceOfType(obj, typeof(A));
        }

        [TestMethod]
        public void ConvertByDeserializeMethod()
        {
            var array = DynaJson.Parse("[0,1]");

            Assert.AreEqual(1d, array.Deserialize<double[]>()[1]);
        }

        [TestMethod]
        public void EnumerateArrayByIEnumerable()
        {
            const string json = "[0,1]";
            var expected = new[] {0d, 1d};

            IEnumerable array = DynaJson.Parse(json); // must be implicit conversion
            Assert.That.SequenceEqual(expected, array.Cast<double>());
        }

        [TestMethod]
        public void EnumerateArrayByForEach()
        {
            const string json = "[0,1]";
            var expected = new[] {0d, 1d};

            var list = new List<double>();
            foreach (double num in DynaJson.Parse(json))
                list.Add(num);
            Assert.That.SequenceEqual(expected, list);
        }

        [TestMethod]
        public void EnumeratePropertyByIEnumerable()
        {
            const string json = @"{""a"":0,""b"":""c""}";
            var expected = new[] {"a:0", "b:c"};

            IEnumerable obj = DynaJson.Parse(json); // must be implicit conversion
            var list = obj.Cast<KeyValuePair<string, dynamic>>().Select(entry => entry.Key + ":" + entry.Value);
            Assert.That.SequenceEqual(expected, list);
        }

        [TestMethod]
        public void EnumeratePropertyByForEach()
        {
            const string json = @"{""a"":0,""b"":1}";
            var expected = new[] {0d, 1d};

            var list = new List<double>();
            foreach (KeyValuePair<string, dynamic> entry in DynaJson.Parse(json))
                list.Add((double)entry.Value);
            Assert.That.SequenceEqual(expected, list);
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
                    var unused = (bool)DynaJson.Parse("[]");
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
                >(() =>
#endif
                {
                    var unused = (double[])DynaJson.Parse("{}");
                });
            }

            [TestMethod]
            public void ConvertNullError()
            {
                Assert.That.Throws<InvalidCastException>(() =>
                {
                    var unused = (bool[])DynaJson.Parse("[null]");
                });

                Assert.That.Throws<InvalidCastException>(() =>
                {
                    var unused = (double[])DynaJson.Parse("[null]");
                });
            }


            [TestMethod]
            public void ConvertToDbNullError()
            {
#if DynamicJson
                Assert.That.Throws<MissingMethodException>(() =>
                {
                    var unused = (DBNull)DynaJson.Parse("{}");
                });
#endif
            }

            [TestMethod]
            public void ConvertArrayToNestedArrayError()
            {
                Assert.That.Throws<InvalidCastException>(() =>
                {
                    var unused = (double[][])DynaJson.Parse("[0,1]");
                });
            }

            [TestMethod]
            public void ConvertPrimitiveToTypedIEnumerableError()
            {
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    IEnumerable<double> unused = DynaJson.Parse("0");
                });
            }

            [TestMethod]
            public void ConvertPrimitiveToIEnumerableError()
            {
                Assert.That.Throws<RuntimeBinderException>(() =>
                {
                    IEnumerable unused = DynaJson.Parse("0");
                });
            }
        }
    }
}