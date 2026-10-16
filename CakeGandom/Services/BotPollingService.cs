using CakeGandom.Dtos;

namespace CakeGandom.Services;

public class BotPollingService:BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<BotPollingService> _logger;
    private long _lastActivityUnix;

    public BotPollingService(IServiceProvider sp,IConfiguration cfg,ILogger<BotPollingService> logger)
    {
        _sp=sp;
        _cfg=cfg;
        _logger=logger;
        _lastActivityUnix=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mode=_cfg["Bot:Mode"]??"polling";
        if(mode!="polling")
        {
            _logger.LogInformation("Polling disabled. Mode={Mode}",mode);
            return;
        }
        int normalInterval=int.TryParse(_cfg["Bot:PollingIntervalSeconds"],out var i)?i:3;
        int idleInterval=int.TryParse(_cfg["Bot:IdlePollingIntervalSeconds"],out var j)?j:60;
        int idleMinutes=int.TryParse(_cfg["Bot:IdleMinutesThreshold"],out var m)?m:20;
        long? offset=null;
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope=_sp.CreateScope();
                var api=scope.ServiceProvider.GetRequiredService<BaleApiClient>();
                var handler=scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                var updates=await api.GetUpdates(offset,30,stoppingToken);
                if(updates.Count>0) _lastActivityUnix=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                foreach(var u in updates)
                {
                    offset=u.UpdateId+1;
                    try
                    {
                        await handler.HandleAsync(u,stoppingToken);
                        _lastActivityUnix=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex,"Handler failed for update {Id}",u.UpdateId);
                    }
                }
            }
            catch(Exception ex)
            {
                if(stoppingToken.IsCancellationRequested) break;
                _logger.LogError(ex,"Polling loop error");
                await Task.Delay(5000,stoppingToken);
            }
            var idleSeconds=DateTimeOffset.UtcNow.ToUnixTimeSeconds()-_lastActivityUnix;
            var delay=idleSeconds>=idleMinutes*60?idleInterval:normalInterval;
            await Task.Delay(TimeSpan.FromSeconds(delay),stoppingToken);
        }
    }
}