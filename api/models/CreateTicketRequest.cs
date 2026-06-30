
public class CreateTicketRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Department { get; set; } = "IT";    // who it routes to
}
