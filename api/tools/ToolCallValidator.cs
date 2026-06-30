using System.Text.Json;

public class ToolCallParseResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public CreateTicketRequest? Request { get; set; }
}

public static class ToolCallValidator
{
    public static ToolCallParseResult ParseAndValidateCreateTicket(string rawArgumentsJson)
    {
        CreateTicketRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<CreateTicketRequest>(
                rawArgumentsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            return new ToolCallParseResult
            {
                IsValid = false,
                ErrorMessage = $"Model produced malformed arguments: {ex.Message}"
            };
        }

        if (request is null)
        {
            return new ToolCallParseResult { IsValid = false, ErrorMessage = "Empty tool arguments." };
        }

        var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
        var validDepartments = new[] { "IT", "HR", "Finance" };

        if (string.IsNullOrWhiteSpace(request.Title))
            return new ToolCallParseResult { IsValid = false, ErrorMessage = "Title is required but missing." };

        if (!validPriorities.Contains(request.Priority))
            return new ToolCallParseResult { IsValid = false, ErrorMessage = $"Invalid priority '{request.Priority}'." };

        if (!validDepartments.Contains(request.Department))
            return new ToolCallParseResult { IsValid = false, ErrorMessage = $"Invalid department '{request.Department}'." };

        return new ToolCallParseResult { IsValid = true, Request = request };
    }
}
