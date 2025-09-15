public static class AnswerOptionExtensions
{
    public static AnswerOption ToAnswerOption(this AnswerOptionDto option)
    {
        return new AnswerOption
        {
            Text = option.Text,
            Score = option.Score
        };
    }

    public static AnswerOption ToAnswerOption(this string option)
    {
        return new AnswerOption
        {
            Text = option,
            Score = 0
        };
    }

    public static AnswerOptionDto ToAnswerOptionDto(this AnswerOption option)
    {
        return new AnswerOptionDto
        {
            Text = option.Text,
            Score = option.Score
        };
    }
}