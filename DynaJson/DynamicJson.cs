using System.IO;
using System.Text;

namespace DynaJson
{
    public class DynamicJson
    {
        public static dynamic Parse(string json)
        {
            return JsonObject.Parse(json);
        }

        public static dynamic Parse(Stream stream)
        {
            return JsonObject.Parse(stream);
        }

        public static dynamic Parse(Stream stream, Encoding encoding)
        {
            return JsonObject.Parse(stream, encoding);
        }

        public static string Serialize(object obj)
        {
            return JsonObject.Serialize(obj);
        }
    }
}