using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;

namespace Benchmark
{
    public class BenchmarkConfig : ManualConfig
    {
        public static IEnumerable<string> Filter { private get; set; }

        public BenchmarkConfig()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(new NameFilter(name => Filter.Contains("all") || Filter.Contains(name.ToLower())));
        }
    }

    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public static class BenchmarkDotNet
    {
        [Config(typeof(BenchmarkConfig))]
        public class Dynamic
        {
            public IEnumerable<DataSet.DataSetConfig> DataSets => Benchmark.DataSet.DataSets;

            public IEnumerable<Library.LibraryConfig> Libraries => Benchmark.Library.Libraries;

            [ParamsSource(nameof(Libraries))]
            public Library.LibraryConfig Library { get; set; }

            [ParamsSource(nameof(DataSets))]
            public DataSet.DataSetConfig DataSet { get; set; }

            [Benchmark]
            public void Parse()
            {
                Library.ParseDynamic(DataSet.JsonString);
            }

            private object _object;

            [GlobalSetup(Target = nameof(Serialize))]
            public void SetObject()
            {
                _object = Library.ParseDynamic(DataSet.JsonString);
            }

            [Benchmark]
            public void Serialize()
            {
                Library.SerializeDynamic(_object);
            }
        }

        [Config(typeof(BenchmarkConfig))]
        public class Static
        {
            public IEnumerable<TargetObjectConfig> TargetObjects => Benchmark.TargetObject.TargetObjects;

            public IEnumerable<Library.LibraryConfig> Libraries => Benchmark.Library.Libraries;

            [ParamsSource(nameof(Libraries))]
            public Library.LibraryConfig Library { get; set; }

            [ParamsSource(nameof(TargetObjects))]
            public TargetObjectConfig TargetObject { get; set; }

            private Func<string> _serializer;
            private Func<object> _deserializer;

            [GlobalSetup(Target = nameof(Serialize))]
            public void SetupSerialize()
            {
                _serializer = Library.CreateSerializeDelegate(TargetObject.Target);
            }

            [GlobalSetup(Target = nameof(Deserialize))]
            public void SetupDeserialize()
            {
                var jsonString = Library.CreateSerializeDelegate(TargetObject.Target)();
                _deserializer = Library.CreateDeserializeDelegate(TargetObject.Target.GetType(), jsonString);
            }

            [Benchmark]
            public void Serialize()
            {
                var unused = _serializer();
            }

            [Benchmark]
            public void Deserialize()
            {
                var unused = _deserializer();
            }
        }
    }
}