# DynaJson

A fast and compact drop-in replacement of DynamicJson

This library is designed as a drop-in replacement of [DynamicJson](https://github.com/neuecc/DynamicJson).  You can intuitively manipulate JSON data through the dynamic type in the same way as DynamicJson. It is written from scratch and licensed under the MIT license instead of Ms-PL of DynamicJson.

It can parse and serialize five times faster than DynamicJson. It has no recursive call to process deeply nested JSON data. It has no extra dependency except for Microsoft.Csharp for .Net Standard, so you can reduce the package size of your applications.

## Usage

This library is available on NuGet for .NET Standard 2.0 or .NET Framework 4.5.1 or later.

```
PM> Install-Package DynaJson
```

A part of the following examples is borrowed from DynamicJson.

### Parsing

```csharp
var json = JsonObject.Parse(@"{
    ""foo"": ""json"",
    ""bar"": [100,200],
    ""nest"": {""foobar"": true}
}");
```

### Accessing Objects

```csharp
// Accessing object properties
var a1 = json.foo; // "json"
var a2 = json.nest.foobar; // true
// The same as above
var a3 = json["nest"]["foobar"]; // bracket notation

// Assignment
json.foo = "aaa";

// Check the specified property exists
var b1 = json.IsDefined("foo"); // true
var b2 = json.IsDefined("foooo"); // false
// object.name() works as object.IsDefined("name")
var b3 = json.foo(); // true
var b4 = json.foooo(); // false
```

### Accessing Arrays

```csharp
// Accessing array elements
var a4 = json.bar[0]; // 100.0

// Assignment
json.bar[0] = 200;

// Check array boundary
var b5 = json.bar.IsDefined(1); // true
var b6 = json.bar.IsDefined(2); // false for out of bounds

// Get array length (DynaJson only)
var len1 = json.bar.Length; // 2
// The same as above
var len2 = json.bar.Count; // 2
```

### Adding/Deleting Properties

```csharp
// Add properties with C# objects
json.Arr = new[] {"aaa", "bbb"}; // Array
json.Obj = new {aaa = "abc", bbb = 100}; // Object

// Delete the specified property
var d1 = json.Delete("foo"); // true for success
var d2 = json.Delete("foo"); // false for failure
// object("name") works as object.Delete("name")
var d3 = json("bar"); // true
```

### Deleting Array Elements

```csharp
// Deleting elements
var e1 = json.Arr.Delete(0); // true for success
var e2 = json.Arr[0]; // "bbb"
// array(index) works as array.Delete(index)
var e3 = json.Arr(0); // true
var len = json.Arr.Length; // 0 (DynaJson only)
```

### Enumerate

```csharp
var arrayJson = JsonObject.Parse("[1,2,3]")
var sum = 0;
// 6
foreach (int item in arrayJson)
    sum += item;

var objectJson = JsonObject.Parse(@"{""foo"":""json"",""bar"":100}");
var list = new List<string>();
// ["foo:json","bar:100"]
foreach (KeyValuePair<string, dynamic> item in objectJson)
    list.Add(item.Key + ":" + item.Value);
```

### Convert/Deserialize

```csharp
var array1 = arrayJson.Deserialize<int[]>(); // dynamic{int[]}
var array2 = (int[])arrayJson; // int[]
var sum2 = array2.Sum(); // 6
var array3 = (List<int>)arrayJson; // List<int>

public class FooBar
{
    public string foo { get; set; }
    public int bar { get; set; }
}
var foobar1 = objectJson.Deserialize<FooBar>(); // dynamic{FooBar}
var foobar2 = (FooBar)objectJson; // Foobar
FooBar foobar3 = objectJson; // the same above
```

### Create Object and Serialize

```csharp
dynamic newJson1 = new JsonObject();
newJson1.str = "aaa";
newJson1.obj = new {foo = "bar"};
var jsonStr1 = newJson1.ToString(); // {"str":"aaa","obj":{"foo":"bar"}}

dynamic newJson2 = new JsonObject(new {str = "aaa"});
newJson2.obj = new {foo = "bar"};
var jsonStr2 = newJson1.ToString(); // {"str":"aaa","obj":{"foo":"bar"}}
```

### Serialize

```csharp
var obj = new
{
    Name = "Foo",
    Age = 30,
    Address = new
    {
        Country = "Japan",
        City = "Tokyo"
    },
    Like = new[] {"Microsoft", "XBox"}
};
// {"Name":"Foo","Age":30,"Address":{"Country":"Japan","City":"Tokyo"},"Like":["Microsoft","XBox"]}
var json1 = JsonObject.Serialize(obj);

var foobar = new[]
{
    new FooBar {foo = "fooooo!", bar = 1000},
    new FooBar {foo = "orz", bar = 10}
};
// [{"foo":"fooooo!","bar":1000},{"foo":"orz","bar":10}]
var json2 = JsonObject.Serialize(foobar);
```

## Incompatibility with DynamicJson

DynaJson supports `Count` and `Length` methods to get the length of each array. They return the same value.

It doesn't accept incomplete object notations such as `{"a":1,` and `{"a":1` while DynamicJson accepts. This strictness allows you to detect incomplete transfer.

In some cases, it throws different exceptions from what DynamicJson throws.

```csharp
// InvalidCastException instead of IndexOutOfRangeException
var e1 = (bool)JsonObject.Parse("[]");
// InvalidCastException instead of MissingMethodException
var e2 = (double[])JsonObject.Parse("{}");
// RuntimeBinderException instead of FormatException
var e3 = JsonObject.Parse("[true]").a;
```

## Benchmark for large JSON strings

The primary usage of DynaJson (and DynamicJson) is processing large JSON strings. The first benchmark evaluates the performance of it.

### Libraries

The following libraries support the dynamic type to access parsed JSON data. Utf8Json, however, doesn't provide dynamic property access like `json.foo`.

Name                      |Version|Size (bytes)|
--------------------------|-------|-----------:|
DynaJson                  |1.0    |35,238      |
[Utf8Json][Utf8Json]      |1.3.7  |237,568     |
[Jil][Jil]                |2.17.0 |755,712     |
[Newtonsoft.Json][Nt.Json]|12.0.3 |693,680     |
[DynamicJson][DynamicJson]|1.2.0  |15,872      |

<sub>The size of Jil includes indispensable Sigil's</sub>

[Utf8Json]: https://github.com/neuecc/Utf8Json
[Jil]: https://github.com/kevin-montrose/Jil
[Nt.Json]: https://www.newtonsoft.com/json
[DynamicJson]: https://github.com/neuecc/DynamicJson

### Sample JSON data

- currency.json (179 KB)

    It is a simple JSON of the result of [Foreign exchange rates API with currency conversion](https://exchangeratesapi.io/) against [the request](https://api.exchangeratesapi.io/history?start_at=2017-08-01&end_at=2018-12-31) for currency data since 2017-08-01 until 2018-12-31.

- geojson.json (178 KB)

    It is GeoJSON data on [Bicycle and Pedestrian On Road Bike Facilities GIS Data](https://catalog.data.gov/dataset/bicycle-and-pedestrian-facilities-gis-data) consisting mainly of coordinates (arrays with 2 numbers).

- github.json (177 KB)

    The result of [the GitHub API to search repositories](https://developer.github.com/v3/search/#search-repositories) against the query `topic:ruby+topic:rails`. It is pretty printed.

- twitter.json (174 KB)

    It is the result of the Twitter search API against the query `一` (the character of "one" in Japanese and Chinese). It is similar to [twitter.json](https://github.com/miloyip/nativejson-benchmark/blob/master/data/twitter.json) in [Native JSON Benchmark](https://github.com/miloyip/nativejson-benchmark/), but it is raw data with many escaped Unicode characters.

- riot-games.json (185 KB)

    It includes seven sets of match data of [League of Legends](https://na.leagueoflegends.com/) of an online battle game. It is a part of the seed data for [the Riot Games API](https://developer.riotgames.com/). It has a complicated data structure.

- citm_catalog.json (1.7 MB)

    It is the JSON file most widely used for benchmarks of various JSON parsers. We borrow it from [Native JSON Benchmark](https://github.com/miloyip/nativejson-benchmark/). It is pretty printed.

### Results

The benchmark program is in [the GitHub repository](https://github.com/fujieda/DynaJson/Benchmark/).

#### Parse the original data sets

The following result shows the time to parse each data set except for citm_catalog.json.

![image](https://user-images.githubusercontent.com/345831/71397682-91eaf880-2661-11ea-86ca-ccacb5862ca7.png)

#### Serialize back

The following shows the time to serialize each parsed JSON data back to its string. It evaluates the throughput of each serializer.

![image](https://user-images.githubusercontent.com/345831/71397896-23f30100-2662-11ea-9f8c-3ec539b6d47e.png)


DynaJson is the fastest partially because it can serialize only the dynamic objects generated by its parser and provides no customization of string representations.

#### Parse and serialize back citm_catalog.json

![image](https://user-images.githubusercontent.com/345831/71398482-e5f6dc80-2663-11ea-9bd1-63bef9e84046.png)

## Benchmark for user-defined types

DynaJson can serialize and deserialize only its own dynamic objects. It can, however, convert between them and the values of user-defined types. When DynaJson serializes a value, it has to convert this to a dynamic object at first. Although it has an inherent overhead, the performance is comparable to others.

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

![image](https://user-images.githubusercontent.com/345831/71347837-8e4a6980-25ae-11ea-9caa-e3d4667293a0.png)

#### Deserialize

![image](https://user-images.githubusercontent.com/345831/71347900-b4700980-25ae-11ea-967e-4cb5834481b2.png)

## Why fast

- Avoid method invocations by inlining aggressively
- Self-implement Dictionary, List, Enumerator to inline these methods
- Pack a JSON value in a 16 bytes struct with NaN boxing
- Use own double-to-string converter to directly write the result into the output buffer (same as Utf8Json)
- Use runtime code generation to make reflective operations faster
- Cover only a minimum set of features so far
