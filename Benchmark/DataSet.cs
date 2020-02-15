using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benchmark
{
    public static class DataSet
    {
        private static string _dataDir;

        public static string ReadFile(string file)
        {
            if (_dataDir == null)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var index = baseDir.IndexOf(@"Benchmark", StringComparison.InvariantCulture);
                _dataDir = Path.Combine(baseDir.Substring(0, index + @"Benchmark".Length), "Data");
            }
            return File.ReadAllText(Path.Combine(_dataDir, file));
        }

        public static IEnumerable<string> Filter { get; set; }

        public static IEnumerable<string> JsonNames =>
            AllNames.Where(name => Filter.Contains("all") || Filter.Contains(name));

        private static readonly string[] AllNames =
        {
            "currency.json", "geojson.json", "github.json", "twitter.json", "riot-games.json",
            "citm_catalog.json"
        };
    }
}