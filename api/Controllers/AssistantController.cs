[ApiController]
[Route("api/assistant")]
[Authorize]
public class AssistantController : ControllerBase
{
    private readonly IAnswerService _answerService;

    public AssistantController(IAnswerService answerService)
    {
        _answerService = answerService;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "Question cannot be empty." });
        }

        var result = await _answerService.AnswerAsync(request.Question);
        return Ok(result);
    }
}

public class AskRequest
{
    public string Question { get; set; } = "";
}
