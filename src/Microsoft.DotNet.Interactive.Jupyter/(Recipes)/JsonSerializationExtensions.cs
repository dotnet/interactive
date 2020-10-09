using System;
using Newtonsoft.Json;

namespace Recipes
{
    internal static class JsonSerializationExtensions
    {
        public static string ToJson(this object source) =>
            JsonConvert.SerializeObject(source);

        public static T FromJsonTo<T>(this string json) =>
            JsonConvert.DeserializeObject<T>(json);

        public static object FromJsonTo(this string json, Type type, JsonSerializerSettings settings = null) =>
            JsonConvert.DeserializeObject(json, type, settings);
    }
}
