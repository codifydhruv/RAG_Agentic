public interface ITicketingTool
{
    Task<CreateTicketResult> CreateTicketAsync(CreateTicketRequest request);
}
