using System.Text;
using System.Text.Json;

namespace VideoConferencingApp.Infrastructure.Caching
{
    public static class SerializationHelper
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public static byte[] ToBytes<T>(T value)
            => JsonSerializer.SerializeToUtf8Bytes(value, Options);

        public static T? FromBytes<T>(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0) return default;
            return JsonSerializer.Deserialize<T>(bytes, Options);
        }

        public static byte[] ToBytesString(string value) => Encoding.UTF8.GetBytes(value);
        public static string? FromBytesString(byte[]? bytes)
            => bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }
}