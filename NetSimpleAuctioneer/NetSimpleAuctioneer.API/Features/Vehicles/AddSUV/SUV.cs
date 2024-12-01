﻿using NetSimpleAuctioneer.API.Features.Vehicles.Shared;

namespace NetSimpleAuctioneer.API.Features.Vehicles.AddSUV
{
    public record SUV(Guid Id, string Manufacturer, string Model, int Year, decimal StartingBid, int NumberOfSeats) : IVehicle;
}