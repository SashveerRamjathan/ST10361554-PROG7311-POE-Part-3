using Agri_Energy_Connect_API.Services;
using DataContextAndModels.Data;
using DataContextAndModels.Enums;
using DataContextAndModels.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

/*
    * Code Attribution
    * Purpose: ASP.NET Core application startup configuration including Identity, EF Core, JWT authentication, and role/user seeding
    * Author: Microsoft Docs & Community Samples (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn - Configure ASP.NET Core Identity, JWT, and EF Core
    * URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
 */

/*
    * Program: Main entry point for Agri_Energy_Connect_API ASP.NET Core application.
    * Description: Configures services, middleware, and dependency injection for the web API, including
    * Entity Framework Core, Identity, authentication/authorization with JWT, and Swagger/OpenAPI.
    * Seeds roles and a default admin user at startup.
 */

namespace Agri_Energy_Connect_API
{
    public class Program
    {
        /// <summary>
        /// Main entry point. Configures and runs the web application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Obtain the connection string from configuration
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Retrieve JWT settings from configuration
            var jwtSettings = builder.Configuration.GetSection("JwtSettings")
                              ?? throw new InvalidOperationException("JwtSettings not found");

            // Configure Entity Framework Core with SQLite
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));

            // Add developer exception filter for database errors
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Configure ASP.NET Core Identity with default options
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Add JWT authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtBearer";
                options.DefaultChallengeScheme = "JwtBearer";
            })
            .AddJwtBearer("JwtBearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
                };
            });

            // Register TokenService for dependency injection
            builder.Services.AddScoped<TokenService>();

            // Add authorization and controllers
            builder.Services.AddAuthorization();
            builder.Services.AddControllers();

            // Swagger/OpenAPI configuration
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            // Map controller endpoints
            app.MapControllers();

            // Seed user roles and the default employee user before the app runs
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await SeedRolesAsync(services); // Seed user roles
                await SeedAdminUserAsync(services); // Seed employee user
            }

            app.Run();
        }

        /// <summary>
        /// Seeds all roles defined in RolesEnum into the IdentityRole store if they do not already exist.
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in Enum.GetValues<RolesEnum>())
            {
                var roleName = role.ToString();

                // Create role if it does not exist
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        /// <summary>
        /// Seeds a default admin (employee) user if it does not exist, and assigns the Employee role.
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var defaultEmail = "employee@agrienergy.com"; // Default email for the employee
            var defaultPassword = "Password123!"; // Default password for the employee
            var fullName = "John Doe"; // Default full name of the employee
            var phoneNumber = "083-678-6545"; // Default phone number for the employee

            // Check if the user already exists
            var user = await userManager.FindByEmailAsync(defaultEmail);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = defaultEmail,
                    Email = defaultEmail,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    Address = "123 Agri Energy St, Greenfield" // Add a default address if necessary
                };

                var createResult = await userManager.CreateAsync(user, defaultPassword);

                if (!createResult.Succeeded)
                {
                    throw new Exception("Failed to create the user.");
                }

                // Assign the employee role
                if (!await userManager.IsInRoleAsync(user, RolesEnum.Employee.ToString()))
                {
                    await userManager.AddToRoleAsync(user, RolesEnum.Employee.ToString());
                }
            }
        }
    }
}