using CakeGandom.Data;
using CakeGandom.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CakeGandom.Tests;

public class MenuServiceTests:IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly BotDbContext _db;
    private readonly MenuService _menu;

    public MenuServiceTests()
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
        var users=new UserService(_db,cfg);
        _menu=new MenuService(_db,users);
    }

    [Fact]
    public async Task AddButton_AtMiddle_ShouldShiftOthers()
    {
        var before=await _menu.GetAllButtonsByLevelAsync(1,null);
        int countBefore=before.Count;
        await _menu.AddButtonAsync(1,null,"جدید",2,false,null);
        var after=await _menu.GetAllButtonsByLevelAsync(1,null);
        after.Count.Should().Be(countBefore+1);
        after.First(x=>x.Position==2).Title.Should().Be("جدید");
    }

    [Fact]
    public async Task DeleteButton_ShouldShiftBack()
    {
        var before=await _menu.GetAllButtonsByLevelAsync(1,null);
        var target=before.First(x=>x.Position==2);
        int id=target.Id;
        await _menu.DeleteButtonAsync(id);
        var after=await _menu.GetAllButtonsByLevelAsync(1,null);
        after.Count.Should().Be(before.Count-1);
        after.Any(x=>x.Id==id).Should().BeFalse();
    }

    [Fact]
    public async Task MoveButton_Down_ShouldSwapPositions()
    {
        var list=await _menu.GetAllButtonsByLevelAsync(1,null);
        var first=list.First(x=>x.Position==1);
        var second=list.First(x=>x.Position==2);
        await _menu.MoveButtonAsync(first.Id,2);
        var after=await _menu.GetAllButtonsByLevelAsync(1,null);
        after.First(x=>x.Id==first.Id).Position.Should().Be(2);
        after.First(x=>x.Id==second.Id).Position.Should().Be(1);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}