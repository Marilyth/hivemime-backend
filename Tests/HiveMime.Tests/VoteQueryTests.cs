namespace HiveMime.Tests;

public class VoteQueryTests
{
    [Fact]
    public async Task ToString_WithDeepQuery_ReturnsExpected()
    {
        // Arrange
        string expected = "(0:0=0 AND 0:1>1 AND 0:2>=2 OR NOT (NOT 1:0<3 AND NOT 1:1<=4) AND 2:0=5)";
        VoteQueryGroup baseGroup = new VoteQueryGroup()
        {
            Children =
            [
                new VoteQuery() { PollIndex = 0, CandidateIndex = 0, ValueOperator = ValueOperator.Equals, Value = 0 },
                new VoteQuery() { PollIndex = 0, CandidateIndex = 1, ValueOperator = ValueOperator.Greater, Value = 1 },
                new VoteQuery() { PollIndex = 0, CandidateIndex = 2, ValueOperator = ValueOperator.GreaterEquals, Value = 2 },
                new VoteQueryGroup() { LeftOperator = BooleanOperator.Or, IsNegated = true,
                    Children =
                    [
                        new VoteQuery() { IsNegated = true, PollIndex = 1, CandidateIndex = 0, ValueOperator = ValueOperator.Less, Value = 3 },
                        new VoteQuery() { IsNegated = true, PollIndex = 1, CandidateIndex = 1, ValueOperator = ValueOperator.LessEquals, Value = 4 }
                    ] },
                new VoteQuery() { PollIndex = 2, CandidateIndex = 0, ValueOperator = ValueOperator.Equals, Value = 5 }
            ]
        };

        // Act
        string result = baseGroup.ToString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FromString_WithDeepQuery_ReturnsExpected()
    {
        // Arrange
        string queryExpression = "(0:0=0 AND 0:1>1 AND 0:2>=2 OR NOT (NOT 1:0<3 AND NOT 1:1<=4) AND 2:0=5)";
        VoteQueryGroup baseGroup;

        // Act
        // TODO 9: Replace with your parser.
        baseGroup = new();

        // Assert
        Assert.Equal(queryExpression, baseGroup.ToString());
    }

    [Fact]
    public async Task ToExpression_WithDeepQuery_ReturnsExpected()
    {
        // TODO 9: Implement VoteQuery to expression conversion for EFCore.
    }
}
