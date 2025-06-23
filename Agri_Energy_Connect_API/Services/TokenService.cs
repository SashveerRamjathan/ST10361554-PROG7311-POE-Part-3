using DataContextAndModels.Enums;
using DataContextAndModels.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

/*
    * Code Attribution
    * Purpose: Generating JWT tokens for stateless user authentication in ASP.NET Core applications
    * Author: Microsoft Documentation & Community Samples (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn & GitHub - ASP.NET Core Security Authentication Guide
    * URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt
 */

/*
    * This file defines the TokenService class, responsible for generating JWT tokens for authenticated users.
    * The service utilizes application configuration for JWT settings and encodes user identity and role information
    * into the token claims. This is commonly used in ASP.NET Core applications for stateless authentication.
 */

namespace Agri_Energy_Connect_API.Services
{
    /// <summary>
    /// Service for creating JWT tokens for application users.
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructs a new TokenService with application configuration dependency.
        /// </summary>
        /// <param name="config">Application configuration to retrieve JWT settings.</param>
        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Creates a JWT token for the specified user and role.
        /// </summary>
        /// <param name="user">The application user for whom the token is generated.</param>
        /// <param name="role">The role assigned to the user, to be included as a claim.</param>
        /// <returns>A string representation of the generated JWT token.</returns>
        public string CreateToken(ApplicationUser user, RolesEnum role)
        {
            // Define claims for the user: subject, unique name identifier, and role.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, role.ToString())
            };

            // Create a symmetric security key using the secret from configuration.
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!));

            // Generate signing credentials using the security key and HMAC-SHA256 algorithm.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Construct the JWT token, including issuer, audience, claims, expiration, and signing credentials.
            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(_config["JwtSettings:ExpireHours"])),
                signingCredentials: creds
            );

            // Serialize the JWT token to a string and return it.
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}