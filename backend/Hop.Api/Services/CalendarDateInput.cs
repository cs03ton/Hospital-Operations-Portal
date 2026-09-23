using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Hop.Api.Services;

/// <summary>Human calendar input is normalized before Gregorian date validation.</summary>
public static class CalendarDateInput
{
    public static int NormalizeYear(int year) => year is >= 2443 and <= 2643 ? year - 543 : year;

    public static bool TryParse(string? input, out DateOnly date)
    {
        date = default;
        if (input is null || input.Length != 10 || input[4] != '-' || input[7] != '-') return false;
        if (!int.TryParse(input.AsSpan(0, 4), out var year)) return false;
        year = NormalizeYear(year);
        return year is >= 1900 and <= 2100 && DateOnly.TryParseExact(
            $"{year:0000}{input[4..]}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}

public sealed class CalendarDateJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && CalendarDateInput.TryParse(reader.GetString(), out var date)) return date;
        throw new JsonException("วันที่ต้องเป็น YYYY-MM-DD ปี ค.ศ. 1900–2100 หรือ พ.ศ. 2443–2643 และเป็นวันที่มีจริง");
    }
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}

public sealed class CalendarDateModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        new[] { typeof(DateOnly), typeof(DateTime), typeof(DateTimeOffset) }.Contains(Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType)
            ? new CalendarDateModelBinder() : null;
}

public sealed class CalendarDateModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        if (string.IsNullOrEmpty(value.FirstValue) && Nullable.GetUnderlyingType(context.ModelType) != null)
            context.Result = ModelBindingResult.Success(null);
        else if ((Nullable.GetUnderlyingType(context.ModelType) ?? context.ModelType) == typeof(DateOnly) && CalendarDateInput.TryParse(value.FirstValue, out var date))
            context.Result = ModelBindingResult.Success(date);
        else if ((Nullable.GetUnderlyingType(context.ModelType) ?? context.ModelType) != typeof(DateOnly) && TryQueryInstant(value.FirstValue, out var instant))
            context.Result = ModelBindingResult.Success((Nullable.GetUnderlyingType(context.ModelType) ?? context.ModelType) == typeof(DateTime)
                ? (object)instant.UtcDateTime : instant.ToUniversalTime());
        else context.ModelState.TryAddModelError(context.ModelName, "วันที่ไม่ถูกต้อง (YYYY-MM-DD ปี ค.ศ. หรือ พ.ศ.)");
        return Task.CompletedTask;
    }

    private static bool TryQueryInstant(string? value, out DateTimeOffset instant)
    {
        // Existing date-only filters mean midnight in the hospital timezone.
        if (CalendarDateInput.TryParse(value, out var date))
        {
            instant = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));
            return true;
        }
        return CalendarInstantInput.TryParse(value, out instant);
    }
}
