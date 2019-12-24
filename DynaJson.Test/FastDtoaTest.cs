using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
    [TestClass]
    public class FastDtoaTest
    {
        private unsafe string Convert(double d)
        {
            var buffer = stackalloc char[24];
            var len = FastDtoa.Convert(d, buffer);
            return new string(buffer, 0, len);
        }

        [DataTestMethod]
        [DataRow(0d, "0")]
        [DataRow(1d, "1")]
        [DataRow(-1d, "-1")]
        [DataRow(1.5d, "1.5")]
        [DataRow(5e-324, "5e-324")]
        [DataRow(1.7976931348623157e308, "1.7976931348623157e308")]
        [DataRow(4294967272.0, "4294967272")]
        [DataRow(4.1855804968213567e298, "4.185580496821357e298")]
        [DataRow(5.5626846462680035e-309, "5.562684646268003e-309")]
        [DataRow(2147483648.0, "2147483648")]
        [DataRow(3.5844466002796428e298, "3.5844466002796428e298")]
        public void VariousNumber(double d, string str)
        {
            Assert.AreEqual(str, Convert(d));
        }

        [TestMethod]
        public void SmallestNormal64()
        {
            var d = BitConverter.Int64BitsToDouble(0x0010000000000000);
            Assert.AreEqual("2.2250738585072014e-308", Convert(d));
        }

        [TestMethod]
        // ReSharper disable once IdentifierTypo
        public void LargestDenormal64()
        {
            var d = BitConverter.Int64BitsToDouble(0x000FFFFFFFFFFFFF);
            Assert.AreEqual("2.225073858507201e-308", Convert(d));
        }

        [DataTestMethod]
        [DataRow(1.2345678901e20, "123456789010000000000")]
        [DataRow(-1.2345678901e20, "-123456789010000000000")]
        [DataRow(1.2345678901e21, "1.2345678901e21")]
        [DataRow(-1.2345678901e21, "-1.2345678901e21")]
        [DataRow(1.2345678901234567, "1.2345678901234567")]
        [DataRow(-1.2345678901234567, "-1.2345678901234567")]
        [DataRow(1.2345678901e-6, "0.0000012345678901")]
        [DataRow(1.2345678901e-7, "1.2345678901e-7")]
        [DataRow(-1.2345678901e-6, "-0.0000012345678901")]
        [DataRow(-1.2345678901e-7, "-1.2345678901e-7")]
        public void DecimalNotation(double d, string str)
        {
            Assert.AreEqual(str, Convert(d));
        }
    }
}