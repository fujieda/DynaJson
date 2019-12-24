using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benchmark
{
    public static class DataSet
    {
        public class DataSetConfig
        {
            public string Name;
            public string JsonString;

            public override string ToString()
            {
                return Name;
            }
        }

        private static string _dataDir;

        private static string ReadFile(string file)
        {
            if (_dataDir == null)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var index = baseDir.IndexOf(@"Benchmark", StringComparison.InvariantCulture);
                _dataDir = Path.Combine(baseDir.Substring(0, index + @"Benchmark".Length), "Data");
            }
            return File.ReadAllText(Path.Combine(_dataDir, file));
        }

        public static IEnumerable<string> Filter { get; set; } = new[] {"all"};

        public static IEnumerable<DataSetConfig> DataSets => InitialDataSets
            .Where(target => Filter.Contains("all") || Filter.Contains(target.Name.ToLower()));

        private static readonly DataSetConfig[] InitialDataSets =
        {
            new DataSetConfig
            {
                Name = "currency.json",
                JsonString = ReadFile("currency.json")
            },
            new DataSetConfig
            {
                Name = "geojson.json",
                JsonString = ReadFile("geojson.json")
            },
            new DataSetConfig
            {
                Name = "github.json",
                JsonString = ReadFile("github.json")
            },
            new DataSetConfig
            {
                Name = "twitter.json",
                JsonString = ReadFile("twitter.json")
            },
            new DataSetConfig
            {
                Name = "riot-games.json",
                JsonString = ReadFile("riot-games.json")
            },
            new DataSetConfig
            {
                Name = "citm_catalog.json",
                JsonString = ReadFile("citm_catalog.json")
            }
        };
    }
}