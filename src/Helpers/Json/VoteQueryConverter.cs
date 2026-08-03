using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class VoteQueryConverter : JsonConverter<VoteQueryBase>
{
    public override VoteQueryBase Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        Type type = root switch
        {
            _ when root.TryGetProperty("children", out _) => typeof(VoteQueryGroup),
            _ when root.TryGetProperty("candidateId", out _) => typeof(VoteQuery),
            _ => throw new JsonException("Unknown VoteQueryBase type.")
        };

        return (VoteQueryBase)JsonSerializer.Deserialize(
            root.GetRawText(),
            type,
            options)!;
    }

    public override void Write(
        Utf8JsonWriter writer,
        VoteQueryBase value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}