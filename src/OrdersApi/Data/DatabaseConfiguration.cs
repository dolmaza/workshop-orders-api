using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;

namespace OrdersApi.Data;

public static class DatabaseConfiguration
{
    public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Use in-memory database for development/testing
            services.AddDbContext<OrdersDbContext>(options =>
                options.UseInMemoryDatabase("OrdersDb"));
        }
        else
        {
            // Use PostgreSQL for production
            services.AddDbContext<OrdersDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
    }
}