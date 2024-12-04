namespace NetSimpleAuctioneer.API.Application
{
    public static class AutoInjectoionExtensions
    {
        public static IServiceCollection AddAutoInjectorCustom(this IServiceCollection services)
        {
            // Scans the calling assembly and registers all classes whose names end with "Service" or "Repository"
            // as services or repositories with their matching interfaces. 
            return services.Scan(scan =>
            scan.FromCallingAssembly()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") || type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces() // Registers each class with every interface it implements, making it injectable under any of those interfaces
            .WithScopedLifetime());
        }
    }
}
