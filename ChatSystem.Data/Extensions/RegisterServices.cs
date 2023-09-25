using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Data.Extensions;

public static class RegisterServices
{
    public static void RegisterDataServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContextFactory<EntityFrameworkContext>(options =>
        {
            options.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=root;Database=ChatSystem;Include Error Detail=true");
            //options.EnableSensitiveDataLogging(); // Optional, for debugging
        });
    }
}