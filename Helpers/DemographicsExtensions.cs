public static class DemographicsExtensions
{
    public static Demographics ToDemographics(this CreatePollDemographicsDto dto)
    {
        return new Demographics
        {
            Title = dto.Title,
            Description = dto.Description,
            AnswerType = dto.AnswerType,
            Options = dto.Options?.Select(option => option.ToAnswerOption()).ToList() ?? new List<AnswerOption>()
        };
    }
}