using System.Net;
using System.Net.Http.Json;
using CakeGandom.Dtos;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CakeGandom.Tests;

public class AdminApiTests:IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public AdminApiTests(TestWebAppFactory factory)
    {
        _factory=factory;
        _client=factory.CreateClient();
        using var scope=factory.Services.CreateScope();
        var cfg=scope.ServiceProvider.GetRequiredService<IConfiguration>();
        _apiKey=cfg["Admin:ApiKey"]??"test-key";
    }

    private void SetKey()
    {
        _client.DefaultRequestHeaders.Remove("X-Api-Key");
        _client.DefaultRequestHeaders.Add("X-Api-Key",_apiKey);
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var resp=await _client.GetAsync("/api/bot/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_WithoutKey_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Remove("X-Api-Key");
        var resp=await _client.GetAsync("/api/admin/menu/tree");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_MenuTree_WithKey_ShouldReturnOk()
    {
        SetKey();
        var resp=await _client.GetAsync("/api/admin/menu/tree");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_AddButton_ThenMenuTree_ShouldContainNewTitle()
    {
        SetKey();
        var dto=new AddButtonDto
        {
            Level=1,
            ParentId=null,
            Title="تست دکمه",
            Position=1,
            IsSuperOnly=false,
            CallbackData=null
        };
        var addResp=await _client.PostAsJsonAsync("/api/admin/menu/button",dto);
        addResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var treeResp=await _client.GetAsync("/api/admin/menu/tree");
        var tree=await treeResp.Content.ReadFromJsonAsync<Dictionary<string,string>>();
        tree.Should().NotBeNull();
        tree!["tree"].Should().Contain("تست دکمه");
    }
}