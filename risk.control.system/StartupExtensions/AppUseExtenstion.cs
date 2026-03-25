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
        app.UseForwardedHeaders();
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Index");
            app.UseHsts();
        }
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new BasicAuthAuthorizationFilter() }
        });

        app.UseHttpsRedirection();

        app.UseWebOptimizer();

        app.UseResponseCompression();

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

        app.UseMiddleware<SecurityMiddleware>();
        app.UseMiddleware<CookieConsentMiddleware>();

        app.UseRateLimiter();

        app.UseNotyf();
        app.UseFileServer();
        app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == 401 &&
                !context.User.Identity!.IsAuthenticated &&
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
        await Seeds.DatabaseSeed.SeedDatabase(app);

        return app;
    }
}