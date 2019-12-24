using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
    [TestClass]
    public class JsonDictionaryTest
    {
        private readonly JsonDictionary _dict = new JsonDictionary();
        private InternalObject _value;
        private readonly Random _random = new Random(0);

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        [DataRow(8)]
        [DataRow(9)]
        [DataRow(10)]
        [DataRow(11)]
        [DataRow(12)]
        public void Add(int count)
        {
            var keys = RandomKeys().Take(count).ToArray();
            var value = new InternalObject();
            for (var i = 0; i < count; i++)
            {
                value.Number = i;
                _dict.Add(keys[i], value);
            }
            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(_dict.TryGetValue(keys[i], out var result), i.ToString());
                Assert.AreEqual(i, result.Number, keys[i]);
            }
        }

        [TestMethod]
        public void AddNull()
        {
            Assert.That.Throws<ArgumentNullException>(() =>
            {
                _dict.Add(null, _value);
            });
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        [DataRow(8)]
        [DataRow(9)]
        [DataRow(10)]
        [DataRow(11)]
        [DataRow(12)]
        public void Remove(int count)
        {
            var keys = RandomKeys().Take(count).ToArray();
            var removes = keys.Where((key, i) => i % 3 == 0).ToArray();
            foreach (var key in keys)
                _dict.Add(key, _value);
            foreach (var key in removes)
                _dict.Remove(key);
            var n = 0;
            foreach (var key in keys)
                Assert.AreEqual(!removes.Contains(key), _dict.ContainsKey(key), n++.ToString());
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        [DataRow(8)]
        [DataRow(9)]
        [DataRow(10)]
        [DataRow(11)]
        [DataRow(12)]
        public void RemoveFail(int count)
        {
            var keys = RandomKeys().Take(count).ToArray();
            var removes = keys.Where((key, i) => i % 3 == 0).ToArray();
            foreach (var key in keys)
                _dict.Add(key, _value);
            var n = 0;
            foreach (var key in removes)
                Assert.IsTrue(_dict.Remove(key), n++.ToString());
            n = 0;
            foreach (var key in removes)
                Assert.IsFalse(_dict.Remove(key), n++.ToString());
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        [DataRow(8)]
        [DataRow(9)]
        [DataRow(10)]
        [DataRow(11)]
        [DataRow(12)]
        public void RemoveAndAdd(int count)
        {
            var half = count / 2 + 1;
            var former = RandomKeys().Take(half).ToArray();
            var latter = RandomKeys().Take(count - half).ToArray();
            var removes = former.Where((key, i) => i % 3 == 0).ToArray();
            foreach (var key in former)
                _dict.Add(key, _value);
            foreach (var key in removes)
                _dict.Remove(key);
            foreach (var key in latter)
                _dict.Add(key, _value);
            var n = 0;
            foreach (var key in former.Concat(latter))
                Assert.AreEqual(!removes.Contains(key), _dict.ContainsKey(key), n++.ToString());
        }

        [TestMethod]
        public void RemoveNull()
        {
            Assert.That.Throws<ArgumentNullException>(() => { _dict.Remove(null); });
        }

        [TestMethod]
        public void AddExistingKey()
        {
            _value.Type = JsonType.False;
            _dict.Add("a", _value);
            _value.Type = JsonType.True;
            _dict.Add("a", _value);
            Assert.AreEqual(JsonType.False, _dict["a"].Type);
        }

        [TestMethod]
        public void AssignValue()
        {
            _value.Type = JsonType.False;
            _dict["a"] = _value;
            _value.Type = JsonType.True;
            _dict["a"] = _value;
            Assert.AreEqual(JsonType.True, _dict["a"].Type);
        }

        [TestMethod]
        public void KeyNotFound()
        {
            Assert.That.Throws<KeyNotFoundException>(() =>
            {
                var unused = _dict["a"];
            });
        }

        [TestMethod]
        public void Enumeration()
        {
            var expected = new List<KeyValuePair<string, InternalObject>>();
            var keys = RandomKeys().Take(6).ToArray();
            var num = 0;
            foreach (var key in keys)
            {
                _value.Number = num++;
                _dict.Add(key, _value);
                expected.Add(new KeyValuePair<string, InternalObject>(key, _value));
            }
            Assert.That.SequenceEqual(expected, _dict.GetEnumerator().GetEnumerable());
        }

        [TestMethod]
        public void RemovedEnumeration()
        {
            var expected = new List<KeyValuePair<string, InternalObject>>();
            var keys = RandomKeys().Take(6).ToArray();
            var removes = keys.Where((_, i) => i % 3 == 0).ToArray();
            var num = 0;
            foreach (var key in keys)
            {
                _value.Number = num++;
                _dict.Add(key, _value);
                if (!removes.Contains(key))
                    expected.Add(new KeyValuePair<string, InternalObject>(key, _value));
            }
            foreach (var key in removes)
            {
                _dict.Remove(key);
            }
            Assert.That.SequenceEqual(expected, _dict.GetEnumerator().GetEnumerable());
        }

        private IEnumerable<string> RandomKeys()
        {
            while (true)
                yield return new string(Enumerable.Range(0, 5).Select(y => (char)_random.Next(0xff)).ToArray());
            // ReSharper disable once IteratorNeverReturns
        }
    }
}