using MediatR;
using NetSimpleAuctioneer.API.Features.Shared;

namespace NetSimpleAuctioneer.API.Features.Auctions.PlaceBid
{
    public record PlaceBidCommand(Guid AuctionId, string BidderEmail, decimal BidAmount) : IRequest<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>>;
    public record PlaceBidCommandResult(Guid BidId);


    public class PlaceBidHandler(IPlaceBidRepository placeBidRepository, ILogger<PlaceBidHandler> logger) : IRequestHandler<PlaceBidCommand, SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>>
    {
        public async Task<SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
        {
            // Check if the auction exists
            var auctionInformation = await placeBidRepository.GetAuctionInformation(request.AuctionId, cancellationToken);
            if (auctionInformation == null)
            {
                logger.LogWarning("Auction with ID {AuctionId} not found.", request.AuctionId);
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.AuctionNotFound);
            }

            // Check if the auction is closed
            if (auctionInformation.EndDate.HasValue)
            {
                logger.LogWarning("Auction with ID {AuctionId} has already closed.", request.AuctionId);
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.AuctionAlreadyClosed);
            }

            // Check if the bid amount is too low
            if (request.BidAmount < auctionInformation.MinimumBidAmount)
            {
                logger.LogWarning("Bid amount of {BidAmount} is too low for auction with ID {AuctionId}.", request.BidAmount, request.AuctionId);
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.BidAmountTooLow);
            }

            // Check if there is an existing higher bid
            var existingBid = await placeBidRepository.GetHighestBidForAuctionAsync(request.AuctionId, cancellationToken);
            if (existingBid != null)
            {
                if (existingBid.BidAmount >= request.BidAmount)
                {
                    logger.LogWarning("Existing higher bid of {BidAmount} found for auction with ID {AuctionId}.", existingBid.BidAmount, request.AuctionId);
                    return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.ExistingHigherBid);
                }

                if (request.BidderEmail.ToLowerInvariant() == existingBid.BiddersEmail.ToLowerInvariant())
                {
                    logger.LogWarning("The current highest bid for  auction with ID {AuctionId} already belongs to bidder with email {Email}", request.AuctionId, request.BidderEmail);
                    return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(PlaceBidErrorCode.BidderHasHigherBid);
                }
            }

            // Place the bid
            var bidResult = await placeBidRepository.PlaceBidAsync(request.AuctionId, request.BidderEmail, request.BidAmount, cancellationToken);

            if (bidResult.HasError)
                return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Failure(bidResult.Error!.Value);

            // Return success with bid details
            logger.LogInformation("Bid placed successfully for auction with ID {AuctionId}, bidder {BidderEmail}.", request.AuctionId, request.BidderEmail);
            return SuccessOrError<PlaceBidCommandResult, PlaceBidErrorCode>.Success(bidResult.Result);
        }
    }

    public class BidderHasHigherBidException() : Exception { }
}
