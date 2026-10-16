using CakeGandom.Data;
using CakeGandom.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CakeGandom.Tests;

public class RegistrationServiceTests:IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly BotDbContext _db;
    private readonly UserService _users;
    private readonly RegistrationService _reg;
    private readonly PhoneProtector _protector;

    public RegistrationServiceTests()
    {
        _conn=new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options=new DbContextOptionsBuilder<BotDbContext>().UseSqlite(_conn).Options;
        _db=new BotDbContext(options);
        _db.Database.EnsureCreated();
        var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            {"SuperUsers:MainUserId","1000"},
            {"Encryption:Key","0123456789abcdef0123456789abcdef"}
        }).Build();
        _users=new UserService(_db,cfg);
        _protector=new PhoneProtector(cfg);
        _reg=new RegistrationService(_db,_protector);
    }

    [Fact]
    public async Task FullFlow_ShouldCompleteAndEncryptPhone()
    {
        await _users.RegisterOrUpdateAsync("7000",null,"Sara","Karimi");
        await _reg.ResetAsync("7000");
        (await _reg.ProcessAsync("7000","Sara")).ok.Should().BeTrue();
        (await _reg.ProcessAsync("7000","Karimi")).ok.Should().BeTrue();
        (await _reg.ProcessAsync("7000","09123456789")).ok.Should().BeTrue();
        (await _reg.ProcessAsync("7000","@sara_bale")).ok.Should().BeTrue();
        (await _reg.IsCompleteAsync("7000")).Should().BeTrue();
        var user=await _users.GetUserAsync("7000");
        var enc=user!.Profile!.PhoneEncrypted!;
        _protector.Decrypt(enc).Should().Be("09123456789");
    }

    [Fact]
    public async Task InvalidFirstName_ShouldReturnFalse()
    {
        await _users.RegisterOrUpdateAsync("7001",null,"A","B");
        await _reg.ResetAsync("7001");
        var r=await _reg.ProcessAsync("7001","A");
        r.ok.Should().BeFalse();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}