using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class VoteQueryConverter : JsonConverter<FilterQueryBase>
{
    public override FilterQueryBase Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        Type type = root switch
        {
            _ when root.TryGetProperty("children", out _) => typeof(FilterQueryGroup),
            _ when root.TryGetProperty("property", out _) => typeof(FilterQuery),
            _ => throw new JsonException("Unknown VoteQueryBase type.")
        };

        return (FilterQueryBase)JsonSerializer.Deserialize(
            root.GetRawText(),
            type,
            options)!;
    }

    public override void Write(
        Utf8JsonWriter writer,
        FilterQueryBase value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}