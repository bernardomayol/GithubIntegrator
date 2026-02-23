
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gh.Tests;

public class MinimalApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public MinimalApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Auth_Token_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/token", new { Username = "bernardo" });
        response.EnsureSuccessStatusCode();
    }
}
