using CakeGandom.Data;
using CakeGandom.Services;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BotDbContext>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddHttpClient<BaleApiClient>();
builder.Services.AddScoped<PhoneProtector>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<UpdateHandler>();
builder.Services.AddHostedService<BotPollingService>();
builder.Services.AddControllers();

var app=builder.Build();

if(!app.Environment.IsEnvironment("Testing"))
{
    using(var scope=app.Services.CreateScope())
    {
        var db=scope.ServiceProvider.GetRequiredService<BotDbContext>();
        db.Database.Migrate();
    }
    var mode=app.Configuration["Bot:Mode"]??"polling";
    if(mode=="webhook")
    {
        var url=app.Configuration["Bot:WebhookUrl"];
        var secret=app.Configuration["Bot:WebhookSecret"];
        if(!string.IsNullOrWhiteSpace(url))
        {
            using var scope=app.Services.CreateScope();
            var api=scope.ServiceProvider.GetRequiredService<BaleApiClient>();
            var full=$"{url.TrimEnd('/')}/api/bot/webhook/{secret}";
            await api.SetWebhook(full);
        }
    }
    else if(mode=="polling")
    {
        using var scope=app.Services.CreateScope();
        var api=scope.ServiceProvider.GetRequiredService<BaleApiClient>();
        await api.DeleteWebhook();
    }
}

app.MapControllers();
app.Run();

public partial class Program{}