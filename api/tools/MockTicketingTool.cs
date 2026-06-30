
public class MockTicketingTool : ITicketingTool
{
    private static readonly List<CreateTicketResult> _tickets = new();
    private static int _counter = 1000;

    public Task<CreateTicketResult> CreateTicketAsync(CreateTicketRequest request)
    {
        ValidateRequest(request);

        var result = new CreateTicketResult
        {
            TicketId = $"TCK-{_counter++}",
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _tickets.Add(result);
        Console.WriteLine($"[MOCK TICKET CREATED] {result.TicketId} | {request.Priority} | {request.Title}");

        return Task.FromResult(result);
    }

    private void ValidateRequest(CreateTicketRequest request)
    {
        var validPriorities = new[] { "Low", "Medium", "High", "Critical" };

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Ticket title cannot be empty.");

        if (!validPriorities.Contains(request.Priority))
            throw new ArgumentException($"Invalid priority '{request.Priority}'. Must be one of: {string.Join(", ", validPriorities)}");
    }
}
