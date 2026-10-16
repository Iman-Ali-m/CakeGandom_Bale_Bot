using CakeGandom.Dtos;
using CakeGandom.Services;
using Microsoft.AspNetCore.Mvc;

namespace CakeGandom.Controllers;

[ApiController]
[Route("api/bot")]
public class BotController:ControllerBase
{
    private readonly UpdateHandler _handler;
    private readonly IConfiguration _cfg;
    private readonly ILogger<BotController> _logger;

    public BotController(UpdateHandler handler,IConfiguration cfg,ILogger<BotController> logger)
    {
        _handler=handler;
        _cfg=cfg;
        _logger=logger;
    }

    [HttpPost("webhook/{secret?}")]
    public async Task<IActionResult> Webhook([FromBody] Update update,string? secret=null,CancellationToken ct=default)
    {
        var expected=_cfg["Bot:WebhookSecret"];
        if(!string.IsNullOrEmpty(expected)&&expected!=secret)
        {
            _logger.LogWarning("Invalid webhook secret");
            return Unauthorized();
        }
        try
        {
            await _handler.HandleAsync(update,ct);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"Webhook handling failed");
        }
        return Ok();
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new{ok=true,time=DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
    }
}