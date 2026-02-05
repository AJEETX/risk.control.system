using AspNetCoreHero.ToastNotification.Extensions;

using Hangfire;

using Microsoft.AspNetCore.Authentication;

using risk.control.system.Middleware;
using risk.control.system.Permission;

namespace risk.control.system.StartupExtensions;

public static class AppUseExtenstion
{
    public static async Task<WebApplication> UseServices(this WebApplication app, IConfiguration configuration)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new BasicAuthAuthorizationFilter() }
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        await Seeds.DatabaseSeed.SeedDatabase(app);

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (!ctx.Context.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=2592000"; // 30 days
                }
            }
        });

        app.UseRouting();
        app.UseCors();
        app.UseCookiePolicy();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<SecurityMiddleware>(configuration["HttpStatusErrorCodes"]);
        app.UseMiddleware<RequirePasswordChangeMiddleware>();
        app.UseMiddleware<CookieConsentMiddleware>();
        app.UseMiddleware<WhitelistListMiddleware>();
        app.UseMiddleware<LicensingMiddleware>();
        app.UseMiddleware<UpdateUserLastActivityMiddleware>();

        app.UseNotyf();
        app.UseFileServer();
        app.UseRateLimiter();
        app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == 401 &&
                !context.User.Identity.IsAuthenticated &&
            !context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Headers.ContainsKey("Authorization"))
            {
                await context.ChallengeAsync();
            }
        });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Dashboard}/{action=Index}/{id?}")
            .RequireRateLimiting("PerUserOrIP");

        //RecurringJob.AddOrUpdate<IHangfireJobService>(
        //    "clean-failed-jobs",
        //    job => job.CleanFailedJobs(),
        //    Cron.Hourly // Runs every hour
        //);

        int sessionTimeoutMinutes = int.Parse(configuration["SESSION_TIMEOUT_SEC"]) / 60;
        //RecurringJob.AddOrUpdate<IdleUserService>(
        //    "check-idle-users",
        //    service => service.CheckIdleUsers(),
        //    $"*/{sessionTimeoutMinutes} * * * *"); // Check every 5 minutes
        return app;
    }
}