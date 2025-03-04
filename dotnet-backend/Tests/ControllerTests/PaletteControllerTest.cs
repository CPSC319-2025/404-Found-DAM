using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
namespace Tests.ControllerTests
{
    public class PaletteControllerTest(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Get_PaletteAssets_ReturnsSuccessAndExpectedResponse()
        {
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer dummy-token");

            var response = await _client.GetAsync("/palette/assets");

            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task Post_MediaUpload_ReturnsSuccessAndExpectedResponse()
        {
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer dummy-token");

            // Create a multipart form data content
            using var formContent = new MultipartFormDataContent();
            
            // Add the file
            string currentDirectory = Path.GetDirectoryName(typeof(Core.Class1).Assembly.Location) ?? string.Empty;
            string projectRootDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "../../../"));
            string zipFilePath = Path.Combine(projectRootDirectory, "sample", "image-zip.zst");
    
    
            // Read the zip file bytes
            byte[] fileBytes = await File.ReadAllBytesAsync(zipFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            formContent.Add(fileContent, "file", Path.GetFileName(zipFilePath));
            
            // Add the DTO properties as form fields
            formContent.Add(new StringContent("TestName"), "Name");
            formContent.Add(new StringContent("TestType"), "Type");

            var response = await _client.PostAsync("/palette/upload", formContent);

            // Assert: verify status and content type.
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
            
            // Optionally verify the response content
            var content = await response.Content.ReadAsStringAsync();
            // Assert content contains expected values
        }

    }
}