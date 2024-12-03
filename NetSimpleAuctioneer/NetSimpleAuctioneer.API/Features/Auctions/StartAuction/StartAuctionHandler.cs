using MediatR;
using Microsoft.Extensions.Options;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Database;
using Npgsql;

namespace NetSimpleAuctioneer.API.Features.Auctions.StartAuction
{
    public record StartAuctionCommand(Guid VehicleId) : IRequest<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>;
    public record StartAuctionCommandResult(Guid AuctionId);

    public class StartAuctionHandler(IStartAuctionRepository startAuctionRepository, IOptions<ConnectionStrings> connectionStrings) : IRequestHandler<StartAuctionCommand, SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>>
    {
        public async Task<SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>> Handle(StartAuctionCommand request, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(connectionStrings.Value.AuctioneerDBConnectionString);

            var vehicleExistsResult = await startAuctionRepository.VehicleExistsAsync(connection, request.VehicleId, cancellationToken);

            if (vehicleExistsResult.HasError)
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(vehicleExistsResult.Error!.Value);
            if (vehicleExistsResult.Result == false)
                return SuccessOrError<StartAuctionCommandResult, StartAuctionErrorCode>.Failure(StartAuctionErrorCode.InvalidVehicle);

            var result = await startAuctionRepository.StartAuctionAsync(request.VehicleId, cancellationToken);

            return result;
        }
    }
}
