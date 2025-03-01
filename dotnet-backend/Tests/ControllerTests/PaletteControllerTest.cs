using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.ControllerTests
{
    // Using IClassFixture<T> to share a single WebApplicationFactory<Program>
    public class PaletteControllerTest(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        // Initialize _client directly using the primary constructor parameter
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Get_PaletteAssets_ReturnsSuccessAndExpectedResponse()
        {
            // Optionally add an Authorization header if your API requires it.
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer dummy-token");

            // Act: call the API endpoint.
            var response = await _client.GetAsync("/palette/assets");

            // Assert: verify status and content type.
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }
    }
}