[ApiController]
[Route("api/test")]
public class ToolTestController : ControllerBase
{
    private readonly ITicketingTool _ticketingTool;

    public ToolTestController(ITicketingTool ticketingTool)
    {
        _ticketingTool = ticketingTool;
    }

    [HttpPost("create-ticket")]
    public async Task<IActionResult> TestCreateTicket([FromBody] CreateTicketRequest request)
    {
        try
        {
            var result = await _ticketingTool.CreateTicketAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
