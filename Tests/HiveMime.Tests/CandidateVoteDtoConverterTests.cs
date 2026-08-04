using System.Text.Json;

namespace HiveMime.Tests;

public class CandidateVoteDtoConverterTests
{
    private static readonly Guid _id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _categoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Read_WithScore_ReturnsCandidateScoreVoteDto()
    {
        var result = Deserialize<CandidateVoteDto>($"{{\"id\":\"{_id}\",\"name\":\"A\",\"score\":3.5}}");

        var score = Assert.IsType<CandidateScoreVoteDto>(result);
        Assert.Equal(_id, score.Id);
        Assert.Equal("A", score.Name);
        Assert.Equal(3.5, score.Score);
    }

    [Fact]
    public void Read_WithRank_ReturnsCandidateRankVoteDto()
    {
        var result = Deserialize<CandidateVoteDto>($"{{\"id\":\"{_id}\",\"name\":\"A\",\"rank\":2}}");

        var rank = Assert.IsType<CandidateRankVoteDto>(result);
        Assert.Equal(2, rank.Rank);
    }

    [Fact]
    public void Read_WithCategoryId_ReturnsCandidateCategoryVoteDto()
    {
        var result = Deserialize<CandidateVoteDto>($"{{\"id\":\"{_id}\",\"name\":\"A\",\"categoryId\":\"{_categoryId}\"}}");

        var category = Assert.IsType<CandidateCategoryVoteDto>(result);
        Assert.Equal(_categoryId, category.CategoryId);
    }

    [Fact]
    public void Read_WithCellIndex_ReturnsCandidateDrawVoteDto()
    {
        var result = Deserialize<CandidateVoteDto>($"{{\"id\":\"{_id}\",\"name\":\"A\",\"cellIndex\":7}}");

        var draw = Assert.IsType<CandidateDrawVoteDto>(result);
        Assert.Equal(7, draw.CellIndex);
    }

    [Fact]
    public void Read_WithTimestamp_ReturnsCandidateDateVoteDto()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = Deserialize<CandidateVoteDto>($"{{\"id\":\"{_id}\",\"name\":\"A\",\"timestamp\":\"{timestamp:o}\"}}");

        var date = Assert.IsType<CandidateDateVoteDto>(result);
        Assert.Equal(timestamp, date.Timestamp);
    }

    [Fact]
    public void Read_WithoutTypeSpecificKeys_FallsBackToChoice()
    {
        var result = Deserialize<CandidateVoteDto>($"{{\"id\":\"{_id}\",\"name\":\"A\"}}");

        var choice = Assert.IsType<CandidateChoiceVoteDto>(result);
        Assert.Equal(_id, choice.Id);
        Assert.Equal("A", choice.Name);
    }

    [Theory]
    [InlineData("score")]
    [InlineData("rank")]
    [InlineData("categoryId")]
    [InlineData("cellIndex")]
    public void Read_PropertyNames_AreCaseSensitive(string key)
    {
        // JSON keys are matched with default (case-sensitive) options. A wrong-case key must fall back to Choice.
        string json = $"{{\"id\":\"{_id}\",\"name\":\"A\",\"{key.ToUpperInvariant()}\":1}}";

        var result = Deserialize<CandidateVoteDto>(json);

        Assert.IsType<CandidateChoiceVoteDto>(result);
    }

    [Fact]
    public void Write_SerializesConcreteSubtype()
    {
        string json = Serialize(new CandidateScoreVoteDto { Id = _id, Name = "A", Score = 4 });

        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("score", out JsonElement score));
        Assert.Equal(4, score.GetDouble());
        Assert.Equal("A", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void RoundTrip_ScorePreservesValueAndType()
    {
        var original = new CandidateScoreVoteDto { Id = _id, Name = "A", Score = 9.25 };
        string json = Serialize(original);

        var result = Deserialize<CandidateVoteDto>(json);

        var score = Assert.IsType<CandidateScoreVoteDto>(result);
        Assert.Equal(9.25, score.Score);
        Assert.Equal(_id, score.Id);
        Assert.Equal("A", score.Name);
    }

    [Fact]
    public void RoundTrip_DrawPreservesCellIndex()
    {
        var original = new CandidateDrawVoteDto { Id = _id, Name = "Cell", CellIndex = 3 };
        string json = Serialize(original);

        var result = Deserialize<CandidateVoteDto>(json);

        var draw = Assert.IsType<CandidateDrawVoteDto>(result);
        Assert.Equal(3, draw.CellIndex);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        // Match the app's configured JSON options (camelCase) in Program.cs.
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new CandidateVoteDtoConverter());
        return options;
    }

    private static string Serialize<T>(T value) where T : CandidateVoteDto
        => JsonSerializer.Serialize(value, value.GetType(), CreateOptions());

    private static T Deserialize<T>(string json) where T : CandidateVoteDto
        => JsonSerializer.Deserialize<T>(json, CreateOptions());
}
