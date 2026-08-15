using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class CandidateVoteDtoConverter : JsonConverter<CandidateVoteDto>
{
    public override CandidateVoteDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        Type type = root switch
        {
            _ when root.TryGetProperty("score", out _) => typeof(CandidateScoreVoteDto),
            _ when root.TryGetProperty("rank", out _) => typeof(CandidateRankVoteDto),
            _ when root.TryGetProperty("categoryId", out _) => typeof(CandidateCategoryVoteDto),
            _ when root.TryGetProperty("cellIndex", out _) => typeof(CandidateGridVoteDto),
            _ when root.TryGetProperty("timestamp", out _) => typeof(CandidateDateVoteDto),
            _ => typeof(CandidateChoiceVoteDto)
        };

        return (CandidateVoteDto)JsonSerializer.Deserialize(
            root.GetRawText(),
            type,
            options)!;
    }

    public override void Write(
        Utf8JsonWriter writer,
        CandidateVoteDto value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}