using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Net;




public class SearchControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    // private readonly WebApplicationFactory<Program> _factory;

    // public SearchControllerTest(WebApplicationFactory<Program> factory)
    // {
    //     _factory = factory;
    // }

    // [Fact]
    // public async Task GET_Search_Same_Query_Ten_Concurrent_Users()
    // {
    //     // Arrange
    //     int userCount = 10;
    //     int overallTimeLimitMs = 3000;
    //     string query = "market";
    //     string encodedQuery = System.Web.HttpUtility.UrlEncode(query);
    //     string url = $"/search?query={encodedQuery}";
    //     List<HttpClient> clients = new List<HttpClient>();
    //     List<Task> userTasks = new List<Task>();

    //     for (int i = 0; i < userCount; i++) 
    //     {
    //         clients.Add(_factory.CreateClient());
    //     }

    //     Stopwatch stopwatch = Stopwatch.StartNew();
        
    //     foreach (var client in clients)
    //     {
    //         userTasks.Add(Task.Run(async () =>
    //         {
    //             var response = await client.GetAsync(url);
    //             Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //         }));
    //     }

    //     // Act
    //     await Task.WhenAll(userTasks);
    //     stopwatch.Stop();

    //     // Assert
    //     long timeSpentMs = stopwatch.ElapsedMilliseconds;
    //     Assert.True(timeSpentMs <= overallTimeLimitMs, $"Elapsed time ({timeSpentMs} ms) exceeded the time limit ({overallTimeLimitMs} ms).");
    // }
}
 
