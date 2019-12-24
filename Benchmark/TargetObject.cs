using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark
{
    public static class TargetObject
    {
        public static IEnumerable<string> Filter { get; set; } = new[] {"all"};

        public static IEnumerable<TargetObjectConfig> TargetObjects => InitialTargetObjects
            .Where(target => Filter.Contains("all") || Filter.Contains(target.Name.ToLower()));

        private static readonly TargetObjectConfig[] InitialTargetObjects =
        {
            new TargetObjectConfig
            {
                Name = "Simple",
                Target = SimpleObject.Create()
            },
            new TargetObjectConfig
            {
                Name = "Array",
                Target = SimpleObject.CreateArray()
            },
            new TargetObjectConfig
            {
                Name = "List",
                Target = SimpleObject.CreateList()
            },
            new TargetObjectConfig
            {
                Name = "Nested",
                Target = SimpleObject.CreateNested()
            }
        };
    }

    public class TargetObjectConfig
    {
        public string Name;
        public object Target;

        public override string ToString()
        {
            return Name;
        }
    }

    public static class SimpleObject
    {
        private static readonly Random Random = new Random(0);

        public static Simple Create()
        {
            return new Simple
            {
                A = (int)Random.NextDouble(),
                B = Random.NextDouble(),
                C = RandomString()
            };
        }

        public static Simple[] CreateArray()
        {
            return Enumerable.Range(0, 10).Select(_ => Create()).ToArray();
        }

        public static List<Simple> CreateList()
        {
            return Enumerable.Range(0, 10).Select(_ => Create()).ToList();
        }

        public static Nested CreateNested()
        {
            return new Nested
            {
                A = Create(),
                B = Create(),
                C = new Nested
                {
                    A = Create(),
                    B = Create()
                }
            };
        }

        private static readonly char[] Printable =
            Enumerable.Range(0x20, 0xff).Select(num => (char)num).Where(num => !char.IsControl(num)).ToArray();

        private static string RandomString()
        {
            return new string(Enumerable.Range(1, 16).Select(_ => Printable[Random.Next(Printable.Length)]).ToArray());
        }

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedField.Global

        public class Simple
        {
            public int A { get; set; }
            public double B { get; set; }
            public string C { get; set; }
        }

        public class Nested
        {
            public Simple A { get; set; }
            public Simple B { get; set; }
            public Nested C { get; set; }
        }
    }
}