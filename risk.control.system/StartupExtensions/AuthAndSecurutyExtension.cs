using System.Security.Claims;
using System.Text;

using AspNetCoreHero.ToastNotification.Abstractions;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Services.Common;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace risk.control.system.StartupExtensions;

public static class AuthAndSecurutyExtension
{
    public static IServiceCollection AddAuthAndSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 2. Controllers & Global Filters
        services.AddControllersWithViews(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
        })
        .AddRazorRuntimeCompilation()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        });
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = context => false;
            options.MinimumSameSitePolicy = SameSiteMode.None;
            options.Secure = CookieSecurePolicy.Always;
        });

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
        });
        authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.SignInScheme = IdentityConstants.ApplicationScheme;
            options.ClientId = configuration["AzureAd:ClientId"];
            options.ClientSecret = EnvHelper.Get("AZUREAD__CLIENTSECRET");
            options.Authority = $"https://login.microsoftonline.com/{configuration["AzureAd:TenantId"]}/v2.0";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.Scope.Clear();
            new[] { "openid", "profile", "email", "User.Read", "User.Read.All" }
                .ToList().ForEach(s => options.Scope.Add(s));

            options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = ClaimTypes.Email };

            // Cookie hardening for OIDC
            options.NonceCookie.Path = options.CorrelationCookie.Path = "/";
            options.NonceCookie.SameSite = options.CorrelationCookie.SameSite = SameSiteMode.None;
            options.NonceCookie.SecurePolicy = options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.NonceCookie.IsEssential = options.CorrelationCookie.IsEssential = true;

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = async context =>
                {
                    var azureAdService = context.HttpContext.RequestServices.GetRequiredService<IAzureAdService>();
                    var notifyService = context.HttpContext.RequestServices.GetRequiredService<INotyfService>();

                    var email = await azureAdService.ValidateAzureSignIn(context);
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        notifyService.Error("Azure AD Login error");
                        context.Response.Redirect(AppCookie.LOGIN_PATH);
                    }
                    else
                    {
                        notifyService.Success($"Welcome <b>{email}</b>, Login successful");
                        context.Response.Redirect("/Dashboard/Index");
                    }
                    context.HandleResponse();
                },
                OnRemoteFailure = context =>
                {
                    if (context.Failure?.Message.Contains("Correlation failed") == true)
                    {
                        context.Response.Redirect(AppCookie.LOGIN_PATH);
                        context.HandleResponse();
                    }
                    return Task.CompletedTask;
                }
            };
        });

        //  3️⃣ JWT Bearer authentication (API)
        authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Data"])),
                ClockSkew = TimeSpan.Zero
            };
        });

        // 4. Configure the Identity Cookie (Do this AFTER AddAuthentication)
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = AppCookie.AUTH_COOKIE_NAME;
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.None;
            options.CookieManager = new ChunkingCookieManager();
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromSeconds(double.Parse(configuration["SESSION_TIMEOUT_SEC"] ?? "900"));

            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    // API requests should return 401, not a redirect
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    //// DYNAMIC PATH LOGIC: Check if user was trying to access /Tools
                    //else if (context.Request.Path.StartsWithSegments("/Tools"))
                    //{
                    //    context.Response.Redirect("/Tools/Try" + context.Request.QueryString);
                    //}
                    else
                    {
                        context.Response.Redirect(AppCookie.LOGIN_PATH + context.Request.QueryString);
                    }
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.Redirect(AppCookie.LOGIN_PATH);
                    return Task.CompletedTask;
                },
                OnSignedIn = context =>
                {
                    var userName = context.Principal.Identity.Name;
                    var cookie = context.HttpContext.User.Claims;
                    Console.WriteLine($"Identity {userName} cookie issued");
                    return Task.CompletedTask;
                },
                OnValidatePrincipal = context =>
                {
                    Console.WriteLine("Identity cookie validated");
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = AppCookie.ANTI_FORGERY_COOKIE_NAME; // Set a custom cookie name
            options.Cookie.HttpOnly = true; // Make the cookie HttpOnly
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require secure cookies (only over HTTPS)
            options.Cookie.SameSite = SameSiteMode.None;
            options.HeaderName = "X-CSRF-TOKEN"; // Set a custom header name
            options.FormFieldName = "__RequestVerificationToken"; // Set a custom form field name
            options.SuppressXFrameOptionsHeader = false; // Enable the X-Frame-Options header
        });

        services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = @"JWT Authorization header. \r\n\r\n Enter the token in the text input below.",
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "iCheckify Utility",
                Description = "iCheckify API ",
                TermsOfService = new Uri("https://icheckify.co.in"),
                Contact = new OpenApiContact
                {
                    Name = "iCheckify Team",
                    Email = "hi@icheckify.co.in",
                    Url = new Uri("https://icheckify.co.in"),
                },
                License = new OpenApiLicense
                {
                    Name = "Use under OpenApiLicense",
                    Url = new Uri("https://icheckify.co.in"),
                }
            });
        });

        services.AddHttpContextAccessor();
        return services;
    }
}