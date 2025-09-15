using System.ComponentModel.DataAnnotations.Schema;

public class AnswerOption : ModelBase
{
    public string Text { get; set; }
    public int Score { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }
}