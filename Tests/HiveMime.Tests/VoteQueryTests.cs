using System.Linq.Expressions;

namespace HiveMime.Tests;

public class VoteQueryTests
{
    private readonly Guid[] _candidateIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

    private string _testQueryString = "(0=0 AND 1>1 AND 2>=2 OR NOT (NOT 3<3 AND NOT 4<=4 AND 5=5))";

    private FilterQueryBase _testQuery = new FilterQueryGroup()
    {
        Children =
        [
            new FilterQuery() { Property = "0", ValueOperator = ValueOperator.Equals, Value = "0" },
            new FilterQuery() { Property = "1", ValueOperator = ValueOperator.Greater, Value = "1" },
            new FilterQuery() { Property = "2", ValueOperator = ValueOperator.GreaterEquals, Value = "2" },
            new FilterQueryGroup() { LeftOperator = BooleanOperator.Or, IsNegated = true,
                Children =
                [
                    new FilterQuery() { Property = "3", IsNegated = true, ValueOperator = ValueOperator.Less, Value = "3" },
                    new FilterQuery() { Property = "4", IsNegated = true, ValueOperator = ValueOperator.LessEquals, Value = "4" },
                    new FilterQuery() { Property = "5", ValueOperator = ValueOperator.Equals, Value = "5" }
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

    // [Theory]
    // [InlineData(0, 2, 3, 4, 5, 5)]
    // [InlineData(1, 2, 3, 4, 5, 5)]
    // [InlineData(0, 1, 3, 4, 5, 5)]
    // [InlineData(0, 2, 1, 4, 5, 5)]
    // [InlineData(0, 2, 1, 2, 5, 5)]
    // [InlineData(0, 2, 1, 4, 4, 5)]
    // [InlineData(0, 2, 1, 4, 5, 4)]
    // public async Task ToExpression_WithDeepQuery_ReturnsExpected(int value1, int value2, int value3, int value4, int value5, int value6)
    // {
    //     // Arrange
    //     var c0 = _candidateIds[0];
    //     var c1 = _candidateIds[1];
    //     var c2 = _candidateIds[2];
    //     var c3 = _candidateIds[3];
    //     var c4 = _candidateIds[4];
    //     var c5 = _candidateIds[5];

    //     Func<PostVote, bool> referenceQuery = (PostVote) => 
    //         PostVote.Votes.First(v => v.CandidateId == c0)?.Value == 0 &&
    //         PostVote.Votes.First(v => v.CandidateId == c1)?.Value > 1 &&
    //         PostVote.Votes.First(v => v.CandidateId == c2)?.Value >= 2 ||
    //         !( !(PostVote.Votes.First(v => v.CandidateId == c3)?.Value < 3) &&
    //            !(PostVote.Votes.First(v => v.CandidateId == c4)?.Value <= 4) &&
    //             PostVote.Votes.First(v => v.CandidateId == c5)?.Value == 5);

    //     int[] values = new int[] { value1, value2, value3, value4, value5, value6 };
    //     PostVote testPostVote = new PostVote()
    //     {
    //         Votes = values.Select((v, i) => new CandidateVote() { Property = _candidateIds[i], Value = v }).ToList()
    //     };

    //     // Act
    //     Expression<Func<PostVote, bool>> expression = _testQuery.ToExpression();
    //     Func<PostVote, bool> compiledExpression = expression.Compile();
        
    //     // Assert
    //     Assert.Equal(referenceQuery(testPostVote), compiledExpression(testPostVote));
    // }
}
