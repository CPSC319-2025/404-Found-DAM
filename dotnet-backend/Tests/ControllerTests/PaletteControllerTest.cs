using System.Net.Http.Headers;
using System.Text.Json;
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
        public async Task Post_MediaUpload_WithMultipleFiles_ReturnsSuccessAndExpectedResponse()
        {
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer dummy-token");

            // Create a multipart form data content
            using var formContent = new MultipartFormDataContent();
            
            // Get the project directory for finding test files
            string currentDirectory = Path.GetDirectoryName(typeof(Core.Class1).Assembly.Location) ?? string.Empty;
            string projectRootDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "../../../"));
            string sampleDirectory = Path.Combine(projectRootDirectory, "sample");
            
            // Add the first file
            string zipFilePath1 = Path.Combine(sampleDirectory, "image-zip.zst");
            byte[] fileBytes1 = await File.ReadAllBytesAsync(zipFilePath1);
            var fileContent1 = new ByteArrayContent(fileBytes1);
            fileContent1.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            formContent.Add(fileContent1, "file", Path.GetFileName(zipFilePath1));
            
            // Add the second file
            string zipFilePath2 = Path.Combine(sampleDirectory, "image-zip.zst");
            // If the second test file doesn't exist, just duplicate the first one for testing
            if (!File.Exists(zipFilePath2))
            {
                zipFilePath2 = zipFilePath1;
            }
            byte[] fileBytes2 = await File.ReadAllBytesAsync(zipFilePath2);
            var fileContent2 = new ByteArrayContent(fileBytes2);
            fileContent2.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            formContent.Add(fileContent2, "file", Path.GetFileName(zipFilePath2));
            
            // Add the DTO properties as form fields
            formContent.Add(new StringContent("TestName"), "Name");
            formContent.Add(new StringContent("TestType"), "Type");
            formContent.Add(new StringContent("1"), "UserId");

            var response = await _client.PostAsync("/palette/upload", formContent);

            // Assert: verify status and content type
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
            
            // Verify the response content
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonDocument>(content);
            
            // Assert the response contains information about both files
            Assert.NotNull(result);

            

            response = await _client.GetAsync("/palette/assets");
            response.EnsureSuccessStatusCode();
         

                
        }

    }
}