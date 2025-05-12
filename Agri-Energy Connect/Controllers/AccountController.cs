using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Agri_Energy_Connect.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("AgriEnergyAPI");

            var jsonContent = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var tokenObj = JsonConvert.DeserializeObject<dynamic>(responseBody);

                if (tokenObj.token == null)
                {
                    ModelState.AddModelError(string.Empty, "Login failed: Token not received.");
                    return View(model);
                }

                string token = tokenObj.token.ToString();

                // Decode token
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Create claims from JWT
                var claims = jwtToken.Claims.ToList();

                // Optional: add extra claims
                claims.Add(new Claim("AccessToken", token));
                claims.Add(new Claim(ClaimTypes.Name, model.Email));

                // Create identity and principal
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Sign in user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["JwtSettings:ExpireHours"]))
                    });

                // Also store token in cookie (for API usage)
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.Now.AddHours(Convert.ToDouble(_configuration["JwtSettings:ExpireHours"]))
                };

                Response.Cookies.Append("AuthToken", token, cookieOptions);

                return RedirectToAction("Index", "Home");

            }

            // Read error message from API if available
            var errorResponse = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Login failed: {errorResponse}");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("AuthToken"); // Clear the AuthToken cookie
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
