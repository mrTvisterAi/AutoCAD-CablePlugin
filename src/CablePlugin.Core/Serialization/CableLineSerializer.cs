using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Serialization
{
    /// <summary>
    /// Сериализация/десериализация CableLine в JSON для хранения в XRecord.
    /// </summary>
    public static class CableLineSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public static string Serialize(CableLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            return JsonSerializer.Serialize(line, Options);
        }

        public static CableLine Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<CableLine>(json, Options);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static byte[] SerializeToBytes(CableLine line)
        {
            var json = Serialize(line);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public static CableLine DeserializeFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            var json = System.Text.Encoding.UTF8.GetString(data);
            return Deserialize(json);
        }
    }
}
