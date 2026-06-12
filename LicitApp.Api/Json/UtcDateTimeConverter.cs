using System.Text.Json;
using System.Text.Json.Serialization;

namespace LicitApp.Api.Json;

/// <summary>
/// Garantiza que todo DateTime que entra/sale de la API sea UTC.
/// Npgsql exige DateTimeKind.Utc para columnas timestamptz; el front usa ISO 8601.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            // Sin offset: asumimos que el cliente envió UTC.
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly UtcDateTimeConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : _inner.Read(ref reader, typeToConvert, options);

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else _inner.Write(writer, value.Value, options);
    }
}
