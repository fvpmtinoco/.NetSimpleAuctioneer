using MediatR;
using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Application;
using NetSimpleAuctioneer.API.Application.Behaviors;
using NetSimpleAuctioneer.API.Application.Policies;
using NetSimpleAuctioneer.API.Infrastructure.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Unchase.Swashbuckle.AspNetCore.Extensions.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// PostgreSQL connection string
builder.Services.AddDbContext<AuctioneerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuctioneerDBConnectionString")));

// Register MediatR services from the current assembly
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));

// Register the PolicyProvider as IPolicyProvider (Polly policies)
builder.Services.AddScoped<IPolicyProvider, PolicyProvider>();

// Inject classes whose names end with "Service" or "Repository"
builder.Services.AddAutoInjectorCustom();

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