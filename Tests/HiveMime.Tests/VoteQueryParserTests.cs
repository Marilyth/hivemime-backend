namespace HiveMime.Tests;

public class VoteQueryParserTests
{
    private static FilterQuery Leaf(
        string property = ":Country",
        string value = "US",
        ValueOperator op = ValueOperator.Equals,
        BooleanOperator leftOperator = BooleanOperator.And,
        bool negated = false)
        => new()
        {
            Property = property,
            Value = value,
            ValueOperator = op,
            LeftOperator = leftOperator,
            IsNegated = negated
        };

    private static FilterQueryGroup Group(params FilterQueryBase[] children) => new()
    {
        Children = [.. children]
    };

    [Fact]
    public void ToAST_NullQuery_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => ((FilterQueryBase)null!).ToAST());
    }

    [Fact]
    public void ToAST_SingleLeaf_ReturnsEquivalentCopy()
    {
        var leaf = Leaf(":Age", "18", ValueOperator.Greater);

        var result = leaf.ToAST();

        var copy = Assert.IsType<FilterQuery>(result);
        Assert.NotSame(leaf, copy);
        Assert.Equal(leaf.IsNegated, copy.IsNegated);
        Assert.Equal(leaf.LeftOperator, copy.LeftOperator);
        Assert.Equal(leaf.Property, copy.Property);
        Assert.Equal(leaf.Value, copy.Value);
        Assert.Equal(leaf.ValueOperator, copy.ValueOperator);
    }

    [Fact]
    public void ToAST_EmptyGroup_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() => Group().ToAST());
    }

    [Fact]
    public void ToAST_SingleChildGroup_CollapsesIntoChild()
    {
        var group = Group(Leaf());

        var result = group.ToAST();

        var leaf = Assert.IsType<FilterQuery>(result);
        Assert.False(leaf.IsNegated);
        Assert.Equal(":Country=US", leaf.ToString());
    }

    [Fact]
    public void ToAST_NegatedSingleChildGroup_PropagatesNegation()
    {
        var group = Group(Leaf());
        group.IsNegated = true;

        var result = group.ToAST();

        var leaf = Assert.IsType<FilterQuery>(result);
        Assert.True(leaf.IsNegated);
        Assert.Equal("NOT :Country=US", leaf.ToString());
    }

    [Fact]
    public void ToAST_DoubleNegatedSingleChildGroup_ProducesPositiveLeaf()
    {
        var group = Group(Leaf(negated: true));
        group.IsNegated = true;

        var result = group.ToAST();

        var leaf = Assert.IsType<FilterQuery>(result);
        Assert.False(leaf.IsNegated);
        Assert.Equal(":Country=US", leaf.ToString());
    }

    [Fact]
    public void ToAST_TwoLeaves_ProducesBalancedBinaryGroup()
    {
        var group = Group(
            Leaf(":Country", "US"),
            Leaf(":Age", "18", ValueOperator.Greater));

        var result = Assert.IsType<FilterQueryGroup>(group.ToAST());

        Assert.Equal(2, result.Children.Count);
        Assert.All(result.Children, c => Assert.IsType<FilterQuery>(c));
        Assert.Equal("(:Country=US AND :Age>18)", result.ToString());
    }

    [Fact]
    public void ToAST_GreedySplit_FavorsOrChildNearMiddle()
    {
        var group = Group(
            Leaf(":Country", "US"),
            Leaf(":Age", "18", ValueOperator.Greater),
            Leaf(":Date", "2024", ValueOperator.Less, leftOperator: BooleanOperator.Or),
            Leaf(":Country", "CA"));

        var result = Assert.IsType<FilterQueryGroup>(group.ToAST());

        Assert.Equal(2, result.Children.Count);

        var left = Assert.IsType<FilterQueryGroup>(result.Children[0]);
        var right = Assert.IsType<FilterQueryGroup>(result.Children[1]);

        Assert.Equal(2, left.Children.Count);
        Assert.Equal(2, right.Children.Count);
        Assert.Equal(BooleanOperator.Or, right.LeftOperator);
        Assert.Equal(":Date<2024", Assert.IsType<FilterQuery>(right.Children[0]).ToString());
        Assert.Equal("((:Country=US AND :Age>18) OR (:Date<2024 AND :Country=CA))", result.ToString());
    }

    [Fact]
    public void ToAST_SplitPropagatesGroupNegation()
    {
        var group = Group(
            Leaf(":Country", "US"),
            Leaf(":Age", "18", ValueOperator.Greater),
            Leaf(":Date", "2024", ValueOperator.Less));
        group.IsNegated = true;

        var result = Assert.IsType<FilterQueryGroup>(group.ToAST());

        Assert.True(result.IsNegated);
        Assert.Equal(2, result.Children.Count);
        Assert.Equal("NOT (:Country=US AND (:Age>18 AND :Date<2024))", result.ToString());
    }

    [Fact]
    public void ToAST_DeepQuery_ProducesBalancedTree()
    {
        var group = new FilterQueryGroup()
        {
            Children =
            [
                Leaf(":Country", "US"),
                Leaf(":Age", "18", ValueOperator.Greater),
                new FilterQueryGroup()
                {
                    Children = [Leaf(":Date", "2024", ValueOperator.Less), Leaf(":Country", "CA")],
                    LeftOperator = BooleanOperator.Or
                },
            ]
        };

        var result = Assert.IsType<FilterQueryGroup>(group.ToAST());

        Assert.Equal(2, result.Children.Count);
        Assert.All(result.Children, c => Assert.IsType<FilterQueryGroup>(c));
        Assert.Equal("((:Country=US AND :Age>18) OR (:Date<2024 AND :Country=CA))", result.ToString());
    }
}
