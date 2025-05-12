
using DataContextAndModels.Data;
using Agri_Energy_Connect_API.Services;
using DataContextAndModels.Enums;
using DataContextAndModels.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Agri_Energy_Connect_API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var jwtSettings = builder.Configuration.GetSection("JwtSettings") ?? throw new InvalidOperationException("JwtSettings not found");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlite(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

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

            builder.Services.AddScoped<TokenService>();

            builder.Services.AddAuthorization();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            // Seed user roles before app runs
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await SeedRolesAsync(services); // Seed user roles
                await SeedAdminUserAsync(services); // Seed employee user
            }

            app.Run();
        }

        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in Enum.GetValues<RolesEnum>())
            {
                var roleName = role.ToString();

                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

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
