using System.ComponentModel.DataAnnotations.Schema;

public class Vote : ModelBase
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int AnswerOptionId { get; set; }
    public AnswerOption? AnswerOption { get; set; }

    public int AnswerOrder { get; set; }
}
