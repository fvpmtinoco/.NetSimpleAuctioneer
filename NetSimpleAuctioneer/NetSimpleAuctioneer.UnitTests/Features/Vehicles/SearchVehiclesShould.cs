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
        private readonly Mock<ILogger<SearchRepository>> _loggerRepositoryMock;
        private readonly Mock<ILogger<SearchService>> _loggerServiceMock;
        private readonly Mock<IOptions<ConnectionStrings>> _connectionStringsMock;
        private readonly Mock<IDatabaseConnection> _dbConnectionMock;
        private readonly SearchRepository _repository;
        private readonly SearchService _service;
        private readonly AsyncPolicy mockRetryPolicy;
        private readonly AsyncPolicy mockCircuitBreakerPolicy;
        private readonly Fixture fixture;
        private readonly Mock<IPolicyProvider> mockPolicyProvider;


        public SearchVehiclesShould()
        {
            _loggerRepositoryMock = new Mock<ILogger<SearchRepository>>();
            _loggerServiceMock = new Mock<ILogger<SearchService>>();
            _connectionStringsMock = new Mock<IOptions<ConnectionStrings>>();
            _dbConnectionMock = new Mock<IDatabaseConnection>();

            //// Mock the IOptions<ConnectionStrings>
            _connectionStringsMock = new Mock<IOptions<ConnectionStrings>>();
            _connectionStringsMock.Setup(x => x.Value).Returns(new ConnectionStrings
            {
                AuctioneerDBConnectionString = "Host=localhost;Port=5432;Database=AuctioneerDB;Username=postgres;Password=postgres"
            });

            // Instantiate the repository
            _repository = new SearchRepository(
                _loggerRepositoryMock.Object,
                _connectionStringsMock.Object,
                _dbConnectionMock.Object);

            // Directly create and mock concrete AsyncPolicy (e.g., RetryPolicy)
            mockRetryPolicy = Policy.NoOpAsync();  // This is a simple NoOp policy
            mockCircuitBreakerPolicy = Policy.NoOpAsync();

            // Mock IPolicyProvider
            mockPolicyProvider = new Mock<IPolicyProvider>();

            // Set up the mock to return concrete policies
            mockPolicyProvider.Setup(x => x.GetRetryPolicyWithoutConcurrencyException()).Returns(mockRetryPolicy);
            mockPolicyProvider.Setup(x => x.GetCircuitBreakerPolicy()).Returns(mockCircuitBreakerPolicy);

            _service = new SearchService(_repository, _loggerServiceMock.Object, mockPolicyProvider.Object);

            fixture = new Fixture();
        }

        [Theory]
        [InlineData(1899)]
        [InlineData(2050)]
        public async Task HandleShouldReturnInvalidYearErrorWhenYearIsInvalid(int year)
        {
            // Arrange
            var searchHandler = new SearchHandler(_service);

            var query = new SearchVehicleQuery(VehicleType.Hatchback, "Toyota", "Corolla", year, 1, 10);

            // Act
            var result = await searchHandler.Handle(query, CancellationToken.None);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(SearchVehicleErrorCode.InvalidYear); // Assert the specific error
        }

        [Fact]
        public async Task SearchVehiclesAsyncShouldReturnResultsWhenNoExceptionOccurs()
        {
            // Arrange
            var mockSearchRepository = new Mock<ISearchRepository>();
            var manufacturer = "Toyota";
            var model = "Corolla";
            var year = 2020;
            var vehicleType = VehicleType.Sedan;
            var pageNumber = 1;
            var pageSize = 10;
            var cancellationToken = CancellationToken.None;

            var expectedResult = SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>
                .Success(new List<SearchVehicleResult> { new SearchVehicleResult { Id = fixture.Create<Guid>(), Manufacturer = "Toyota", Model = "Corolla", Year = 2020 } });

            mockSearchRepository.Setup(s => s.SearchVehiclesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<VehicleType?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(expectedResult);

            var _service = new SearchService(mockSearchRepository.Object, _loggerServiceMock.Object, mockPolicyProvider.Object);

            // Act
            var result = await _service.SearchVehiclesAsync(manufacturer, model, year, vehicleType, pageNumber, pageSize, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().NotBeNull();
            result.Result.Should().ContainSingle();
            result.Result.First().Manufacturer.Should().Be("Toyota");
        }


        [Fact]
        public async Task SearchVehiclesAsyncShouldReturnFailureWhenExceptionOccurs()
        {
            // Arrange
            var mockSearchRepository = new Mock<ISearchRepository>(); // Mock the ISearchRepository
            var mockLogger = new Mock<ILogger<SearchService>>(); // Mock ILogger
            var mockPolicyProvider = new Mock<IPolicyProvider>(); // Mock IPolicyProvider

            var searchService = new SearchService(mockSearchRepository.Object, mockLogger.Object, mockPolicyProvider.Object); // Inject the mocks into the service

            var manufacturer = "Toyota";
            var model = "Corolla";
            var year = 2020;
            var vehicleType = VehicleType.SUV;
            var pageNumber = 1;
            var pageSize = 10;
            var cancellationToken = CancellationToken.None;

            mockSearchRepository.Setup(s => s.SearchVehiclesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<VehicleType?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                 .ThrowsAsync(new Exception("Internal error")); // Simulate an exception in repository

            // Act
            var result = await searchService.SearchVehiclesAsync(manufacturer, model, year, vehicleType, pageNumber, pageSize, cancellationToken);

            // Assert
            result.HasError.Should().BeTrue();
            result.Error.Should().Be(SearchVehicleErrorCode.InternalError); // Assert that it returns an internal error
        }

        [Fact]
        public async Task SearchVehiclesAsync_ShouldReturnEmpty_WhenNoVehiclesMatchQuery()
        {
            // Arrange
            var mockDbConnection = new Mock<IDatabaseConnection>();

            var manufacturer = "Toyota";
            var model = "Corolla";
            var year = 2020;
            var vehicleType = VehicleType.Truck;
            var pageNumber = 1;
            var pageSize = 10;
            var cancellationToken = CancellationToken.None;

            var queryResult = new List<SearchVehicleResult>(); // Simulate no results returned

            mockDbConnection.Setup(db => db.QueryAsync<SearchVehicleResult>(It.IsAny<CommandDefinition>()))
                             .ReturnsAsync(queryResult); // Setup mock db connection to return empty list

            var _repository = new SearchRepository(_loggerRepositoryMock.Object, _connectionStringsMock.Object, mockDbConnection.Object);

            // Act
            var result = await _repository.SearchVehiclesAsync(manufacturer, model, year, vehicleType, pageNumber, pageSize, cancellationToken);

            // Assert
            result.HasError.Should().BeFalse();
            result.Result.Should().BeEmpty(); // Assert no results found
        }

        [Fact]
        public async Task SearchServiceShouldReturnPaginatedResultsWhenPageNumberAndPageSizeAreProvided()
        {
            // Arrange
            var manufacturer = "Toyota";
            var model = "Corolla";
            var year = 2020;
            var vehicleType = VehicleType.SUV;
            var pageNumber = 2;
            var pageSize = 10;

            // Mock the repository to return a paginated result
            var mockResults = new List<SearchVehicleResult>
            {
                new SearchVehicleResult { Id = fixture.Create<Guid>(), Manufacturer = manufacturer, Model = model, Year = year },
                new SearchVehicleResult { Id = fixture.Create<Guid>(), Manufacturer = manufacturer, Model = model, Year = year }
            };

            var searchRepositoryMock = new Mock<ISearchRepository>();
            searchRepositoryMock.Setup(r => r.SearchVehiclesAsync(manufacturer, model, year, vehicleType, pageNumber, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Success(mockResults));

            var service = new SearchService(searchRepositoryMock.Object, _loggerServiceMock.Object, mockPolicyProvider.Object);
            var command = new SearchVehicleQuery(vehicleType, manufacturer, model, year, pageNumber, pageSize);

            var searchHandler = new SearchHandler(service);

            // Act
            var result = await searchHandler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.HasError.Should().BeFalse();
            result.Result.Should().HaveCount(mockResults.Count);
            result.Result.First().Manufacturer.Should().Be(manufacturer);
            result.Result.First().Model.Should().Be(model);
        }

        [Fact]
        public async Task SearchService_ShouldReturnAllVehicles_WhenManufacturerAndModelAreNull()
        {
            // Arrange
            string? manufacturer = null;
            string? model = null;
            var year = 2020;
            var vehicleType = VehicleType.Truck;
            var pageNumber = 1;
            var pageSize = 10;

            var mockResults = new List<SearchVehicleResult>
            {
                new SearchVehicleResult { Id = It.IsAny<Guid>(), Manufacturer = "Toyota", Model = "Corolla", Year = year },
                new SearchVehicleResult { Id =  It.IsAny<Guid>(), Manufacturer = "Honda", Model = "Civic", Year = year }
            };

            var searchRepositoryMock = new Mock<ISearchRepository>();
            searchRepositoryMock.Setup(r => r.SearchVehiclesAsync(manufacturer, model, year, vehicleType, pageNumber, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(SuccessOrError<IEnumerable<SearchVehicleResult>, SearchVehicleErrorCode>.Success(mockResults));

            var service = new SearchService(searchRepositoryMock.Object, _loggerServiceMock.Object, mockPolicyProvider.Object);
            var command = new SearchVehicleQuery(vehicleType, manufacturer, model, year, pageNumber, pageSize);

            var searchHandler = new SearchHandler(service);

            // Act
            var result = await searchHandler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.HasError.Should().BeFalse();
            result.Result.Should().HaveCount(mockResults.Count);
            result.Result.First().Manufacturer.Should().Be("Toyota");
            result.Result.First().Model.Should().Be("Corolla");
        }
    }
}
