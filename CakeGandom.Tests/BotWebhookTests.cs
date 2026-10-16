using System.Net;
using System.Net.Http.Json;
using CakeGandom.Dtos;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CakeGandom.Tests;

public class BotWebhookTests:IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly string _secret;

    public BotWebhookTests(TestWebAppFactory factory)
    {
        _factory=factory;
        _client=factory.CreateClient();
        using var scope=factory.Services.CreateScope();
        var cfg=scope.ServiceProvider.GetRequiredService<IConfiguration>();
        _secret=cfg["Bot:WebhookSecret"]??"change-me-secret";
    }

    [Fact]
    public async Task Webhook_WithWrongSecret_ShouldReturnUnauthorized()
    {
        var update=new Update{UpdateId=1};
        var resp=await _client.PostAsJsonAsync("/api/bot/webhook/wrong-secret",update);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_WithValidSecret_ShouldReturnOk()
    {
        var update=new Update
        {
            UpdateId=2,
            Message=new Message
            {
                MessageId=1,
                From=new UserFrom{Id=990001,FirstName="Test",Username="tester"},
                Chat=new Chat{Id=990001,Type="private"},
                Date=DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Text="/start"
            }
        };
        var resp=await _client.PostAsJsonAsync($"/api/bot/webhook/{_secret}",update);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_EmptyUpdate_ShouldStillReturnOk()
    {
        var update=new Update{UpdateId=3};
        var resp=await _client.PostAsJsonAsync($"/api/bot/webhook/{_secret}",update);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}