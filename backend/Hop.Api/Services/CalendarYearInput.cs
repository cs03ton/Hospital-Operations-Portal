using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Hop.Api.Services;

public static class CalendarYearInput
{
    // JSON conversion is intentionally limited to a true calendar-year field.
    // Fiscal years are business keys and generic Year is used by leave balances.
    public static bool IsJsonCalendarYear(string? name) =>
        string.Equals(name, "manufactureYear", StringComparison.OrdinalIgnoreCase);

    // Generic "year" is excluded because several endpoints use it as a fiscal
    // business key. Explicit calendar filters may opt in by name.
    public static bool IsQueryCalendarYear(string? name) => new[] { "trendYear", "manufactureYear" }
        .Contains(name, StringComparer.OrdinalIgnoreCase);
    public static int Parse(int year)
    {
        year = CalendarDateInput.NormalizeYear(year);
        return year is >= 1900 and <= 2100 ? year : throw new JsonException("ปีอยู่นอกช่วง ค.ศ. 1900–2100 / พ.ศ. 2443–2643");
    }
}
public sealed class CalendarYearJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => CalendarYearInput.Parse(reader.GetInt32());
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
public sealed class NullableCalendarYearJsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType == JsonTokenType.Null ? null : CalendarYearInput.Parse(reader.GetInt32());
    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options) { if (value.HasValue) writer.WriteNumberValue(value.Value); else writer.WriteNullValue(); }
}
public sealed class CalendarYearModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        (Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType) == typeof(int) &&
        CalendarYearInput.IsQueryCalendarYear(context.Metadata.PropertyName ?? context.Metadata.ParameterName) ? new CalendarYearModelBinder() : null;
}
public sealed class CalendarYearModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        if (string.IsNullOrEmpty(value.FirstValue) && Nullable.GetUnderlyingType(context.ModelType) != null)
            context.Result = ModelBindingResult.Success(null);
        else if (int.TryParse(value.FirstValue, out var year) && CalendarDateInput.NormalizeYear(year) is >= 1900 and <= 2100)
            context.Result = ModelBindingResult.Success(CalendarDateInput.NormalizeYear(year));
        else context.ModelState.TryAddModelError(context.ModelName, "ปีไม่ถูกต้อง");
        return Task.CompletedTask;
    }
}
