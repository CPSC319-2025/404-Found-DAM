using Xunit;
using NSubstitute;
using Core.Services;
using Core.Interfaces;
using Core.Dtos;

namespace Project.Tests;

public class ProjectServicesUnitTests
{
    private readonly IProjectService _projectService;
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();

    public ProjectServicesUnitTests()
    {
        _projectService = new ProjectService(_projectRepository);
    }

    [Fact]
    public async Task SubmitAssetstoDb_Successful() 
    {
        // Arrange
        int projectID = 123;
        List<int> blobIDs = new List<int> {1, 2, 3};
        int submitterID = 1;
        // Mock SubmitAssetstoDb to return true when it is called, regardless of values of its int arguments
        _projectRepository.SubmitAssetstoDb(Arg.Any<int>(), Arg.Any<List<int>>(), Arg.Any<int>())
            .Returns(Task.FromResult((new List<int> { 1, 2, 3 }, new List<int> {})));

        // Act
        var response = await _projectService.SubmitAssets(projectID, blobIDs, submitterID);
    
        // Assert
        Assert.NotNull(response);
        Assert.IsType<SubmitAssetsRes>(response); // Assert the data type
        Assert.NotEqual(default(DateTime), response.submittedAt);

        // Verify that the SubmitAssetstoDb method of the mocked repository was called exactly once, 
        // with the provided projectID and blobIDs.
        await _projectRepository.Received(1).SubmitAssetstoDb(projectID, blobIDs, submitterID);
    }
}


