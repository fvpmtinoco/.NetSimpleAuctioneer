using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Database;
using NetSimpleAuctioneer.API.Features.Auctions.CloseAuction;
using NetSimpleAuctioneer.API.Features.Auctions.PlaceBid;
using NetSimpleAuctioneer.API.Features.Auctions.StartAuction;
using NetSimpleAuctioneer.API.Features.Vehicles.Search;
using NetSimpleAuctioneer.API.Features.Vehicles.Shared;
using System.Reflection;
using System.Text.RegularExpressions;
using Unchase.Swashbuckle.AspNetCore.Extensions.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// PostgreSQL connection string
builder.Services.AddDbContext<AuctioneerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuctioneerDBConnectionString")));

// Get connection string from appsettings
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection(nameof(ConnectionStrings)));

// Register MediatR services from the current assembly
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

// Register IDatabaseConnection as NpgsqlDatabaseConnection
builder.Services.AddScoped<IDatabaseConnection>(provider =>
{
    var connectionString = provider.GetRequiredService<IOptions<ConnectionStrings>>().Value.AuctioneerDBConnectionString;
    return new NpgsqlDatabaseConnection(connectionString!);
});

// Register the PolicyProvider as IPolicyProvider (Polly policies)
//builder.Services.AddSingleton<IPolicyProvider, PolicyProvider>();
builder.Services.AddScoped<IPolicyProvider, PolicyProvider>();

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IStartAuctionRepository, StartAuctionRepository>();
builder.Services.AddScoped<ICloseAuctionRepository, CloseAuctionRepository>();
builder.Services.AddScoped<IPlaceBidRepository, PlaceBidRepository>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<ICloseAuctionService, CloseAuctionService>();
builder.Services.AddScoped<IPlaceBidService, PlaceBidService>();
builder.Services.AddScoped<IStartAuctionService, StartAuctionService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Swagger options
builder.Services.AddSwaggerGen(options =>
{
    // Swagger UI grouping - associate controller to a module according to its relative path 
    options.TagActionsBy(apiDescription =>
    {
        if (apiDescription.RelativePath != null)
        {
            Match match = Regex.Match(apiDescription.RelativePath, "api/(.*?)(\\.|$)");
            if (match.Success)
            {
                string extractedText = match.Groups[1].Value;
                return [extractedText];
            }
        }

        throw new InvalidOperationException($"Unable to determine module for relative path '{apiDescription.RelativePath}'");
    });

    // Show Enums descriptions - Unchase.Swashbuckle.AspNetCore.Extensions.Extensions
    options.AddEnumsWithValuesFixFilters(x =>
    {
        x.IncludeDescriptions = true;

        //https://openapi-generator.tech/docs/templating/#all-generators-core
        x.XEnumNamesAlias = "x-enum-varnames";
        x.XEnumDescriptionsAlias = "x-enum-descriptions";
    });

    // Add API documentation
    var filePath = Path.Combine(AppContext.BaseDirectory, "NetSimpleAuctioneer.API.xml");
    options.IncludeXmlComments(filePath, includeControllerXmlComments: true);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Show request/response models instead of the code example
        options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


// For Web API integration tests
public partial class Program { }