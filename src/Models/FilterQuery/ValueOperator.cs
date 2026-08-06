using System.ComponentModel;

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
    LessEquals,

    [Description("in")]
    Inside,
    [Description("out")]
    Outside
}