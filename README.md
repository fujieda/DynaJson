# DynaJson

A fast and compact drop-in replacement of DynamicJson

DynaJson is designed as a drop-in replacement of [DynamicJson](https://github.com/neuecc/DynamicJson).  You can intuitively manipulate JSON data through the dynamic type in the same way as DynamicJson. It is written from scratch and licensed under the MIT license instead of Ms-PL of DynamicJson.

DynaJson can parse and serialize four times faster than DynamicJson. It has no recursive call to process deeply nested JSON data and no extra dependency except for Microsoft.CSharp for .Net Standard to reduce the package size of your applications.

## Usage

DynaJson is available on NuGet for .NET Standard 2.0 and .NET Framework 4.5.1.

```
PM> Install-Package DynaJson
```

The following examples are borrowed from DynamicJson.

### Parse

```csharp
var json = JsonObject.Parse(@"{
    ""foo"": ""json"",
    ""bar"": [100,200],
    ""nest"": {""foobar"": true}
}");
```

### Access Objects

```csharp
// Accessing object properties
var a1 = json.foo; // "json"
var a2 = json.nest.foobar; // true
// The same as above
var a3 = json["nest"]["foobar"]; // bracket notation

// Check the specified property exists
var b1 = json.IsDefined("foo"); // true
var b2 = json.IsDefined("foooo"); // false
// object.name() works as object.IsDefined("name")
var b3 = json.foo(); // true
var b4 = json.foooo(); // false
```

### Access Arrays

```csharp
// Accessing array elements
var a4 = json.bar[0]; // 100.0

// Check array boundary
var b5 = json.bar.IsDefined(1); // true
var b6 = json.bar.IsDefined(2); // false for out of bounds

// Get array length (DynaJson only)
var len1 = json.bar.Length; // 2
// The same as above
var len2 = json.bar.Count; // 2
```

### Convert to C# types

```csharp
// a JSON objects to a C# object
public class FooBar
{
    public string foo { get; set; }
    public int bar;
}
var jsonObject = JsonObject.Parse(@"{""foo"":""json"",""bar"":100}");
var foobar = (FooBar)objectJson; // FooBar
var c1 = foobar.bar; // 100
// You can use the Deserialize method instead of type casting
// var foobar = objectJson.Deserialize<FooBar>();

// to a C# dictionary
var dict1 = (Dictionary<string, dynamic>)jsonObject;
var c2 = dict1["bar"]; // 100

// a JSON array to a C# array
var jsonArray = JsonObject.Parse("[1,2,3]");
var array = (int[])jsonArray; // int[]
var sum1 = array.Sum(); // 6

// to a C# list
var list = (List<int>)jsonArray;
var sum2 = list.Sum(); // 6
```

### Serialize

```csharp
var foobar = new[]
{
    new FooBar {foo = "fooooo!", bar = 1000},
    new FooBar {foo = "orz", bar = 10}
};
var json1 = JsonObject.Serialize(foobar); // [{"foo":"fooooo!","bar":1000},{"foo":"orz","bar":10}]

// Serialize a dictionary
var dict = new Dictionary<string, int>
{
    {"aaa", 1},
    {"bbb", 2}
};
var json2 = JsonObject.Serialize(dict); // {"aaa":1,"bbb":2}

// Serialize an object created dynamically
dynamic jsonObject = new JsonObject();
jsonObject.str = "aaa";
jsonObject.obj = new {foo = "bar"};
var json3 = jsonObject.ToString(); // {"str":"aaa","obj":{"foo":"bar"}}
```

### Modify JSON objects

```csharp
dynamic jsonObject = new JsonObject(); // or JsonObject.Parse("{}");
// Add properties
jsonObject.str = "aaa"; // string
jsonObject.obj = new {foo = "bar"}; // an object
jsonObject.arr = new[] {"aaa", "bbb"}; // an array
// Assign a new value
jsonObject.str = "bbb";
// Delete a specified property
var d1 = jsonObject.Delete("str"); // true for success
var d2 = jsonObject.Delete("str"); // false for failure
// object("name") works as object.Delete("name")
var d3 = jsonObject("obj"); // true
```

### Modify JSON Arrays

```csharp
dynamic jsonArray = new JsonObject(new[]{"aaa", "bbb"}); // or JsonObject.Parse(@"[""aaa"",""bbb""]");
// Assign a new value
jsonArray[0] = "ccc";
// Delete elements
var e1 = jsonArray.Delete(0); // true for success
var e2 = jsonArray[0]; // "bbb"
// array(index) works as array.Delete(index)
var e3 = jsonArray(0); // true
var len = jsonArray.Length; // 0 (DynaJson only)
```

### Enumerate

```csharp
var arrayJson = JsonObject.Parse("[1,2,3]")
var sum = 0;
foreach (int item in arrayJson)
    sum += item;
// sum = 6

var objectJson = JsonObject.Parse(@"{""foo"":""json"",""bar"":100}");
var list = new List<string>();
foreach (KeyValuePair<string, dynamic> item in objectJson)
    list.Add(item.Key + ":" + item.Value);
// list = ["foo:json","bar:100"]
```

## Incompatibility with DynamicJson

DynaJson supports `Count` and `Length` methods to get the length of each array. Both return the same value.

It does not accept incomplete object notations such as `{"a":1,` and `{"a":1` while DynamicJson accepts. This strictness allows you to detect incomplete transfer.

In some cases, it throws exceptions different from what DynamicJson throws.

```csharp
// InvalidCastException instead of IndexOutOfRangeException
var e1 = (bool)JsonObject.Parse("[]");
// InvalidCastException instead of MissingMethodException
var e2 = (double[])JsonObject.Parse("{}");
// RuntimeBinderException instead of FormatException
var e3 = JsonObject.Parse("[true]").a;
```

## Benchmark for large JSON strings

The primary usage of DynaJson (and DynamicJson) is processing large JSON strings. So the first benchmark evaluates the performance of it.

### Target libraries

The following libraries support the dynamic type to access parsed JSON data. Utf8Json, however, does not provide dynamic property access like `json.foo`.

Name                      |Version|Size (bytes)|
--------------------------|-------|-----------:|
DynaJson                  |2.1    |35,238      |
[Utf8Json][Utf8Json]      |1.3.7  |237,568     |
[Jil][Jil]                |2.17.0 |755,712     |
[Newtonsoft.Json][Nt.Json]|12.0.3 |693,680     |
[DynamicJson][DynamicJson]|1.2.0  |15,872      |

<sub>The size of Jil includes indispensable Sigil's</sub>

[Utf8Json]: https://github.com/neuecc/Utf8Json
[Jil]: https://github.com/kevin-montrose/Jil
[Nt.Json]: https://www.newtonsoft.com/json
[DynamicJson]: https://github.com/neuecc/DynamicJson

### Target JSON files

- currency.json (179 KB)

    The result of [Foreign exchange rates API with currency conversion](https://exchangeratesapi.io/) against [the request](https://api.exchangeratesapi.io/history?start_at=2017-08-01&end_at=2018-12-31) for currency data from 2017-08-01 until 2018-12-31.

- geojson.json (178 KB)

    A GeoJSON on [Bicycle and Pedestrian On Road Bike Facilities GIS Data](https://catalog.data.gov/dataset/bicycle-and-pedestrian-facilities-gis-data) consisting mainly of coordinates (arrays with two numbers).

- riot-games.json (185 KB)

    Seven sets of match data in [League of Legends](https://na.leagueoflegends.com/) of an online battle game. It is part of the seed data for [the Riot Games API](https://developer.riotgames.com/). It has a complicated data structure.

- twitter.json (174 KB)

    The Twitter search API result against the query `一` (the character of "one" in Japanese and Chinese). It is a raw data differently from [twitter.json](https://github.com/miloyip/nativejson-benchmark/blob/master/data/twitter.json) in [Native JSON Benchmark](https://github.com/miloyip/nativejson-benchmark/).

- github.json (177 KB)

    The result of [the GitHub API to search repositories](https://developer.github.com/v3/search/#search-repositories) against the query `topic:ruby+topic:rails`. It is pretty printed.

- citm_catalog.json (1.7 MB)

   The JSON file used most widely to benchmark JSON parsers. We borrow it from [Native JSON Benchmark](https://github.com/miloyip/nativejson-benchmark/). It is pretty printed.

### Results

The benchmark program in [the GitHub repository](https://github.com/fujieda/DynaJson/Benchmark/) generated the following results on Standard D2ds_v4 in Microsoft Azure.

#### Parse the original JSON files

The following result shows the time to parse each JSON file except for citm_catalog.json.

![benchmark-dynamic-parse](https://user-images.githubusercontent.com/345831/100993884-46430d80-3599-11eb-8f04-6bacaf922b33.png)

DynaJson is considerably faster than other parsers.

#### Serialize back

The following shows the time to serialize each result of parsing back to its string representation. It evaluates the throughput of serializing large objects.

![benchmark-dynamic-serialize](https://user-images.githubusercontent.com/345831/100993970-683c9000-3599-11eb-9e82-cfe2648d924c.png)

DynaJson is the fastest partially because it can serialize only the dynamic objects generated by its parser and provides no customization of string representations.

#### Parse and serialize back citm_catalog.json

![benchmark-dynamic-citm](https://user-images.githubusercontent.com/345831/100994066-899d7c00-3599-11eb-9c6e-fd93000bc634.png)

## Benchmark for user-defined types

DynaJson can serialize and deserialize only `JsonObject`. DynaJson, however, has a converter between `JsonObject` and user-defined types. When DynaJson serializes a value, it converts the value to a `JsonObject` first. Although it has an inherent overhead, the performance is comparable to others.

### User-defined types

- Simple class

    ```csharp
    public class Simple
    {
        public int A { get; set; }
        public double B { get; set; }
        public string C { get; set; }
    }
    ```
- Nested class

    ```csharp
    public class Nested
    {
        public Simple A { get; set; }
        public Simple B { get; set; }
        public Nested C { get; set; }
    }
    ```

    The nesting level is one, so `C` of the nested object is null.

- List with ten elements of the simple class

### Results

#### Serialize

![benchmark-static-serialize](https://user-images.githubusercontent.com/345831/101001865-06812380-35a3-11eb-86fa-f20d5a5ac640.png)

#### Deserialize

![benchmark-static-deserialize](https://user-images.githubusercontent.com/345831/101001979-2c0e2d00-35a3-11eb-8c33-031047754788.png)

## Why fast

- Avoid method invocations by inlining aggressively
- Self-implement Dictionary, List, Enumerator to inline these methods
- Pack a JSON value in a 16 bytes struct with NaN boxing
- Use own double-to-string converter to directly write the result into the output buffer (the same as Utf8Json)
- Use runtime code generation to make reflective operations faster
- Cover only a minimum set of features so far
