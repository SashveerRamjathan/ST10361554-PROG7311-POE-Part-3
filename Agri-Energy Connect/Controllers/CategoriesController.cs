using Agri_Energy_Connect.Services;
using DataContextAndModels.DataTransferObjects;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Agri_Energy_Connect.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(IHttpClientFactory httpClientFactory, ILogger<CategoriesController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET: Categories
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Log the request
            _logger.LogInformation("Fetching all categories from the API.");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync("api/categories/all");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to categories API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("No categories found.");

                    return View(new List<CategoryDto>());
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var categories = JsonConvert.DeserializeObject<List<CategoryDto>>(jsonResponse);

                    if (categories == null || categories.Count == 0)
                    {
                        _logger.LogWarning("No categories found.");
                        TempData["ErrorMessage"] = "No categories found.";

                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                        return View(new List<CategoryDto>());
                    }

                    // Log the number of categories found
                    _logger.LogInformation($"Successfully retrieved {categories.Count} categories from API.");

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Return the list of categories to the view
                    return View(categories);

                }

                var errorMessage = await response.Content.ReadAsStringAsync();

                _logger.LogError($"Failed to fetch categories. Status Code: {response.StatusCode}, Error: {errorMessage}");

                TempData["ErrorMessage"] = "Failed to fetch categories. Please try again later.";

                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View(new List<CategoryDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching categories from API.");

                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";

                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View(new List<CategoryDto>());
            }
        }

        // GET: Categories/Create
        [HttpGet]
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("Category model is null.");

                TempData["ErrorMessage"] = "Category model is null.";

                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for category creation.");

                // add error messages to TempData
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

                var response = await client.PostAsync("api/categories", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to categories API.");
                    return RedirectToAction(nameof(Index));
                }

                if (response.IsSuccessStatusCode)
                {

                    // Log the successful creation of the category
                    _logger.LogInformation($"Successfully created category: {model.Name}");

                    // Deserialize the response to get the created category
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var createdCategory = JsonConvert.DeserializeObject<CategoryDto>(jsonResponse);

                    if (createdCategory == null)
                    {
                        _logger.LogWarning("Failed to create category. No response from API.");
                        TempData["ErrorMessage"] = "Failed to create category. No response from API.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return View(model);
                    }

                    // Log the successful creation of the category
                    _logger.LogInformation($"Successfully created category: {createdCategory.Name}");
                    TempData["SuccessMessage"] = "Category created successfully.";

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];

                    return RedirectToAction(nameof(Index));
                }

                var errorMessage = await response.Content.ReadAsStringAsync();

                _logger.LogError($"Failed to create category. Status Code: {response.StatusCode}, Error: {errorMessage}");

                TempData["ErrorMessage"] = $"Failed to create category. {errorMessage}.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(model);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating category.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return View(model);
            }
        }

        // GET: Categories/Delete/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Category ID is null or empty.");
                TempData["ErrorMessage"] = "Category ID cannot be null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"Fetching category with ID: {id} for deletion.");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"api/categories/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to categories API.");
                    return RedirectToAction(nameof(Index));
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Category with ID: {id} not found.");
                    TempData["ErrorMessage"] = "Category not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var category = JsonConvert.DeserializeObject<CategoryDto>(jsonResponse);

                    if (category == null)
                    {
                        _logger.LogWarning($"Failed to fetch category with ID: {id}.");
                        TempData["ErrorMessage"] = "Failed to fetch category. No response from API.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogInformation($"Successfully fetched category for deletion: {category.Name}");

                    return View(category);
                }

                var errorMessage = await response.Content.ReadAsStringAsync();

                _logger.LogError($"Failed to fetch category for deletion. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch category. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting category.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Category ID is null or empty.");
                TempData["ErrorMessage"] = "Category ID cannot be null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"Deleting category with ID: {id}.");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.DeleteAsync($"api/categories/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Category with ID: {id} not found.");
                    TempData["ErrorMessage"] = "Category not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to categories API.");
                    TempData["ErrorMessage"] = "Unauthorized access.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Index));
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully deleted category with ID: {id}.");
                    TempData["SuccessMessage"] = "Category deleted successfully.";
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];

                    return RedirectToAction(nameof(Index));
                }

                var errorMessage = await response.Content.ReadAsStringAsync();

                _logger.LogError($"Failed to delete category. Status Code: {response.StatusCode}, Error: {errorMessage}");

                TempData["ErrorMessage"] = "Failed to delete category. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting category.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return RedirectToAction(nameof(Index));
            }
        }
    }
}
