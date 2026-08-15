using System.ComponentModel;

public enum BooleanOperator
{
    And,
    Or
}

public enum ValueOperator
{
    [Description("=")]
    Equals,
    [Description(">")]
    Greater,
    [Description(">=")]
    GreaterEquals,
    [Description("<")]
    Less,
    [Description("<=")]
    LessEquals
}

public enum SubProperty
{
    Row,
    Column,
    Date,
    Month,
    DayOfMonth,
    DayOfWeek,
    Hour,
    Minute
}