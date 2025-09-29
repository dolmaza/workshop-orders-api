using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OrdersApi.Data;

namespace OrdersApi.IntegrationTests;

public class OrdersApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing OrdersDbContext and its options registrations
            var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<OrdersDbContext>)
                                              || d.ServiceType == typeof(OrdersDbContext)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Use a fixed in-memory database name so all contexts share the same in-memory store
            var inMemoryDbName = "InMemoryDbForTesting";

            // Add a database context using an in-memory database for testing
            services.AddDbContext<OrdersDbContext>(options =>
            {
                options.UseInMemoryDatabase(inMemoryDbName);
            });

            // Build the provider and ensure the database schema is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}