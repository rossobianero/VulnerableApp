using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HashEndpoint_ReturnsMd5Hash()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/hash?input=hello");
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync();
        Assert.Contains("5d41402abc4b2a76b9719d911017c592", content.ToLower());
    }

    [Fact]
    public async Task SearchEndpoint_SqlInjectionLikeBehavior()
    {
        var client = _factory.CreateClient();
        // Normal query
        var resp = await client.GetAsync("/search?username=admin");
        resp.EnsureSuccessStatusCode();
        var normal = await resp.Content.ReadAsStringAsync();
        Assert.Contains("admin", normal);

        // Attempt a SQLi-like input (will not actually exploit here, but endpoint is vulnerable pattern-wise)
        var resp2 = await client.GetAsync("/search?username=admin'%20OR%20'1'='1");
        resp2.EnsureSuccessStatusCode();
        var injected = await resp2.Content.ReadAsStringAsync();
        // The vulnerable behavior will return results or possibly an empty JSON array, assert call succeeds
        Assert.NotNull(injected);
    }

    [Fact]
    public async Task ParseEndpoint_ParsesJson_WithNewtonsoft()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{\"name\":\"demo-test\"}", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync("/parse", content);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("demo-test", body);
    }
}
