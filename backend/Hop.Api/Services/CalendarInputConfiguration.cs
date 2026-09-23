using System.Text.Json.Serialization.Metadata;

namespace Hop.Api.Services;

public static class CalendarInputConfiguration
{
    public static IMvcBuilder AddCalendarInput(this IMvcBuilder builder) => builder
        .AddMvcOptions(options =>
        {
            options.ModelBinderProviders.Insert(0, new CalendarDateModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new CalendarYearModelBinderProvider());
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new CalendarDateJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new CalendarInstantJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new CalendarOffsetJsonConverter());
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(info =>
            {
                foreach (var property in info.Properties.Where(p => CalendarYearInput.IsJsonCalendarYear(p.Name)))
                {
                    if (property.PropertyType == typeof(int)) property.CustomConverter = new CalendarYearJsonConverter();
                    if (property.PropertyType == typeof(int?)) property.CustomConverter = new NullableCalendarYearJsonConverter();
                }
            });
            options.JsonSerializerOptions.TypeInfoResolver = resolver;
        });
}
