using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.StartupExtensions;

public static class DatabaseServiceExtension
{
    public static IServiceCollection AddDatastoreServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            var connectionString = EnvHelper.Get("DefaultConnection");
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString),
                ServiceLifetime.Scoped,
                ServiceLifetime.Singleton);
        }
        else
        {
            var connectionString = "Data Source=" + EnvHelper.Get("COUNTRY") + "_" + configuration.GetConnectionString("Database");
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
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