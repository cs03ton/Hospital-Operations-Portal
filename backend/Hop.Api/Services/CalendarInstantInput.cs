using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hop.Api.Services;

public static class CalendarInstantInput
{
    public static bool TryParse(string? input, out DateTimeOffset instant)
    {
        instant = default;
        if (input is null || input.Length < 20 || input[10] != 'T' || !CalendarDateInput.TryParse(input[..10], out var date)) return false;
        var suffix = input[11..];
        if (!suffix.EndsWith('Z') && !suffix.Contains('+') && !suffix.Contains('-')) return false;
        return DateTimeOffset.TryParse(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + input[10..],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out instant);
    }
}

public sealed class CalendarInstantJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && CalendarInstantInput.TryParse(reader.GetString(), out var date)
            ? date.UtcDateTime : throw new JsonException("วันเวลาต้องเป็น ISO 8601 พร้อม timezone และปีที่ถูกต้อง");
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}

public sealed class CalendarOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && CalendarInstantInput.TryParse(reader.GetString(), out var date)
            ? date.ToUniversalTime() : throw new JsonException("วันเวลาต้องเป็น ISO 8601 พร้อม timezone และปีที่ถูกต้อง");
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
