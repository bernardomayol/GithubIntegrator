using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gh.ControllersApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/github")]
    public class GitHubController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private readonly IHttpClientFactory _factory;
        public GitHubController(IHttpClientFactory factory) => _factory = factory;

        [Microsoft.AspNetCore.Mvc.HttpGet("search")]
        public async Task<IActionResult> Search([Microsoft.AspNetCore.Mvc.FromQuery] string q, [Microsoft.AspNetCore.Mvc.FromQuery] int page, [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize)
        {
            var client = _factory.CreateClient("github");
            var resp = await client.GetAsync($"search/repositories?q={Uri.EscapeDataString(q)}&page={page}&per_page={pageSize}");
            var json = await resp.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
    }
}

