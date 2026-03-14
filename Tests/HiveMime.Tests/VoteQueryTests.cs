using System.Linq.Expressions;

namespace HiveMime.Tests;

public class VoteQueryTests
{
    private string _testQueryString = "(0=0 AND 1>1 AND 2>=2 OR NOT (NOT 3<3 AND NOT 4<=4 AND 5=5))";

    private VoteQueryBase _testQuery = new VoteQueryGroup()
    {
        Children =
        [
            new VoteQuery() { CandidateId = 0, ValueOperator = ValueOperator.Equals, Value = 0 },
            new VoteQuery() { CandidateId = 1, ValueOperator = ValueOperator.Greater, Value = 1 },
            new VoteQuery() { CandidateId = 2, ValueOperator = ValueOperator.GreaterEquals, Value = 2 },
            new VoteQueryGroup() { LeftOperator = BooleanOperator.Or, IsNegated = true,
                Children =
                [
                    new VoteQuery() { CandidateId = 3, IsNegated = true, ValueOperator = ValueOperator.Less, Value = 3 },
                    new VoteQuery() { CandidateId = 4, IsNegated = true, ValueOperator = ValueOperator.LessEquals, Value = 4 },
                    new VoteQuery() { CandidateId = 5, ValueOperator = ValueOperator.Equals, Value = 5 }
                ] },
        ]
    };

    [Fact]
    public async Task ToString_WithDeepQuery_ReturnsExpected()
    {
        // Act
        string result = _testQuery.ToString();

        // Assert
        Assert.Equal(_testQueryString, result);
    }

    [Fact]
    public async Task ToVoteQuery_WithDeepQuery_ReturnsExpected()
    {
        // Act
        VoteQueryBase result = _testQueryString.ToVoteQuery();

        // Assert
        Assert.Equal(_testQueryString, result.ToString());
    }

    [Theory]
    [InlineData(0, 2, 3, 4, 5, 5)]
    [InlineData(1, 2, 3, 4, 5, 5)]
    [InlineData(0, 1, 3, 4, 5, 5)]
    [InlineData(0, 2, 1, 4, 5, 5)]
    [InlineData(0, 2, 1, 2, 5, 5)]
    [InlineData(0, 2, 1, 4, 4, 5)]
    [InlineData(0, 2, 1, 4, 5, 4)]
    public async Task ToExpression_WithDeepQuery_ReturnsExpected(int value1, int value2, int value3, int value4, int value5, int value6)
    {
        // Arrange
        Func<PostVote, bool> referenceQuery = (PostVote) => 
            PostVote.Votes.First(v => v.CandidateId == 0)?.Value == 0 &&
            PostVote.Votes.First(v => v.CandidateId == 1)?.Value > 1 &&
            PostVote.Votes.First(v => v.CandidateId == 2)?.Value >= 2 ||
            !( !(PostVote.Votes.First(v => v.CandidateId == 3)?.Value < 3) &&
               !(PostVote.Votes.First(v => v.CandidateId == 4)?.Value <= 4) &&
                PostVote.Votes.First(v => v.CandidateId == 5)?.Value == 5);

        int[] values = new int[] { value1, value2, value3, value4, value5, value6 };
        PostVote testPostVote = new PostVote()
        {
            Votes = values.Select((v, i) => new CandidateVote() { CandidateId = i, Value = v }).ToList()
        };

        // Act
        Expression<Func<PostVote, bool>> expression = _testQuery.ToExpression();
        Func<PostVote, bool> compiledExpression = expression.Compile();
        
        // Assert
        Assert.Equal(referenceQuery(testPostVote), compiledExpression(testPostVote));
    }
}
