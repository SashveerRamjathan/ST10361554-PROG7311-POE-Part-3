using Agri_Energy_Connect.Services;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

/*
    * Code Attribution
    * Purpose: Implementation of role-based authorization, API consumption using HttpClientFactory with JWT token authentication,
    *          and CRUD operations with detailed logging and robust error handling in ASP.NET Core MVC.
    * Concepts Used: Secure API calls with JWT, role-based access control ("Employee"), deserialization with Newtonsoft.Json,
    *                TempData/ViewData for user feedback, and standard MVC patterns for data management.
    * Author: Adapted from Microsoft and Newtonsoft.Json official documentation and best practices.
    * Date Accessed: 23 June 2025
    * Sources:
    * - Microsoft Learn: Call a web API from ASP.NET Core MVC
    *   https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie#call-a-web-api-from-an-aspnet-core-app
    * - Newtonsoft.Json Documentation
    *   https://www.newtonsoft.com/json/help/html/DeserializeObject.htm
*/


/*
    * Controller: FarmerManagementController
    * Description: This controller manages CRUD operations for Farmer accounts via the Agri-Energy API.
    * Only users with the "Employee" role are authorized to access these endpoints.
    * The controller provides actions for listing, viewing, updating, and deleting farmer accounts.
    * Logging is included for traceability and error diagnostics.
 */

namespace Agri_Energy_Connect.Controllers
{
    public class FarmerManagementController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FarmerManagementController> _logger;

        /// <summary>
        /// Constructor for FarmerManagementController.
        /// </summary>
        public FarmerManagementController(IHttpClientFactory httpClientFactory, ILogger<FarmerManagementController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Lists all farmers via API.
        /// </summary>
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

        /// <summary>
        /// Displays details for a specific farmer.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Details(string id)
        {
            _logger.LogInformation($"Fetching details for farmer with ID: {id}");

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Farmer ID is null or empty.");
                TempData["ErrorMessage"] = "Invalid farmer ID.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);
                var response = await client.GetAsync($"/api/FarmerAccount/farmer/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Farmer with ID: {id} not found.");
                    TempData["ErrorMessage"] = "Farmer not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to farmer details.");
                    TempData["ErrorMessage"] = "Unauthorized access.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var farmer = JsonConvert.DeserializeObject<ApplicationUser>(jsonString);

                    if (farmer == null)
                    {
                        _logger.LogWarning($"Farmer with ID: {id} not found.");
                        TempData["ErrorMessage"] = "Farmer not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogInformation($"Successfully retrieved details for farmer with ID: {id}");

                    var farmerViewModel = new FarmerUpdateViewModel
                    {
                        Id = farmer.Id,
                        FullName = farmer.FullName!,
                        EmailAddress = farmer.Email!,
                        PhoneNumber = farmer.PhoneNumber!,
                        Address = farmer.Address!
                    };

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    return View(farmerViewModel);
                }
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to retrieve farmer. Status: {response.StatusCode}, Response: {errorResponse}");
                TempData["ErrorMessage"] = "Failed to retrieve farmer. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching farmer details.");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Fetches a farmer for update.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Update(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Farmer ID is null or empty.");
                TempData["ErrorMessage"] = "Invalid farmer ID.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"Fetching farmer with ID: {id} for update.");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/FarmerAccount/farmer/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Farmer with ID: {id} not found.");
                    TempData["ErrorMessage"] = "Farmer not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to farmer details.");
                    TempData["ErrorMessage"] = "Unauthorized access.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var farmer = JsonConvert.DeserializeObject<ApplicationUser>(jsonString);

                    if (farmer == null)
                    {
                        _logger.LogWarning($"Farmer with ID: {id} not found.");
                        TempData["ErrorMessage"] = "Farmer not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogInformation($"Successfully retrieved farmer with ID: {id} for update.");

                    var farmerViewModel = new FarmerUpdateViewModel
                    {
                        Id = farmer.Id,
                        FullName = farmer.FullName!,
                        EmailAddress = farmer.Email!,
                        PhoneNumber = farmer.PhoneNumber!,
                        Address = farmer.Address!
                    };

                    return View(farmerViewModel);
                }
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to retrieve farmer. Status: {response.StatusCode}, Response: {errorResponse}");

                TempData["ErrorMessage"] = "Failed to retrieve farmer. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching farmer for update.");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handles farmer update POST.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Update(FarmerUpdateViewModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("FarmerUpdateViewModel is null.");
                TempData["ErrorMessage"] = "Invalid model.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(model.Id))
            {
                _logger.LogWarning("Farmer ID is null or empty.");
                TempData["ErrorMessage"] = "Invalid farmer ID.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"Updating farmer with ID: {model.Id}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for updating farmer with ID: {model.Id}");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessage"] += error.ErrorMessage + "\n";
                }
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var jsonContent = JsonConvert.SerializeObject(model);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"/api/FarmerAccount/farmer/{model.Id}", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully updated farmer with ID: {model.Id}");
                    TempData["SuccessMessage"] = "Farmer updated successfully.";
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning($"Farmer with ID: {model.Id} not found.");
                        TempData["ErrorMessage"] = "Farmer not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Unauthorized access to update farmer.");
                        TempData["ErrorMessage"] = "Unauthorized access.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to update farmer. Status: {response.StatusCode}, Response: {errorResponse}");
                        TempData["ErrorMessage"] = "Failed to update farmer. Please try again later.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating farmer.");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(model);
            }
        }

        /// <summary>
        /// Fetches a farmer for deletion.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation($"Fetching farmer with ID: {id} for deletion.");

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Farmer ID is null or empty.");
                TempData["ErrorMessage"] = "Invalid farmer ID.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/FarmerAccount/farmer/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Farmer with ID: {id} not found.");
                    TempData["ErrorMessage"] = "Farmer not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to farmer details.");
                    TempData["ErrorMessage"] = "Unauthorized access.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var farmer = JsonConvert.DeserializeObject<ApplicationUser>(jsonString);

                    if (farmer == null)
                    {
                        _logger.LogWarning($"Farmer with ID: {id} not found.");
                        TempData["ErrorMessage"] = "Farmer not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogInformation($"Successfully retrieved farmer with ID: {id} for deletion.");

                    var farmerViewModel = new FarmerUpdateViewModel
                    {
                        Id = farmer.Id,
                        FullName = farmer.FullName!,
                        EmailAddress = farmer.Email!,
                        PhoneNumber = farmer.PhoneNumber!,
                        Address = farmer.Address!
                    };

                    return View(farmerViewModel);
                }

                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to retrieve farmer. Status: {response.StatusCode}, Response: {errorResponse}");
                TempData["ErrorMessage"] = "Failed to retrieve farmer. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching farmer for deletion.");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handles farmer deletion POST.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            _logger.LogInformation($"Deleting farmer with ID: {id}");

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Farmer ID is null or empty.");
                TempData["ErrorMessage"] = "Invalid farmer ID.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.DeleteAsync($"/api/FarmerAccount/farmer/{id}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully deleted farmer with ID: {id}");
                    TempData["SuccessMessage"] = "Farmer deleted successfully.";
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning($"Farmer with ID: {id} not found.");
                        TempData["ErrorMessage"] = "Farmer not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Unauthorized access to delete farmer.");
                        TempData["ErrorMessage"] = "Unauthorized access.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to delete farmer. Status: {response.StatusCode}, Response: {errorResponse}");
                        TempData["ErrorMessage"] = "Failed to delete farmer. Please try again later.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting farmer.");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }
        }
    }
}