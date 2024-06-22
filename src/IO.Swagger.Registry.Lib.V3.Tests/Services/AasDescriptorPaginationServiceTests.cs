using AasxServerStandardBib.Logging;
using IO.Swagger.Models;
using IO.Swagger.Registry.Lib.V3.Models;
using IO.Swagger.Registry.Lib.V3.Services;

namespace IO.Swagger.Registry.Lib.V3.Tests.Services;

public class AasDescriptorPaginationServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IAppLogger<AasDescriptorPaginationService>> _mockLogger;
    private readonly AasDescriptorPaginationService _service;

    public AasDescriptorPaginationServiceTests()
    {
        _fixture    = new Fixture().Customize(new AutoMoqCustomization());
        _mockLogger = _fixture.Freeze<Mock<IAppLogger<AasDescriptorPaginationService>>>();
        _service    = _fixture.Create<AasDescriptorPaginationService>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new AasDescriptorPaginationService(null);

        act.Should().Throw<ArgumentNullException>()
           .WithMessage("Value cannot be null. (Parameter 'logger')");
    }

    [Fact]
    public void GetPaginatedList_WhenSourceListIsEmpty_ReturnsEmptyResult()
    {
        // Arrange
        var paginationParameters = new PaginationParameters(null, 10);
        var sourceList           = new List<AssetAdministrationShellDescriptor>();

        // Act
        var result = _service.GetPaginatedList(sourceList, paginationParameters);

        // Assert
        result.result.Should().BeEmpty();
        result.paging_metadata.cursor.Should().BeNull();
    }

    [Fact]
    public void GetPaginatedList_WhenSourceListHasElements_ReturnsPaginatedResult()
    {
        // Arrange
        var paginationParameters = new PaginationParameters("0", 2);
        var sourceList           = _fixture.CreateMany<AssetAdministrationShellDescriptor>(5).ToList();

        // Act
        var result = _service.GetPaginatedList(sourceList, paginationParameters);

        // Assert
        result.result.Should().HaveCount(2);
        result.paging_metadata.cursor.Should().Be("2");
    }

    [Fact]
    public void GetPaginatedList_WhenEndIndexExceedsSourceList_ReturnsCappedResult()
    {
        // Arrange
        var paginationParameters = new PaginationParameters("3", 5);
        var sourceList           = _fixture.CreateMany<AssetAdministrationShellDescriptor>(5).ToList();

        // Act
        var result = _service.GetPaginatedList(sourceList, paginationParameters);

        // Assert
        result.result.Should().HaveCount(2);
        result.paging_metadata.cursor.Should().BeNull();
    }

    [Fact]
    public void GetPaginationList_WhenStartIndexExceedsSourceList_LogsError()
    {
        // Arrange
        var sourceList           = _fixture.CreateMany<AssetAdministrationShellDescriptor>(5).ToList();
        var paginationParameters = new PaginationParameters("6", 5);

        // Act
        var result = _service.GetPaginatedList(sourceList, paginationParameters);

        // Assert
        _mockLogger.Verify(logger => logger.LogError(It.Is<string>(s => s.Contains("There are less elements in the retrieved list than requested pagination"))), Times.Once);
        result.result.Should().BeEmpty();
    }
}