using System.Net.Http.Headers;

namespace Agri_Energy_Connect.Services
{
    public static class HttpClientExtensions
    {
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
