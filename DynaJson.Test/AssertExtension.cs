using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
    public static class AssertExtension
    {
        // ReSharper disable once UnusedParameter.Global
        public static void Throws<T>(this Assert unused, Action action, string expectedMessage = null) where T : Exception
        {
            try
            {
                action.Invoke();
            }
            catch (T e)
            {
#if !DynamicJson
                if (expectedMessage != null)
                    Assert.AreEqual(expectedMessage, e.Message);
#endif
                return;
            }
            Assert.Fail($"Not throws exception {typeof(T)} ");
        }

        public static void SequenceEqual<T>(this Assert unused, IEnumerable<T> a, IEnumerable<T> b)
        {
            var aa = a.ToArray();
            var bb = b.ToArray();
            if (aa.Length != bb.Length)
                Assert.Fail($"Expected length:<{aa.Length}>. Actual:<{bb.Length}>");
            for (var i = 0; i < aa.Length; i++)
            {
                if (aa[i].Equals(bb[i]))
                    continue;
                Assert.Fail($"Expected at {i} <{aa[i]}>. Actual:<{bb[i]}>");
            }
        }
    }
}