using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Benchmark
{
    public static class StopWatch
    {
        public static int RepeatCount { get; set; }

        [Flags]
        public enum BenchmarkType
        {
            Dynamic = 1,
            Static = 2
        }

        public static BenchmarkType Type { private get; set; }

        public static IEnumerable<string> Methods { private get; set; }

        public static void Run()
        {
            if (Type == BenchmarkType.Dynamic)
                Dynamic.RunBenchmark();
            if (Type == BenchmarkType.Static)
                Static.RunBenchmark();
        }

        private static class Dynamic
        {
            private const int DefaultRepeatCount = 1000;

            public static void RunBenchmark()
            {
                if (RepeatCount == 0)
                    RepeatCount = DefaultRepeatCount;
                var sw = new Stopwatch();

                foreach (var set in DataSet.DataSets)
                {
                    if (Methods.Contains("parse") || Methods.Contains("all"))
                    {
                        Console.Out.WriteLine("Parse: " + set.Name);
                        foreach (var library in Library.Libraries)
                        {
                            GC.Collect(2, GCCollectionMode.Forced, true);
                            sw.Restart();
                            for (var i = 0; i < RepeatCount; i++)
                            {
                                var unused = library.ParseDynamic(set.JsonString);
                            }
                            sw.Stop();
                            Console.Out.WriteLine(
                                $"{(library.Name + ":").PadRight(20)}{sw.Elapsed / RepeatCount}");
                        }
                    }
                    if (Methods.Contains("serialize") || Methods.Contains("all"))
                    {
                        Console.Out.WriteLine("Serialize: " + set.Name);
                        foreach (var library in Library.Libraries)
                        {
                            var obj = library.ParseDynamic(set.JsonString);
                            GC.Collect(2, GCCollectionMode.Forced, true);
                            sw.Restart();
                            for (var i = 0; i < RepeatCount; i++)
                            {
                                var unused = library.SerializeDynamic(obj);
                            }
                            sw.Stop();
                            Console.Out.WriteLine($"{(library.Name + ":").PadRight(20)}{sw.Elapsed / RepeatCount}");
                        }
                    }
                }
            }
        }

        private static class Static
        {
            private const int DefaultRepeatCount = 1000000;

            public static void RunBenchmark()
            {
                if (RepeatCount == 0)
                    RepeatCount = DefaultRepeatCount;
                var sw = new Stopwatch();

                foreach (var targetObject in TargetObject.TargetObjects)
                {
                    if (Methods.Contains("serialize") || Methods.Contains("all"))
                    {
                        Console.Out.WriteLine("Serialize: " + targetObject.Name);
                        foreach (var library in Library.Libraries)
                        {
                            var method = library.CreateSerializeDelegate(targetObject.Target);
                            GC.Collect(2, GCCollectionMode.Forced, true);
                            sw.Restart();
                            for (var i = 0; i < RepeatCount; i++)
                            {
                                var unused = method();
                            }
                            sw.Stop();
                            Console.Out.WriteLine(
                                $"{(library.Name + ":").PadRight(20)}{sw.Elapsed / RepeatCount}");
                        }
                    }
                    if (Methods.Contains("deserialize") || Methods.Contains("all"))
                    {
                        Console.Out.WriteLine("Deserialize: " + targetObject.Name);
                        foreach (var library in Library.Libraries)
                        {
                            var jsonString = library.CreateSerializeDelegate(targetObject.Target)();
                            var method = library.CreateDeserializeDelegate(targetObject.Target.GetType(), jsonString);
                            GC.Collect(2, GCCollectionMode.Forced, true);
                            sw.Restart();
                            for (var i = 0; i < RepeatCount; i++)
                            {
                                var unused = method();
                            }
                            sw.Stop();
                            Console.Out.WriteLine($"{(library.Name + ":").PadRight(20)}{sw.Elapsed / RepeatCount}");
                        }
                    }
                }
            }
        }
    }
}