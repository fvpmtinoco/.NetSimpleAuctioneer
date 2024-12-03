namespace NetSimpleAuctioneer.API.Features.Auctions.Shared
{
    public class AuctionNotFoundException() : Exception { }
    public class AuctionAlreadyClosedException() : Exception { }
    public class BidAmountTooLowException() : Exception { }
}
