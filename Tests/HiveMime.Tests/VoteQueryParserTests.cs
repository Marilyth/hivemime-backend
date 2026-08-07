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

    #region CleanUp

    [Fact]
    public void CleanUp_NullQuery_ReturnsNull()
    {
        Assert.Null(((FilterQueryBase)null!).CleanUp());
    }

    [Fact]
    public void CleanUp_SingleLeaf_ReturnsSameInstance()
    {
        var leaf = Leaf();

        var result = leaf.CleanUp();

        Assert.Same(leaf, result);
    }

    [Fact]
    public void CleanUp_EmptyGroup_ReturnsNull()
    {
        Assert.Null(Group().CleanUp());
    }

    [Fact]
    public void CleanUp_SingleChildGroup_CollapsesIntoChild()
    {
        var group = Group(Leaf());

        var result = group.CleanUp();

        var leaf = Assert.IsType<FilterQuery>(result);
        Assert.False(leaf.IsNegated);
        Assert.Equal(":Country=US", leaf.ToString());
    }

    [Fact]
    public void CleanUp_NegatedSingleChildGroup_PropagatesNegation()
    {
        var group = Group(Leaf());
        group.IsNegated = true;

        var result = group.CleanUp();

        var leaf = Assert.IsType<FilterQuery>(result);
        Assert.True(leaf.IsNegated);
        Assert.Equal("NOT :Country=US", leaf.ToString());
    }

    [Fact]
    public void CleanUp_DoubleNegatedSingleChildGroup_ProducesPositiveLeaf()
    {
        var group = Group(Leaf(negated: true));
        group.IsNegated = true;

        var result = group.CleanUp();

        var leaf = Assert.IsType<FilterQuery>(result);
        Assert.False(leaf.IsNegated);
        Assert.Equal(":Country=US", leaf.ToString());
    }

    [Fact]
    public void CleanUp_RemovesEmptyGroups()
    {
        var group = Group(Leaf(":Country", "US"), Group());

        var result = group.CleanUp();

        var leaf = Assert.IsType<FilterQuery>(result);
        Assert.Equal(":Country=US", leaf.ToString());
    }

    [Fact]
    public void CleanUp_KeepsGroupWithMultipleChildren()
    {
        var group = Group(
            Leaf(":Country", "US"),
            Leaf(":Age", "18", ValueOperator.Greater));

        var result = Assert.IsType<FilterQueryGroup>(group.CleanUp());

        Assert.Equal(2, result.Children.Count);
        Assert.Equal("(:Country=US AND :Age>18)", result.ToString());
    }

    #endregion

    #region ToAST

    [Fact]
    public void ToAST_NullQuery_ReturnsNull()
    {
        Assert.Null(((FilterQueryBase)null!).ToAST());
    }

    [Fact]
    public void ToAST_SingleLeaf_ReturnsSameInstance()
    {
        var leaf = Leaf(":Age", "18", ValueOperator.Greater);

        var result = leaf.ToAST();

        Assert.Same(leaf, result);
        Assert.Equal(":Age>18", result.ToString());
    }

    [Fact]
    public void ToAST_EmptyGroup_ReturnsNull()
    {
        Assert.Null(Group().ToAST());
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
    public void ToAST_CleansUpTreeBeforeBalancing()
    {
        var group = Group(
            Leaf(":Country", "US"),
            new FilterQueryGroup()
            {
                Children = [Leaf(":Age", "18", ValueOperator.Greater)]
            },
            Leaf(":Date", "2024", ValueOperator.Less));

        var result = Assert.IsType<FilterQueryGroup>(group.ToAST());

        Assert.Equal(2, result.Children.Count);
        Assert.IsType<FilterQuery>(result.Children[0]);
        Assert.Equal("(:Country=US AND (:Age>18 AND :Date<2024))", result.ToString());
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

    #endregion
}
