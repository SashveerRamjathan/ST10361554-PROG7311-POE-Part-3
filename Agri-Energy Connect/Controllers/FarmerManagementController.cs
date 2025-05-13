using Agri_Energy_Connect.Services;
using DataContextAndModels.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Agri_Energy_Connect.Controllers
{
    public class FarmerManagementController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FarmerManagementController> _logger;

        public FarmerManagementController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FarmerManagementController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: FarmerManagement
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching all farmers from API.");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync("/api/FarmerAccount/farmer/all");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var farmers = JsonConvert.DeserializeObject<List<ApplicationUser>>(jsonString);

                    if (farmers == null || farmers.Count == 0)
                    {
                        _logger.LogWarning("No farmers found.");
                        TempData["ErrorMessage"] = "No farmers found.";

                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                        return View(new List<ApplicationUser>());
                    }

                    _logger.LogInformation($"Successfully retrieved {farmers.Count} farmers from API.");

                    return View(farmers);
                }

                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to retrieve farmers. Status: {response.StatusCode}, Response: {errorResponse}");

                TempData["ErrorMessage"] = "Failed to retrieve farmers. Please try again later.";

                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View(new List<ApplicationUser>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching farmers from API.");
                TempData["ErrorMessage"] = "An unexpected error occurred.";

                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View(new List<ApplicationUser>());
            }
        }
    }
}
