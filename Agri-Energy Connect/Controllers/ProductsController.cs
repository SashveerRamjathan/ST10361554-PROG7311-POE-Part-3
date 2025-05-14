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

namespace Agri_Energy_Connect.Controllers
{
    public class ProductsController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IHttpClientFactory httpClientFactory, ILogger<ProductsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        // GET: Products
        [HttpGet]
        public async Task<IActionResult> Index(
            string sortBy = "name_asc",
            string? category = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {

            // log the request
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

                    // log the number of products found
                    _logger.LogInformation($"Successfully retrieved {products.Count} products from API.");

                    SelectList selectListItems = await LoadCategories();

                    if (selectListItems == null || !selectListItems.Any())
                    {
                        // log the warning
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }

                    // log the number of categories found
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                    // Create a SelectList and pass it to the view via ViewData
                    ViewData["CategorySelectList"] = selectListItems;

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering
                    if (!string.IsNullOrEmpty(category))
                    {
                        products = products.Where(p => p.CategoryId.Equals(category)).ToList();
                    }

                    if (startDate.HasValue && endDate.HasValue)
                    {
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();
                    }

                    // Sorting
                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList() // default
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

        // GET: Products/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Product ID is null or empty.");
                TempData["ErrorMessage"] = "Product ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return RedirectToRefererOrFallback();
            }

            // log the request
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


                    // log the product details
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

        // GET: Products/Create
        [HttpGet]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> Create()
        {
            // log the request
            _logger.LogInformation("Navigating to create product page.");

            try
            {
                SelectList selectListItems = await LoadCategories();

                if (selectListItems == null || !selectListItems.Any())
                {
                    // log the warning
                    _logger.LogWarning("No categories found for selection.");
                    TempData["ErrorMessage"] = "No categories found for selection.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    ViewData["CategorySelectList"] = new SelectList(new List<Category>());
                }

                // log the number of categories found
                _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                // Create a SelectList and pass it to the view via ViewData
                ViewData["CategorySelectList"] = selectListItems;

                // get the user ID from the claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID is null or empty.");
                    TempData["ErrorMessage"] = "User ID is null or empty.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    return RedirectToAction("Login", "Account");
                }

                // create a new product view model
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

        // POST: Products/Create
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


                // add validation errors to TempData
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessage"] += error.ErrorMessage + "\n";
                }

                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                SelectList selectListItems = await LoadCategories();

                if (selectListItems == null || !selectListItems.Any())
                {
                    // log the warning
                    _logger.LogWarning("No categories found for selection.");
                    TempData["ErrorMessage"] = "No categories found for selection.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                }

                // log the number of categories found
                _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                // Create a SelectList and pass it to the view via ViewData
                ViewData["CategorySelectList"] = selectListItems;

                return View(model);
            }

            // log the request
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

                // log the response status code
                _logger.LogInformation($"Response status code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    // log the successful creation
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

                    // log the successful creation
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

        // GET: Products/Edit/5
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

            // log the request
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

                    // log the product details
                    _logger.LogInformation($"Successfully retrieved product for editing with ID: {id}");

                    // create a view model for editing
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

                    // load categories for the select list
                    SelectList selectListItems = await LoadCategories(productUpdateViewModel.CategoryId);

                    if (selectListItems == null || !selectListItems.Any())
                    {
                        // log the warning
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }

                    // log the number of categories found
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                    // Create a SelectList and pass it to the view via ViewData
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

        // POST: Products/Edit
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
                // add validation errors to TempData
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessage"] += error.ErrorMessage + "\n";
                }

                SelectList selectListItems = await LoadCategories(model.CategoryId);

                if (selectListItems == null || !selectListItems.Any())
                {
                    // log the warning
                    _logger.LogWarning("No categories found for selection.");
                    TempData["ErrorMessage"] = "No categories found for selection.";
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                }

                // log the number of categories found
                _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                // Create a SelectList and pass it to the view via ViewData
                ViewData["CategorySelectList"] = selectListItems;

                return View(model);
            }

            // log the request
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
                    // log the successful update
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

                    // log the successful update
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

        // GET: Products/Delete/5
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

            // log the request
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
                    // log the product details
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

        // POST: Products/Delete/5
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

            // log the request
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
                    // log the successful deletion
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

