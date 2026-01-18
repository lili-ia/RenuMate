using System.Text.Json;
using System.Text.Json.Serialization;

namespace RenuMate.Api.Converters;

/// <summary>
/// Ensures all <see cref="DateTime"/> properties are handled as UTC during JSON serialization.
/// </summary>
/// <remarks>
/// <para>
/// When reading: Converts incoming strings to a UTC-kind DateTime.
/// </para>
/// <para>
/// When writing: Formats the date using the ISO 8601 "O" (round-trip) format (e.g., 2023-10-25T14:30:00.0000000Z).
/// </para>
/// </remarks>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dt = DateTime.Parse(reader.GetString()!);
        
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("O"));
    }
}