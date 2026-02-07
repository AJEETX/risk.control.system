using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;

namespace risk.control.system.StartupExtensions;

public static class DatabaseServiceExtension
{
    public static IServiceCollection AddDatastoreServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")
            ));
        }
        else
        {
            var connectionString = "Data Source=" + Environment.GetEnvironmentVariable("COUNTRY") + "_" + configuration.GetConnectionString("Database");
            services.AddDbContext<ApplicationDbContext>(options =>
                                    options.UseSqlite(connectionString,
                    sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        }

        services.AddHangfire(config => config.UseMemoryStorage());
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
            options.Queues = new[] { "default", "emails", "critical" };
        });

        return services;
    }
}