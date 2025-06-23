using System.Net.Http.Headers;

/*
    * Code Attribution
    * Purpose: Providing an extension method for HttpClient to add JWT Bearer tokens from HTTP cookies,
    *          enabling authenticated API requests by extracting the "AuthToken" cookie and setting
    *          the Authorization header appropriately in ASP.NET Core applications.
    * Author: Inspired by common ASP.NET Core middleware and HttpClient usage patterns
    * Date Accessed: 23 June 2025
    * Source Concepts:
    * - https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.defaultrequestheaders
    * - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/cookies
    * - https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt
 */


/*
    * Class: HttpClientExtensions
    * Description: Provides an extension method for HttpClient to easily add a JWT token from cookies in an ASP.NET Core request.
    * This allows API requests to be authenticated using the "AuthToken" cookie as a Bearer token.
 */

namespace Agri_Energy_Connect.Services
{
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Adds a Bearer JWT token from the "AuthToken" cookie in the current request to the HttpClient's Authorization header.
        /// If the cookie is missing or empty, the Authorization header is not set.
        /// </summary>
        /// <param name="client">The HttpClient to add the Authorization header to.</param>
        /// <param name="request">The current HTTP request, which may contain the "AuthToken" cookie.</param>
        public static void AddJwtFromCookies(this HttpClient client, HttpRequest request)
        {
            if (request.Cookies.TryGetValue("AuthToken", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}