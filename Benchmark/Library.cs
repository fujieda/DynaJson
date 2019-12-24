using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Benchmark
{
    public static class Library
    {
        public class LibraryConfig
        {
            public string Name;
            public Func<string, object> ParseDynamic;
            public Func<object, string> SerializeDynamic;
            public MethodInfo Serialize;
            public MethodInfo Deserialize;

            public Func<string> CreateSerializeDelegate(object target)
            {
                return (Func<string>)Serialize.MakeGenericMethod(target.GetType())
                    .CreateDelegate(typeof(Func<string>), target);
            }

            public Func<object> CreateDeserializeDelegate(Type target, string json)
            {
                return (Func<object>)Deserialize.MakeGenericMethod(target)
                    .CreateDelegate(typeof(Func<object>), json);
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private static HashSet<string> _filter = new HashSet<string>(new[] {"all"});

        public static IEnumerable<string> Filter
        {
            set { _filter = new HashSet<string>(value.Select(name => name.ToLower())); }
        }

        public static IEnumerable<LibraryConfig> Libraries => new[]
        {
            new LibraryConfig
            {
                Name = "DynaJson",
                ParseDynamic = DynaJson.DynaJson.Parse,
                SerializeDynamic = obj => obj.ToString(),
                Serialize = GetMethod("DynaJsonSerialize"),
                Deserialize = GetMethod("DynaJsonDeserialize")
            },
            new LibraryConfig
            {
                Name = "Utf8Json",
                ParseDynamic = Utf8Json.JsonSerializer.Deserialize<dynamic>,
                SerializeDynamic = Utf8Json.JsonSerializer.ToJsonString,
                Serialize = GetMethod("Utf8JsonSerialize"),
                Deserialize = GetMethod("Utf8JsonDeserialize")
            },
            new LibraryConfig
            {
                Name = "Jil",
                ParseDynamic = json => Jil.JSON.DeserializeDynamic(json),
                SerializeDynamic = obj => Jil.JSON.SerializeDynamic(obj),
                Serialize = GetMethod("JilSerialize"),
                Deserialize = GetMethod("JilDeserialize")
            },
            new LibraryConfig
            {
                Name = "Newtonsoft.Json",
                ParseDynamic = Newtonsoft.Json.Linq.JToken.Parse,
                SerializeDynamic = obj => ((dynamic)obj).ToString(Newtonsoft.Json.Formatting.None),
                Serialize = GetMethod("NewtonsoftSerialize"),
                Deserialize = GetMethod("NewtonsoftDeserialize")
            },
            new LibraryConfig
            {
                Name = "DynamicJson",
                ParseDynamic = Codeplex.Data.DynamicJson.Parse,
                SerializeDynamic = obj => obj.ToString(),
                Serialize = GetMethod("DynamicJsonSerialize"),
                Deserialize = GetMethod("DynamicJsonDeserialize")
            }
        }.Where(lib => _filter.Contains("all") || _filter.Contains(lib.Name.ToLower()));

        private static MethodInfo GetMethod(string name)
        {
            return typeof(Library).GetMethod(name);
        }

        // ReSharper disable UnusedMember.Global
        public static string DynaJsonSerialize<T>(T obj)
        {
            return DynaJson.DynaJson.Serialize(obj);
        }

        public static T DynaJsonDeserialize<T>(string json)
        {
            return (T)DynaJson.DynaJson.Parse(json);
        }

        public static string Utf8JsonSerialize<T>(T obj)
        {
            return Utf8Json.JsonSerializer.ToJsonString(obj);
        }

        public static T Utf8JsonDeserialize<T>(string json)
        {
            return Utf8Json.JsonSerializer.Deserialize<T>(json);
        }

        public static string JilSerialize<T>(T obj)
        {
            return Jil.JSON.Serialize(obj);
        }

        public static T JilDeserialize<T>(string json)
        {
            return Jil.JSON.Deserialize<T>(json);
        }

        public static string NewtonsoftSerialize<T>(T obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public static T NewtonsoftDeserialize<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        public static string DynamicJsonSerialize<T>(T obj)
        {
            return Codeplex.Data.DynamicJson.Serialize(obj);
        }

        public static T DynamicJsonDeserialize<T>(string json)
        {
            return (T)Codeplex.Data.DynamicJson.Parse(json);
        }
    }
}