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
            public IEnumerable<string> JsonNames => DataSet.JsonNames;

            public IEnumerable<string> LibraryNames => Library.LibraryNames;

            [ParamsSource(nameof(LibraryNames))]
            public string LibraryName { get; set; }

            [ParamsSource(nameof(JsonNames))]
            public string JsonName { get; set; }

            private string _string;
            private Func<string, object> _parseDynamic;

            [GlobalSetup(Target = nameof(Parse))]
            public void ParseSetup()
            {
                _string = DataSet.ReadFile(JsonName);
                _parseDynamic = Library.GetConfig(LibraryName).ParseDynamic;
            }

            [Benchmark]
            public void Parse()
            {
                _parseDynamic(_string);
            }

            private object _object;
            private Func<object, string> _serializeDynamic;

            [GlobalSetup(Target = nameof(Serialize))]
            public void SerializeSetup()
            {
                _object = Library.GetConfig(LibraryName).ParseDynamic(DataSet.ReadFile(JsonName));
                _serializeDynamic = Library.GetConfig(LibraryName).SerializeDynamic;
            }

            [Benchmark]
            public void Serialize()
            {
                _serializeDynamic(_object);
            }
        }

        [Config(typeof(BenchmarkConfig))]
        public class Static
        {
            public IEnumerable<string> TargetNames => TargetObject.TargetNames;

            public IEnumerable<string> LibraryNames => Library.LibraryNames;

            [ParamsSource(nameof(LibraryNames))]
            public string LibraryName { get; set; }

            [ParamsSource(nameof(TargetNames))]
            public string TargetName { get; set; }

            private Func<string> _serializer;
            private Func<object> _deserializer;

            [GlobalSetup(Target = nameof(Serialize))]
            public void SerializeSetup()
            {
                var target = TargetObject.GetConfig(TargetName).Target;
                _serializer = Library.GetConfig(LibraryName).CreateSerializeDelegate(target);
            }

            [GlobalSetup(Target = nameof(Deserialize))]
            public void DeserializeSetup()
            {
                var conf = Library.GetConfig(LibraryName);
                var target = TargetObject.GetConfig(TargetName).Target;
                var jsonString = conf.CreateSerializeDelegate(target)();
                _deserializer = conf.CreateDeserializeDelegate(target.GetType(), jsonString);
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