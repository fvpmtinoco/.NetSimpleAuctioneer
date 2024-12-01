using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetSimpleAuctioneer.API
{
    public class ActionNameOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if the controller action has a custom ActionName
            var actionName = context.ApiDescription.ActionDescriptor.AttributeRouteInfo?.Name;

            if (!string.IsNullOrEmpty(actionName))
            {
                // Modify the path in Swagger based on the ActionName
                var routeTemplate = context.ApiDescription.RelativePath;
                if (routeTemplate.Contains("addSedan"))
                {
                    operation.Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Add Sedan" } };
                }
                else if (routeTemplate.Contains("addHatchback"))
                {
                    operation.Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Add Hatchback" } };
                }
            }
        }
    }
}
