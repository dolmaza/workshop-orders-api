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
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrdersDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add a database context using an in-memory database for testing
            services.AddDbContext<OrdersDbContext>(options =>
            {
                options.UseInMemoryDatabase($"InMemoryDbForTesting_{Guid.NewGuid()}");
            });

            // Create the schema in the database
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}