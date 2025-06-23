using Agri_Energy_Connect.Services;
using DataContextAndModels.DataTransferObjects;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;

/*
    * Code Attribution
    * Purpose: Consuming a secured Web API with HttpClientFactory and JWT authentication in ASP.NET Core MVC,
    *          implementing role-based authorization, robust error handling, and usage of TempData/ViewData for user messaging.
    * Author: Microsoft Docs (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn - Call a web API from ASP.NET Core MVC
    * URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie#call-a-web-api-from-an-aspnet-core-app
 */

/*
    * Controller: ProductsController
    * Description: Manages product-related views and actions for Agri-Energy Connect.
    * Interacts with backend API for CRUD operations on products, filtering, sorting, and product details.
    * Provides category helpers and redirection logic for robust UX.
 */

namespace Agri_Energy_Connect.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductsController> _logger;

        /// <summary>
        /// Constructor for ProductsController.
        /// </summary>
        public ProductsController(IHttpClientFactory httpClientFactory, ILogger<ProductsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Displays a list of products with optional sorting, filtering, and date range.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(
            string sortBy = "name_asc",
            string? category = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            _logger.LogInformation("Fetching all products from the API.");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync("/api/Product/all");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to products API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("No products found.");
                    return View(new List<ProductDto>());
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductDto>>(jsonResponse);

                    if (products == null || products.Count == 0)
                    {
                        _logger.LogWarning("No products found.");
                        TempData["ErrorMessage"] = "No products found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return View(new List<ProductDto>());
                    }

                    _logger.LogInformation($"Successfully retrieved {products.Count} products from API.");
                    SelectList selectListItems = await LoadCategories();
                    if (selectListItems == null || !selectListItems.Any())
                    {
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                    ViewData["CategorySelectList"] = selectListItems;
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering
                    if (!string.IsNullOrEmpty(category))
                        products = products.Where(p => p.CategoryId.Equals(category)).ToList();
                    if (startDate.HasValue && endDate.HasValue)
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();

                    // Sorting
                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList()
                    };

                    return View(products);
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch products. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch products. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching products from API.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
        }

        /// <summary>
        /// Displays product details for a given product ID.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Product ID is null or empty.");
                TempData["ErrorMessage"] = "Product ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
            _logger.LogInformation($"Fetching product details for product ID: {id}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/Product/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to product details API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    TempData["ErrorMessage"] = $"Product with ID {id} not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToRefererOrFallback();
                }
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var product = JsonConvert.DeserializeObject<ProductDto>(jsonResponse);

                    if (product == null)
                    {
                        _logger.LogError($"Product with ID {id} not found.");
                        TempData["ErrorMessage"] = $"Product with ID {id} not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToRefererOrFallback();
                    }
                    _logger.LogInformation($"Successfully retrieved product details for product ID: {id}");
                    TempData["SuccessMessage"] = "Product details retrieved successfully.";
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];

                    return View(product);
                }
                var errorMessage = await response.Content.ReadAsStringAsync();

                _logger.LogError($"Failed to fetch product details. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch product details. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching product details from API.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
        }

        /// <summary>
        /// Displays the create product page for Farmers.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Navigating to create product page.");

            try
            {
                SelectList selectListItems = await LoadCategories();
                if (selectListItems == null || !selectListItems.Any())
                {
                    _logger.LogWarning("No categories found for selection.");
                    TempData["ErrorMessage"] = "No categories found for selection.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    ViewData["CategorySelectList"] = new SelectList(new List<Category>());
                }
                _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                ViewData["CategorySelectList"] = selectListItems;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID is null or empty.");
                    TempData["ErrorMessage"] = "User ID is null or empty.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction("Login", "Account");
                }

                var productCreateViewModel = new ProductCreateViewModel
                {
                    FarmerId = userId,
                    ProductionDate = DateTime.Today
                };

                ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(productCreateViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching categories.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallbackFarmer();
            }
        }

        /// <summary>
        /// Handles product create POST for Farmers.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("Product model is null.");
                TempData["ErrorMessage"] = "Product model is null.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Create));
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for product creation.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessage"] += error.ErrorMessage + "\n";
                }
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                SelectList selectListItems = await LoadCategories();
                if (selectListItems == null || !selectListItems.Any())
                {
                    _logger.LogWarning("No categories found for selection.");
                    TempData["ErrorMessage"] = "No categories found for selection.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                }
                _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                ViewData["CategorySelectList"] = selectListItems;
                return View(model);
            }

            _logger.LogInformation("Creating new product.");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var jsonProduct = JsonConvert.SerializeObject(model);
                var content = new StringContent(jsonProduct, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/Product", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to create product API.");
                    return RedirectToRefererOrFallback();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Bad request while creating product. Error: {error}");
                    TempData["ErrorMessage"] = "Failed to create product. Please check your input.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToAction(nameof(Create));
                }

                _logger.LogInformation($"Response status code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product created successfully: {model.Name}.");
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var createdProduct = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                    if (createdProduct == null)
                    {
                        _logger.LogError("Failed to create product. No response from API.");
                        TempData["ErrorMessage"] = "Failed to create product. Please try again later.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToRefererOrFallbackFarmer();
                    }

                    _logger.LogInformation($"Product created successfully with ID: {createdProduct.id}");
                    TempData["SuccessMessage"] = "Product created successfully.";
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    return RedirectToRefererOrFallbackFarmer();
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to create product. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to create product. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the product.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction(nameof(Create));
            }
        }

        /// <summary>
        /// Displays the edit product view for a given product ID.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Product ID is null or empty.");
                TempData["ErrorMessage"] = "Product ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
            _logger.LogInformation($"Fetching product for editing with ID: {id}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/Product/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to edit product API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    TempData["ErrorMessage"] = $"Product with ID {id} not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToRefererOrFallback();
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var product = JsonConvert.DeserializeObject<ProductDto>(jsonResponse);

                    if (product == null)
                    {
                        _logger.LogError($"Product with ID {id} not found.");
                        TempData["ErrorMessage"] = $"Product with ID {id} not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToRefererOrFallback();
                    }

                    _logger.LogInformation($"Successfully retrieved product for editing with ID: {id}");

                    var productUpdateViewModel = new ProductUpdateViewModel
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price,
                        Quantity = product.Quantity,
                        ProductionDate = product.ProductionDate,
                        CategoryId = product.CategoryId
                    };

                    SelectList selectListItems = await LoadCategories(productUpdateViewModel.CategoryId);
                    if (selectListItems == null || !selectListItems.Any())
                    {
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                    ViewData["CategorySelectList"] = selectListItems;

                    return View(productUpdateViewModel);
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch product for editing. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch product for editing. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching product for editing.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
        }

        /// <summary>
        /// Handles product update POST.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductUpdateViewModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("Product model is null.");
                TempData["ErrorMessage"] = "Product model is null.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for product update.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessage"] += error.ErrorMessage + "\n";
                }
                SelectList selectListItems = await LoadCategories(model.CategoryId);
                if (selectListItems == null || !selectListItems.Any())
                {
                    _logger.LogWarning("No categories found for selection.");
                    TempData["ErrorMessage"] = "No categories found for selection.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                }
                _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                ViewData["CategorySelectList"] = selectListItems;
                return View(model);
            }

            _logger.LogInformation($"Updating product with ID: {model.Id}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var jsonProduct = JsonConvert.SerializeObject(model);
                var content = new StringContent(jsonProduct, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"/api/Product/{model.Id}", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to update product API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Product with ID {model.Id} not found.");
                    TempData["ErrorMessage"] = $"Product with ID {model.Id} not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToRefererOrFallback();
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product updated successfully: {model.Name}.");
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var updatedProduct = JsonConvert.DeserializeObject<ProductDto>(jsonResponse);

                    if (updatedProduct == null)
                    {
                        _logger.LogError($"Failed to update product. No response from API.");
                        TempData["ErrorMessage"] = "Failed to update product. Please try again later.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToRefererOrFallback();
                    }

                    _logger.LogInformation($"Product updated successfully with ID: {updatedProduct.Id}");
                    TempData["SuccessMessage"] = "Product updated successfully.";
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    return RedirectToAction(nameof(Details), new { id = updatedProduct.Id });
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to update product. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to update product. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the product.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(model);
            }
        }

        /// <summary>
        /// Displays the delete confirmation view for a product.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Product ID is null or empty.");
                TempData["ErrorMessage"] = "Product ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
            _logger.LogInformation($"Fetching product for deletion with ID: {id}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/Product/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to delete product API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    TempData["ErrorMessage"] = $"Product with ID {id} not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToRefererOrFallback();
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var product = JsonConvert.DeserializeObject<ProductDto>(jsonResponse);

                    if (product == null)
                    {
                        _logger.LogError($"Product with ID {id} not found.");
                        TempData["ErrorMessage"] = $"Product with ID {id} not found.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return RedirectToRefererOrFallback();
                    }
                    _logger.LogInformation($"Successfully retrieved product for deletion with ID: {id}");
                    return View(product);
                }
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch product for deletion. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch product for deletion. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching product for deletion.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
        }

        /// <summary>
        /// Handles product deletion POST.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Product ID is null or empty.");
                TempData["ErrorMessage"] = "Product ID is null or empty.";
                return RedirectToRefererOrFallback();
            }
            _logger.LogInformation($"Deleting product with ID: {id}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.DeleteAsync($"/api/Product/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to delete product API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    TempData["ErrorMessage"] = $"Product with ID {id} not found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return RedirectToRefererOrFallback();
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product deleted successfully with ID: {id}");
                    TempData["SuccessMessage"] = "Product deleted successfully.";
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    return RedirectToRefererOrFallback();
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to delete product. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to delete product. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the product.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToRefererOrFallback();
            }
        }

        /// <summary>
        /// Displays the farmer's products for the currently logged-in Farmer.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> FarmerProductsIndex(
            string sortBy = "name_asc",
            string? category = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is null or empty.");
                TempData["ErrorMessage"] = "User ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction("Login", "Account");
            }
            _logger.LogInformation($"Fetching products for farmer with ID: {userId}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/Product/farmer/{userId}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to farmer products API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"No products found for farmer with ID {userId}.");
                    return View(new List<ProductDto>());
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductDto>>(jsonResponse);

                    if (products == null || products.Count == 0)
                    {
                        _logger.LogWarning($"No products found for farmer with ID {userId}.");
                        TempData["ErrorMessage"] = $"No products found for farmer with ID {userId}.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return View(new List<ProductDto>());
                    }
                    _logger.LogInformation($"Successfully retrieved {products.Count} products for farmer with ID: {userId}");

                    SelectList selectListItems = await LoadCategories();
                    if (selectListItems == null || !selectListItems.Any())
                    {
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                    ViewData["CategorySelectList"] = selectListItems;
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering and sorting
                    if (!string.IsNullOrEmpty(category))
                        products = products.Where(p => p.CategoryId.Equals(category)).ToList();
                    if (startDate.HasValue && endDate.HasValue)
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();

                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList()
                    };

                    return View(products);
                }
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch farmer's products. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch farmer's products. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching farmer's products from API.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
        }

        /// <summary>
        /// Displays a farmer's products to the employee.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> FarmerProductsEmployee(
            string id,
            string sortBy = "name_asc",
            string? category = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Farmer ID is null or empty.");
                TempData["ErrorMessage"] = "Farmer ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction("FarmerManagement", "Index");
            }
            _logger.LogInformation($"Fetching products for farmer with ID: {id}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/Product/farmer/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to farmer products API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"No products found for farmer with ID {id}.");
                    return View(new List<ProductDto>());
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductDto>>(jsonResponse);

                    if (products == null || products.Count == 0)
                    {
                        _logger.LogWarning($"No products found for farmer with ID {id}.");
                        TempData["ErrorMessage"] = $"No products found for farmer with ID {id}.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return View(new List<ProductDto>());
                    }
                    _logger.LogInformation($"Successfully retrieved {products.Count} products for farmer with ID: {id}");

                    SelectList selectListItems = await LoadCategories();
                    if (selectListItems == null || !selectListItems.Any())
                    {
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                    ViewData["CategorySelectList"] = selectListItems;
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering and sorting
                    if (!string.IsNullOrEmpty(category))
                        products = products.Where(p => p.CategoryId.Equals(category)).ToList();
                    if (startDate.HasValue && endDate.HasValue)
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();

                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList()
                    };

                    return View(products);
                }
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch farmer's products. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch farmer's products. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching farmer's products from API.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
        }

        /// <summary>
        /// Displays all products in a category to the employee.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CategoryProductsEmployee(
            string id,
            string sortBy = "name_asc",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Category ID is null or empty.");
                TempData["ErrorMessage"] = "Category ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return RedirectToAction("Categories", "Index");
            }
            _logger.LogInformation($"Fetching products for Category with ID: {id}");

            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync($"/api/Product/category/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to Category products API.");
                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"No products found for Category with ID {id}.");
                    return View(new List<ProductDto>());
                }

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductDto>>(jsonResponse);

                    if (products == null || products.Count == 0)
                    {
                        _logger.LogWarning($"No products found for Category with ID {id}.");
                        TempData["ErrorMessage"] = $"No products found for Category with ID {id}.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        return View(new List<ProductDto>());
                    }
                    _logger.LogInformation($"Successfully retrieved {products.Count} products for Category with ID: {id}");

                    SelectList selectListItems = await LoadCategories();
                    if (selectListItems == null || !selectListItems.Any())
                    {
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");
                    ViewData["CategorySelectList"] = selectListItems;
                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering and sorting
                    if (startDate.HasValue && endDate.HasValue)
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();

                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList()
                    };

                    return View(products);
                }
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to fetch Categories products. Status Code: {response.StatusCode}, Error: {errorMessage}");
                TempData["ErrorMessage"] = "Failed to fetch Categories products. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching Categories products from API.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return View(new List<ProductDto>());
            }
        }

        #region Category Helper Methods

        /// <summary>
        /// Loads categories for SelectList, optionally setting a default selection.
        /// </summary>
        private async Task<SelectList> LoadCategories()
        {
            _logger.LogInformation("Getting categories from API");
            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync("api/categories/all");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to categories API.");
                    return new SelectList(new List<CategoryDto>());
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch categories: {response.StatusCode} - {error}");
                    TempData["ErrorMessage"] = "Could not retrieve categories. Please try again later.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return new SelectList(new List<CategoryDto>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var categories = JsonConvert.DeserializeObject<List<CategoryDto>>(json);

                if (categories == null || categories.Count == 0)
                {
                    _logger.LogWarning("No categories found.");
                    TempData["ErrorMessage"] = "No categories found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return new SelectList(new List<CategoryDto>());
                }

                // Order categories by name
                SelectList selectListItems = new SelectList(categories.OrderBy(c => c.Name), "Id", "Name");
                return selectListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching categories.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return new SelectList(new List<CategoryDto>());
            }
        }

        private async Task<SelectList> LoadCategories(string defaultSelectedValue)
        {
            _logger.LogInformation("Getting categories from API");
            try
            {
                var client = _httpClientFactory.CreateClient("AgriEnergyAPI");
                client.AddJwtFromCookies(Request);

                var response = await client.GetAsync("api/categories/all");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized access to categories API.");
                    return new SelectList(new List<CategoryDto>());
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch categories: {response.StatusCode} - {error}");
                    TempData["ErrorMessage"] = "Could not retrieve categories. Please try again later.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return new SelectList(new List<CategoryDto>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var categories = JsonConvert.DeserializeObject<List<CategoryDto>>(json);

                if (categories == null || categories.Count == 0)
                {
                    _logger.LogWarning("No categories found.");
                    TempData["ErrorMessage"] = "No categories found.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                    return new SelectList(new List<CategoryDto>());
                }

                SelectList selectListItems = new SelectList(categories.OrderBy(c => c.Name), "Id", "Name", defaultSelectedValue);
                return selectListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching categories.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
                return new SelectList(new List<CategoryDto>());
            }
        }

        #endregion

        #region Redirection Helpers

        /// <summary>
        /// Redirects to HTTP referer or Employee fallback.
        /// </summary>
        private IActionResult RedirectToRefererOrFallbackEmployee()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
            return RedirectToAction("FarmerManagement", "Index");
        }

        /// <summary>
        /// Redirects to HTTP referer or Farmer fallback.
        /// </summary>
        private IActionResult RedirectToRefererOrFallbackFarmer()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
            return RedirectToAction(nameof(FarmerProductsIndex));
        }

        /// <summary>
        /// Redirects to HTTP referer or Index fallback.
        /// </summary>
        private IActionResult RedirectToRefererOrFallback()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                if (referer.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                    referer.Contains("DeleteConfirmed", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction(nameof(Index));
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}