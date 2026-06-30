public class AnswerResult
{
    public string Answer { get; set; } = "";
    public List<string> Sources { get; set; } = new();
    public bool WasRefusal { get; set; }
}