        // GET: Products/FarmerProductsIndex
        [HttpGet]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> FarmerProductsIndex(
            string sortBy = "name_asc",
            string? category = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            // get the user ID from the claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is null or empty.");
                TempData["ErrorMessage"] = "User ID is null or empty.";
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                return RedirectToAction("Login", "Account");
            }

            // log the request
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

                    // log the number of products found
                    _logger.LogInformation($"Successfully retrieved {products.Count} products for farmer with ID: {userId}");

                    SelectList selectListItems = await LoadCategories();

                    if (selectListItems == null || !selectListItems.Any())
                    {
                        // log the warning
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }

                    // log the number of categories found
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                    // Create a SelectList and pass it to the view via ViewData
                    ViewData["CategorySelectList"] = selectListItems;

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering
                    if (!string.IsNullOrEmpty(category))
                    {
                        products = products.Where(p => p.CategoryId.Equals(category)).ToList();
                    }

                    if (startDate.HasValue && endDate.HasValue)
                    {
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();
                    }

                    // Sorting
                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList() // default
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

        // GET: Products/FarmerProductsEmployee
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

            // log the request
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

                    // log the number of products found
                    _logger.LogInformation($"Successfully retrieved {products.Count} products for farmer with ID: {id}");

                    SelectList selectListItems = await LoadCategories();

                    if (selectListItems == null || !selectListItems.Any())
                    {
                        // log the warning
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }

                    // log the number of categories found
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                    // Create a SelectList and pass it to the view via ViewData
                    ViewData["CategorySelectList"] = selectListItems;

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering
                    if (!string.IsNullOrEmpty(category))
                    {
                        products = products.Where(p => p.CategoryId.Equals(category)).ToList();
                    }

                    if (startDate.HasValue && endDate.HasValue)
                    {
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();
                    }

                    // Sorting
                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList() // default
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

        // GET: Products/CategoryProductsEmployee
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

            // log the request
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

                    // log the number of products found
                    _logger.LogInformation($"Successfully retrieved {products.Count} products for Category with ID: {id}");

                    SelectList selectListItems = await LoadCategories();

                    if (selectListItems == null || !selectListItems.Any())
                    {
                        // log the warning
                        _logger.LogWarning("No categories found for selection.");
                        TempData["ErrorMessage"] = "No categories found for selection.";
                        ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                        ViewData["CategorySelectList"] = new SelectList(new List<CategoryDto>());
                    }

                    // log the number of categories found
                    _logger.LogInformation($"Loaded {selectListItems!.Count()} categories for selection.");

                    // Create a SelectList and pass it to the view via ViewData
                    ViewData["CategorySelectList"] = selectListItems;

                    ViewData["SuccessMessage"] = TempData["SuccessMessage"];
                    ViewData["ErrorMessage"] = TempData["ErrorMessage"];

                    // Filtering
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        products = products.Where(p => p.ProductionDate >= startDate && p.ProductionDate <= endDate).ToList();
                    }

                    // Sorting
                    products = sortBy switch
                    {
                        "name_asc" => products.OrderBy(p => p.Name).ToList(),
                        "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
                        "date_asc" => products.OrderBy(p => p.ProductionDate).ToList(),
                        "date_desc" => products.OrderByDescending(p => p.ProductionDate).ToList(),
                        _ => products.OrderBy(p => p.Name).ToList() // default
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


        #region Category Helper Method

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

                // Create a SelectList and return
                SelectList selectListItems = new SelectList(categories, "Id", "Name");

                // order the categories by name
                selectListItems = new SelectList(categories.OrderBy(c => c.Name), "Id", "Name");

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

                // Create a SelectList and return
                SelectList selectListItems = new SelectList(categories, "Id", "Name", defaultSelectedValue);

                // order the categories by name
                selectListItems = new SelectList(categories.OrderBy(c => c.Name), "Id", "Name", defaultSelectedValue);

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

        private IActionResult RedirectToRefererOrFallbackEmployee()
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction("FarmerManagement", "Index"); // Fallback if no referer
        }

        private IActionResult RedirectToRefererOrFallbackFarmer()
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(FarmerProductsIndex)); // Fallback if no referer
        }

        private IActionResult RedirectToRefererOrFallback()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index)); // Fallback if no referer
        }


        #endregion
    }
}

