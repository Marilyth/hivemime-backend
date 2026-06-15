public class CandidateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsCustom { get; set; }
    public List<string> MediaKeys { get; set; }
}
