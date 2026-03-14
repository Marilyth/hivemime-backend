public static class ValueOperatorExtensions
{
    /// <summary>
    /// Converts a ValueOperator enum value to its corresponding symbol representation.
    /// </summary>
    /// <param name="valueOperator">The ValueOperator to convert.</param>
    /// <returns>The symbol representation of the ValueOperator.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported ValueOperator is provided.</exception>
    public static string OperatorToSymbol(this ValueOperator valueOperator)
    {
        return valueOperator switch
        {
            ValueOperator.Equals => "=",
            ValueOperator.Greater => ">",
            ValueOperator.GreaterEquals => ">=",
            ValueOperator.Less => "<",
            ValueOperator.LessEquals => "<=",
            _ => throw new ArgumentOutOfRangeException(nameof(valueOperator), $"Unsupported operator: {valueOperator}")
        };
    }
}