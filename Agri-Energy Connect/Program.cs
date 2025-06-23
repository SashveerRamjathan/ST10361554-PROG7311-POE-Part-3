using Microsoft.AspNetCore.Authentication.Cookies;

/*
    * Code Attribution
    * Purpose: Configuring ASP.NET Core MVC application with cookie-based authentication,
    *          HTTP client setup, and environment-specific middleware for secure, scalable web hosting.
    * Author: Inspired by official Microsoft ASP.NET Core documentation and templates
    * Date Accessed: 23 June 2025
    * Source Concepts:
    * - Cookie Authentication: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie
    * - HttpClient Factory: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
    * - Environment-based middleware configuration: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments
    * - MVC Routing: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
 */


/*
    * Program: Main entry point for Agri_Energy_Connect MVC application.
    * Description: Configures services and middleware for controllers, authentication (using cookies), 
    * authorization, HTTP client base address, and environment-specific error handling.
    * Uses configuration for JWT and API settings, sets up default MVC routing, and runs the app.
 */

namespace Agri_Energy_Connect
{
    public class Program
    {
        /// <summary>
        /// Main entry point. Configures and runs the MVC web application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add MVC controllers with views to the service container
            builder.Services.AddControllersWithViews();

            // Retrieve JWT and API settings from configuration
            var jwtSettings = builder.Configuration.GetSection("JwtSettings") ?? throw new InvalidOperationException("JwtSettings not found");
            var apiSettings = builder.Configuration.GetSection("ApiSettings") ?? throw new InvalidOperationException("ApiSettings not found");

            // Register a named HttpClient for API calls with the configured base address
            builder.Services.AddHttpClient("AgriEnergyAPI", client =>
            {
                client.BaseAddress = new Uri(apiSettings["BaseUrl"]!);
            });

            // Configure cookie-based authentication with settings from configuration
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(Convert.ToDouble(jwtSettings["ExpireHours"]));
                    options.SlidingExpiration = true;
                });

            // Add authorization services
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. For production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Configure MVC default route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}