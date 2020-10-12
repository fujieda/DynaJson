# DynaJson

A fast and compact drop-in replacement of DynamicJson

DynaJson is designed as a drop-in replacement of [DynamicJson](https://github.com/neuecc/DynamicJson).  You can intuitively manipulate JSON data through the dynamic type in the same way as DynamicJson. It is written from scratch and licensed under the MIT license instead of Ms-PL of DynamicJson.

DynaJson can parse and serialize five times faster than DynamicJson. It has no recursive call to process deeply nested JSON data and no extra dependency except for Microsoft.CSharp for .Net Standard to reduce the package size of your applications.

## Usage

DynaJson is available on NuGet for .NET Standard 2.0 and .NET Framework 4.5.1.

```
PM> Install-Package DynaJson
```

The following examples are borrowed from DynamicJson.

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

![benchmark-dynamic-parse2](https://user-images.githubusercontent.com/345831/95461724-89c23680-09b1-11eb-8785-d342013609c0.png)

DynaJson is considerably faster than other parsers.

#### Serialize back

The following shows the time to serialize each result of parsing back to its string representation. It evaluates the throughput of serializing large objects.

![benchmark-dynamic-serialize2](https://user-images.githubusercontent.com/345831/95464859-68fbe000-09b5-11eb-9db2-8e293723ab16.png)

DynaJson is the fastest partially because it can serialize only the dynamic objects generated by its parser and provides no customization of string representations.

#### Parse and serialize back citm_catalog.json

![benchmakr-dynamic-citm](https://user-images.githubusercontent.com/345831/95582675-ed159c80-0a75-11eb-84df-d8ad89bd65f8.png)

## Benchmark for user-defined types

DynaJson can serialize and deserialize only its own dynamic objects. It, however, has a converter between dynamic objects and values of user-defined types. When DynaJson serializes a value, it converts this to a dynamic object first. Although it has an inherent overhead, the performance is comparable to others.

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

![benchmark-static-serialize](https://user-images.githubusercontent.com/345831/95708649-c931a100-0c97-11eb-813a-6bacbcc91ecf.png)

#### Deserialize

![benchmark-static-deserialize](https://user-images.githubusercontent.com/345831/95708703-e49cac00-0c97-11eb-8143-8582bcab02f6.png)

## Why fast

- Avoid method invocations by inlining aggressively
- Self-implement Dictionary, List, Enumerator to inline these methods
- Pack a JSON value in a 16 bytes struct with NaN boxing
- Use own double-to-string converter to directly write the result into the output buffer (same as Utf8Json)
- Use runtime code generation to make reflective operations faster
- Cover only a minimum set of features so far
