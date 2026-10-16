using CakeGandom.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CakeGandom.Tests;

public class TestWebAppFactory:WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection=new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.ConfigureServices(services=>
        {
            var toRemove=services.Where(d=>
                d.ServiceType==typeof(DbContextOptions<BotDbContext>)||
                d.ServiceType==typeof(DbContextOptions)||
                d.ServiceType==typeof(BotDbContext)||
                (d.ServiceType.IsGenericType&&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration"))
            ).ToList();
            foreach(var d in toRemove) services.Remove(d);
            services.AddDbContext<BotDbContext>(o=>o.UseSqlite(_connection));
        });
        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host=base.CreateHost(builder);
        using(var scope=host.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<BotDbContext>();
            db.Database.EnsureCreated();
        }
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if(disposing) _connection.Dispose();
    }
}