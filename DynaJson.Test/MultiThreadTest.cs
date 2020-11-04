using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using Benchmark;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynaJson.Test
{
    [TestClass]
    public class MultiThreadTest
    {
        private readonly ConcurrentQueue<object> _results = new ConcurrentQueue<object>();
        private readonly Barrier _barrier = new Barrier(TargetObject.Configs.Length);

        private Func<object, object> MakeTypeCaster(Type type)
        {
            var method = GetType().GetMethod("Caster", BindingFlags.NonPublic | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            return (Func<object, object>)method.MakeGenericMethod(type)
                .CreateDelegate(typeof(Func<object, object>), null);
        }

        // ReSharper disable once UnusedMember.Local
        private object Caster<T>(dynamic o)
        {
            return (T)o;
        }

        private void Serialize(object target)
        {
            var obj = TargetObject.GetConfig((string)target).Target;
            _barrier.SignalAndWait();
            _results.Enqueue(JsonObject.Serialize(obj));
        }

        private void Deserialize(object target)
        {
            var obj = TargetObject.GetConfig((string)target).Target;
            var caster = MakeTypeCaster(obj.GetType());
            var json = JsonObject.Serialize(obj);
            _barrier.SignalAndWait();
            var jsonObj = JsonObject.Parse(json);
            _barrier.SignalAndWait();
            _results.Enqueue(caster(jsonObj));
        }

        [TestMethod]
        public void SerializeTest()
        {
            var threads = TargetObject.Configs.Select(_ => new Thread(Serialize)).ToArray();
            foreach (var (thread, cfg) in threads.Zip(TargetObject.Configs))
                thread.Start(cfg.Name);
            foreach (var t in threads)
                t.Join();
            Assert.AreEqual(TargetObject.Configs.Length, _results.Count);
        }

        [TestMethod]
        public void DeserializeTest()
        {
            var threads = TargetObject.Configs.Select(_ => new Thread(Deserialize)).ToArray();
            foreach (var (thread, cfg) in threads.Zip(TargetObject.Configs))
                thread.Start(cfg.Name);
            foreach (var t in threads)
                t.Join();
            Assert.AreEqual(TargetObject.Configs.Length, _results.Count);
        }
    }
}