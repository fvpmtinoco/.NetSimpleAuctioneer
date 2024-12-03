using AutoFixture;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Vehicles.Search;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using Polly;

namespace NetSimpleAuctioneer.UnitTests.Features.Vehicles
{
    public class SearchVehiclesShould
    {
        private readonly Mock<ILogger<SearchRepository>> mockLogger;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;
        private readonly Mock<IDatabaseConnection> mockDbConnection;
        private readonly Mock<IOptions<ConnectionStrings>> mockConnectionStrings;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Fixture fixture;
        private readonly Mock<IVehicleService> mockVehicleService;
        private readonly Mock<ISearchRepository> mockSearchRepository;

        public SearchVehiclesShould()
        {
            // Mock the logger
            mockLogger = new Mock<ILogger<SearchRepository>>();

            // Mock the VehicleService
            mockVehicleService = new Mock<IVehicleService>();

            // Mock the VehicleRepository
            mockSearchRepository = new Mock<ISearchRepository>();

            // Mock the database connection
            mockDbConnection = new Mock<IDatabaseConnection>();

            // Mock the IOptions<ConnectionStrings>
            mockConnectionStrings = new Mock<IOptions<ConnectionStrings>>();
            mockConnectionStrings.Setup(x => x.Value).Returns(new ConnectionStrings
            {
                AuctioneerDBConnectionString = "Host=localhost;Port=5432;Database=AuctioneerDB;Username=postgres;Password=postgres"
            });

            // Directly create and mock concrete AsyncPolicy (e.g., RetryPolicy)
            mockRetryPolicy = Policy.NoOpAsync();  // This is a simple NoOp policy
            mockCircuitBreakerPolicy = Policy.NoOpAsync();

            // Mock IPolicyProvider
            mockPolicyProvider = new Mock<IPolicyProvider>();

            // Set up the mock to return concrete policies
            mockPolicyProvider.Setup(x => x.GetRetryPolicyWithoutConcurrencyException()).Returns(mockRetryPolicy);
            mockPolicyProvider.Setup(x => x.GetCircuitBreakerPolicy()).Returns(mockCircuitBreakerPolicy);

            fixture = new Fixture();
        }

