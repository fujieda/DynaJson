﻿using System.IO;
using System.Text;
// ReSharper disable MemberCanBePrivate.Global

namespace DynaJson
{
    public class DynaJson: JsonObject
    {
        public static int MaxDepth { get; set; } = 512;

        public DynaJson()
        {
        }

        public DynaJson(object obj) : base(obj)
        {
        }

        public static dynamic Parse(string json)
        {
            return Parse(new StringReader(json));
        }

        public static dynamic Parse(Stream stream, Encoding encoding = null)
        {
            return Parse(new StreamReader(stream, encoding ?? Encoding.UTF8));
        }

        public static dynamic Parse(TextReader reader)
        {
            return JsonParser.Parse(reader, MaxDepth);
        }

        public static string Serialize(object obj)
        {
            var writer = new StringWriter();
            Serializer.Serialize(obj, writer, MaxDepth);
            return writer.ToString();
        }
    }
}