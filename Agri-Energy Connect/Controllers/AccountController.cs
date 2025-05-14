using Agri_Energy_Connect.Services;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Agri_Energy_Connect.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
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
                // Log the model state errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogError(error.ErrorMessage);
                }

                return View(model);
            }

            var client = _httpClientFactory.CreateClient("AgriEnergyAPI");

            var jsonContent = JsonConvert.SerializeObject(model);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/auth/login", content);

            // Log that the request was sent
            _logger.LogInformation($"Login request sent to API with email: {model.Email}");

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseBody);

                // if the response is null or doesn't contain expected properties, log the error
                if (responseObj == null)
                {
                    ModelState.AddModelError(string.Empty, "Login failed: Invalid response from server.");
                    _logger.LogError("Login failed: Invalid response from server.");
                    return View(model);
                }

                if (responseObj.token == null || responseObj.id == null)
                {
                    ModelState.AddModelError(string.Empty, "Login failed: Missing token or user ID.");
                    _logger.LogError("Login failed: Missing token or user ID.");
                    return View(model);
                }

                string token = responseObj.token.ToString();
                string userId = responseObj.id.ToString();

                // Log the received token
                _logger.LogInformation($"Received token: {token}");
                _logger.LogInformation($"User ID: {userId}");

                // Decode token
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Create claims from JWT
                var claims = jwtToken.Claims.ToList();

                // Optional: add extra claims
                claims.Add(new Claim("AccessToken", token));
                claims.Add(new Claim(ClaimTypes.Name, model.Email));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

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

                // Log successful login
                _logger.LogInformation($"User {model.Email} logged in successfully.");

                // redirect user according to their role
                var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                if (roleClaim != null)
                {
                    var role = roleClaim.Value;
                    
                    if (role == "Farmer")
                    {
                        return RedirectToAction("FarmerIndex", "Home");
                    }
                    else if (role == "Employee")
                    {
                        return RedirectToAction("EmployeeIndex", "Home");
                    }
                }

                return RedirectToAction("Index", "Home");

            }

            // Read error message from API if available
            var errorResponse = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Login failed: {errorResponse}");
            _logger.LogError($"Login failed: {errorResponse}");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            Response.Cookies.Delete("AuthToken"); // Clear the AuthToken cookie

            // Log successful logout
            _logger.LogInformation("User logged out successfully.");

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Account/RegisterFarmer
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public IActionResult RegisterFarmer()
        {
            return View();
        }

        // POST: Account/RegisterFarmer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> RegisterFarmer(FarmerRegisterViewModel model)
        {
            _logger.LogInformation($"Farmer registration attempt for email: {model.EmailAddress}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Farmer registration failed due to invalid model state for email: {model.EmailAddress}");
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var jsonContent = JsonConvert.SerializeObject(model);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug($"Sending registration request to API for email: {model.EmailAddress}");

                var response = await client.PostAsync("/api/auth/register/farmer", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Farmer registered successfully via API for email: {model.EmailAddress}");
                    TempData["SuccessMessage"] = "Farmer registered successfully.";
                    return RedirectToAction("Index", "FarmerManagement");
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning($"Farmer registration failed with BadRequest for email: {model.EmailAddress}. Response: {responseContent}");

                    try
                    {
                        // Attempt to deserialize API validation errors
                        var apiErrors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);

                        // if successful, add errors to ModelState
                        if (apiErrors == null)
                        {
                            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                            return View(model);
                        }

                        foreach (var error in apiErrors)
                        {
                            foreach (var msg in error.Value)
                            {
                                ModelState.AddModelError(error.Key, msg);
                            }
                        }
                    }
                    catch
                    {
                        // If not in dictionary format, treat it as plain error
                        ModelState.AddModelError(string.Empty, responseContent);
                    }
                }
                else
                {
                    _logger.LogError($"Unexpected API error during farmer registration. Status: {response.StatusCode}, Response: {responseContent}");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred while processing your request.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occurred while registering farmer with email: {model.EmailAddress}");

                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }

            return View(model);
        }
    }
}
