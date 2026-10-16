using CakeGandom.Data;
using CakeGandom.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CakeGandom.Tests;

public class UserServiceTests:IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly BotDbContext _db;
    private readonly UserService _users;

    public UserServiceTests()
    {
        _conn=new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options=new DbContextOptionsBuilder<BotDbContext>().UseSqlite(_conn).Options;
        _db=new BotDbContext(options);
        _db.Database.EnsureCreated();
        var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            {"SuperUsers:MainUserId","1000"},
            {"SuperUsers:AdminIds:0","2000"},
            {"Encryption:Key","0123456789abcdef0123456789abcdef"}
        }).Build();
        _users=new UserService(_db,cfg);
    }

    [Fact]
    public void IsSuperUser_ShouldMatchConfig()
    {
        _users.IsSuperUser("1000").Should().BeTrue();
        _users.IsSuperUser("9999").Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_ShouldMatchConfig()
    {
        _users.IsAdmin("2000").Should().BeTrue();
        _users.IsAdmin("1000").Should().BeFalse();
    }

    [Fact]
    public async Task RegisterOrUpdate_NewUser_ShouldReturnTrue()
    {
        var isNew=await _users.RegisterOrUpdateAsync("5000","baleuser","Ali","Ahmadi");
        isNew.Should().BeTrue();
        var user=await _users.GetUserAsync("5000");
        user.Should().NotBeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}