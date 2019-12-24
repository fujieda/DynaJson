using System.Collections.Generic;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using CommandLine;

namespace Benchmark
{
    public static class Runner
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private class Options
        {
            [Option('b', "benchmark", Required = true, HelpText = "Benchmark framework (stopwatch|dotnet)")]
            public string Benchmark { get; set; }

            [Option('t', "type", Default = "all", HelpText = "Data type (static|dynamic|all)")]
            public string Type { get; set; }

            [Option('m', "method", Separator = ':', Default = new[] {"all"})]
            public IEnumerable<string> Methods { get; set; }

            [Option('j', "job", SetName = "dotnet", Default = "default",
                HelpText = "Job for BenchmarkDotNet (default|dry|short|medium|long)")]
            public string Job { get; set; }

            [Option('c', "count", SetName = "stopwatch")]
            public int Count { get; set; }

            [Option('l', "library", Separator = ':', Default = new[] {"all"})]
            public IEnumerable<string> Libraries { get; set; }

            [Option('d', "data", Separator = ':', Default = new[] {"all"})]
            public IEnumerable<string> DataSets { get; set; }

            [Option('o', "object", Separator = ':', Default = new[] {"all"})]
            public IEnumerable<string> Objects { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        public static void Run(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(opt =>
            {
                Library.Filter = opt.Libraries;
                DataSet.Filter = opt.DataSets;
                TargetObject.Filter = opt.Objects;
                if (opt.Benchmark == "stopwatch")
                {
                    StopWatch.Methods = opt.Methods;
                    StopWatch.RepeatCount = opt.Count;
                    if (opt.Type == "dynamic")
                        StopWatch.Type = StopWatch.BenchmarkType.Dynamic;
                    if (opt.Type == "static")
                        StopWatch.Type = StopWatch.BenchmarkType.Static;
                    StopWatch.Run();
                }
                if (opt.Benchmark == "dotnet")
                {
                    BenchmarkConfig.Filter = opt.Methods;
                    var config = ManualConfig.Create(DefaultConfig.Instance).With(StringToJob(opt.Job));
                    if (opt.Type == "dynamic" || opt.Type == "all")
                        BenchmarkRunner.Run(typeof(BenchmarkDotNet.Dynamic), config);
                    if (opt.Type == "static" || opt.Type == "all")
                        BenchmarkRunner.Run(typeof(BenchmarkDotNet.Static), config);
                }
            });
        }

        private static Job StringToJob(string str)
        {
            var job = Job.Default;
            switch (str)
            {
                case "dry":
                    job = Job.Dry;
                    break;
                case "short":
                    job = Job.ShortRun;
                    break;
                case "medium":
                    job = Job.MediumRun;
                    break;
                case "long":
                    job = Job.LongRun;
                    break;
            }
            return job;
        }
    }
}