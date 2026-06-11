using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MiLauncher.launcher.tools
{
    /// <summary>
    /// Equivalent to MiLauncher's Json
    /// Standardized JSON handling logic across the launcher.
    /// </summary>
    public static class Json
    {
        public static void Write(JsonDocument doc, string filename)
        {
            FileSystem.Write(filename, JsonSerializer.SerializeToUtf8Bytes(doc));
        }

        public static void Write(JsonObject obj, string filename)
        {
            FileSystem.Write(filename, JsonSerializer.SerializeToUtf8Bytes(obj));
        }

        public static void Write(JsonArray array, string filename)
        {
            FileSystem.Write(filename, JsonSerializer.SerializeToUtf8Bytes(array));
        }

        public static byte[] ToText(JsonObject obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj);
        }

        public static byte[] ToText(JsonArray array)
        {
            return JsonSerializer.SerializeToUtf8Bytes(array);
        }

        public static JsonDocument RequireDocument(byte[] data, string what)
        {
            try
            {
                var options = new JsonDocumentOptions { AllowTrailingCommas = true };
                return JsonDocument.Parse(data, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"{what}: Error parsing JSON: {ex.Message}");
            }
        }

        public static JsonDocument RequireDocument(string filename, string what)
        {
            return RequireDocument(FileSystem.Read(filename), what);
        }

        public static JsonObject RequireObject(JsonDocument doc, string what)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"{what} is not an object");
            }
            return JsonObject.Create(doc.RootElement);
        }

        public static JsonArray RequireArray(JsonDocument doc, string what)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"{what} is not an array");
            }
            return JsonArray.Create(doc.RootElement);
        }

        public static JsonDocument ParseUntilGarbage(byte[] json, out string garbage)
        {
            garbage = null;
            try
            {
                var options = new JsonDocumentOptions { AllowTrailingCommas = true };
                return JsonDocument.Parse(json, options);
            }
            catch (JsonException ex)
            {
                if (ex.LineNumber > 0 && ex.BytePositionInLine > 0)
                {
                    // Basic emulation of QJsonParseError::GarbageAtEnd
                    long offset = ex.BytePositionInLine ?? 0;
                    byte[] validJson = new byte[(int)offset];
                    Array.Copy(json, validJson, (int)offset);
                    
                    garbage = System.Text.Encoding.UTF8.GetString(json, (int)offset, json.Length - (int)offset);
                    return JsonDocument.Parse(validJson);
                }
                throw;
            }
        }

        public static void WriteString(JsonObject to, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                to[key] = value;
            }
        }

        public static void WriteStringList(JsonObject to, string key, List<string> values)
        {
            if (values != null && values.Count > 0)
            {
                var array = new JsonArray();
                foreach (var value in values)
                {
                    array.Add(value);
                }
                to[key] = array;
            }
        }

        public static T RequireIsType<T>(JsonNode value, string what)
        {
            if (value == null)
                throw new InvalidOperationException($"{what} is null or undefined");

            try
            {
                if (typeof(T) == typeof(byte[]))
                {
                    string str = value.GetValue<string>();
                    return (T)(object)Convert.FromHexString(str);
                }
                else if (typeof(T) == typeof(JsonArray))
                {
                    if (value is not JsonArray array)
                        throw new InvalidOperationException($"{what} is not an array");
                    return (T)(object)array;
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)value.GetValue<string>();
                }
                else if (typeof(T) == typeof(bool))
                {
                    return (T)(object)value.GetValue<bool>();
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)value.GetValue<double>();
                }
                else if (typeof(T) == typeof(int))
                {
                    double doubl = value.GetValue<double>();
                    if (doubl % 1 != 0)
                        throw new InvalidOperationException($"{what} is not an integer");
                    return (T)(object)(int)doubl;
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    string str = value.GetValue<string>();
                    if (!DateTime.TryParse(str, out DateTime dt))
                        throw new InvalidOperationException($"{what} is not a ISO formatted date/time value");
                    return (T)(object)dt;
                }
                else if (typeof(T) == typeof(Uri))
                {
                    string str = value.GetValue<string>();
                    if (string.IsNullOrEmpty(str)) return default;
                    if (!Uri.TryCreate(str, UriKind.Absolute, out Uri uri))
                        throw new InvalidOperationException($"{what} is not a correctly formatted URL");
                    return (T)(object)uri;
                }
                else if (typeof(T) == typeof(Guid))
                {
                    string str = value.GetValue<string>();
                    if (!Guid.TryParse(str, out Guid guid))
                        throw new InvalidOperationException($"{what} is not a valid UUID");
                    return (T)(object)guid;
                }
                else if (typeof(T) == typeof(JsonObject))
                {
                    if (value is not JsonObject obj)
                        throw new InvalidOperationException($"{what} is not an object");
                    return (T)(object)obj;
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"{what} is not a valid {typeof(T).Name}");
            }

            throw new NotSupportedException($"Type {typeof(T).Name} is not supported by RequireIsType");
        }

        public static List<string> ToStringList(string jsonString)
        {
            try
            {
                var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return new List<string>();

                var list = new List<string>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    list.Add(element.GetString());
                }
                return list;
            }
            catch
            {
                return new List<string>();
            }
        }

        public static string FromStringList(List<string> list)
        {
            var array = new JsonArray();
            foreach (var str in list)
            {
                array.Add(str);
            }
            return JsonSerializer.Serialize(array, new JsonSerializerOptions { WriteIndented = false });
        }

        public static Dictionary<string, object> ToMap(string jsonString)
        {
            try
            {
                var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return new Dictionary<string, object>();

                return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public static string FromMap(Dictionary<string, object> map)
        {
            return JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = false });
        }
    }
}