        [Fact]
        public async Task SearchVehiclesAsyncShouldReturnVehiclesWhenSearchIsSuccessful()
        {
            // Arrange
            var vehicle = fixture.Create<SearchVehicleResult>();
            var pageNumber = 1;
            var pageSize = 10;

            mockDbConnection.Setup(conn => conn.QueryAsync<SearchVehicleResult>(It.IsAny<CommandDefinition>())).ReturnsAsync([vehicle]);

            var repository = new SearchRepository(mockLogger.Object, mockPolicyProvider.Object, mockConnectionStrings.Object, mockDbConnection.Object);

            // Act
            var result = await repository.SearchVehiclesAsync(vehicle.Manufacturer, vehicle.Model, vehicle.Year, vehicle.VehicleType, pageNumber, pageSize, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SearchVehiclesShouldReturnEmptyWhenNoVehiclesMatchCriteria()
        {
            // Arrange
            var vehicle = fixture.Create<SearchVehicleResult>();
            var pageNumber = 1;
            var pageSize = 10;

            mockDbConnection.Setup(conn => conn.QueryAsync<SearchVehicleResult>(It.IsAny<CommandDefinition>()))
                .ReturnsAsync([]);

            var repository = new SearchRepository(mockLogger.Object, mockPolicyProvider.Object, mockConnectionStrings.Object, mockDbConnection.Object);

            // Act
            var result = await repository.SearchVehiclesAsync("NonExistentManufacturer", "NonExistentModel", 9999, VehicleType.Sedan, pageNumber, pageSize, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchVehiclesShouldApplyPaginationCorrectly()
        {
            // Arrange
            var vehicles = fixture.CreateMany<SearchVehicleResult>(25).ToList();
            var pageNumber = 2;
            var pageSize = 10;

            // Mock the QueryAsync method to return the vehicles - return only the vehicles for the current page
            mockDbConnection.Setup(conn => conn.QueryAsync<SearchVehicleResult>(It.IsAny<CommandDefinition>()))
                .ReturnsAsync(vehicles.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList());

            var repository = new SearchRepository(mockLogger.Object, mockPolicyProvider.Object, mockConnectionStrings.Object, mockDbConnection.Object);

            // Act 
            var result = await repository.SearchVehiclesAsync(vehicles[0].Manufacturer, vehicles[0].Model, vehicles[0].Year, vehicles[0].VehicleType, pageNumber, pageSize, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().HaveCount(pageSize);
            result.Result.Should().Contain(vehicles.Skip((pageNumber - 1) * pageSize).Take(pageSize));
        }

        [Fact]
        public async Task SearchVehiclesShouldReturnResultsWhenNullParameterIsPassed()
        {
            // Arrange
            var vehicle = fixture.Create<SearchVehicleResult>();
            var pageNumber = 1;
            var pageSize = 10;

            mockDbConnection.Setup(conn => conn.QueryAsync<SearchVehicleResult>(It.IsAny<CommandDefinition>()))
                .ReturnsAsync([vehicle]);

            var repository = new SearchRepository(mockLogger.Object, mockPolicyProvider.Object, mockConnectionStrings.Object, mockDbConnection.Object);

            // Act
            var result = await repository.SearchVehiclesAsync(null, null, null, null, pageNumber, pageSize, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().NotBeEmpty();
            result.Result.Should().Contain(vehicle);
        }

        [Fact]
        public async Task HandleShouldReturnInvalidYearWhenVehicleYearIsInvalid()
        {
            // Arrange
            var query = new SearchVehicleQuery(VehicleType.Hatchback, "Toyota", "Corolla", 2020, 1, 10);

            // Mock the IVehicleService to return false for the year validation
            mockVehicleService.Setup(service => service.IsVehicleYearValid(It.IsAny<int>())).Returns(false);

            // Create the handler with the mocked dependencies
            var handler = new SearchHandler(mockSearchRepository.Object, mockVehicleService.Object);

            // Act
            var result = await handler.Handle(query, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(SearchVehicleErrorCode.InvalidYear);
        }

        [Fact]
        public async Task HandleShouldReturnSearchResultsWhenSearchIsSuccessful()
        {
            // Arrange
            var vehicle = fixture.Create<SearchVehicleResult>();
            var query = new SearchVehicleQuery(VehicleType.Hatchback, "Toyota", "Corolla", 2020, 1, 10);

            mockVehicleService.Setup(service => service.IsVehicleYearValid(It.IsAny<int>())).Returns(true);

            mockSearchRepository.Setup(repo => repo.SearchVehiclesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<VehicleType?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Success([vehicle]));

            var handler = new SearchHandler(mockSearchRepository.Object, mockVehicleService.Object);

            // Act
            var result = await handler.Handle(query, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().Contain(vehicle);
        }

        [Fact]
        public async Task Handle_ShouldReturnError_WhenSearchRepositoryFails()
        {
            // Arrange
            var query = new SearchVehicleQuery(VehicleType.Hatchback, "Toyota", "Corolla", 2020, 1, 10);

            // Mock the IVehicleService to return true for the year validation
            mockVehicleService.Setup(service => service.IsVehicleYearValid(It.IsAny<int>())).Returns(true);

            // Mock the SearchRepository to return an error
            mockSearchRepository.Setup(repo => repo.SearchVehiclesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<VehicleType?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Failure(SearchVehicleErrorCode.InternalError));

            // Create the handler with the mocked dependencies
            var handler = new SearchHandler(mockSearchRepository.Object, mockVehicleService.Object);

            // Act
            var result = await handler.Handle(query, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(SearchVehicleErrorCode.InternalError);
        }

        [Fact]
        public async Task Handle_ShouldNotCallRepository_WhenVehicleYearIsInvalid()
        {
            // Arrange
            var query = new SearchVehicleQuery(VehicleType.Hatchback, "Toyota", "Corolla", 2020, 1, 10);

            mockVehicleService.Setup(service => service.IsVehicleYearValid(It.IsAny<int>())).Returns(false);

            var handler = new SearchHandler(mockSearchRepository.Object, mockVehicleService.Object);

            // Act
            var result = await handler.Handle(query, It.IsAny<CancellationToken>());

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(SearchVehicleErrorCode.InvalidYear);
            // Ensure the repository was not called
            mockSearchRepository.Verify(repo => repo.SearchVehiclesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<VehicleType?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
