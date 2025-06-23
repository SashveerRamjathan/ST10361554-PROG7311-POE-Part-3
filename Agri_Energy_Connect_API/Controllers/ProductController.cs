using DataContextAndModels.Data;
using DataContextAndModels.DataTransferObjects;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/*
    * Code Attribution
    * Purpose: Implementing RESTful CRUD operations using ASP.NET Core Web API with Entity Framework Core for data persistence and authorization
    * Author: Microsoft Documentation & Community Samples (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn - ASP.NET Core Web API & EF Core tutorials
    * URL: https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-8.0
 */

/*
    * Controller: ProductController
    * Description: This controller provides CRUD and query operations for products, including filtering by category and farmer.
    * It uses Entity Framework Core for data access and supports authorization for creating, updating, and deleting products.
    * Each action logs requests, errors, and other significant events for traceability.
    * All endpoints return appropriate HTTP status codes and error messages.
 */

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor for ProductController.
        /// </summary>
        /// <param name="context">Database context for data access.</param>
        /// <param name="logger">Logger for diagnostic and trace logging.</param>
        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all products with their associated category and farmer information.
        /// </summary>
        /// <returns>List of products as DTOs or 404 if none found.</returns>
        // GET: api/products/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                _logger.LogInformation("Fetching all products.");

                // Retrieve all products and include related navigation properties.
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Farmer)
                    .ToListAsync();

                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning("No products found.");
                    return NotFound("No products found.");
                }

                _logger.LogInformation($"Found {products.Count} products.");

                // Validate navigation properties for each product.
                foreach (var product in products)
                {
                    if (product.Category == null || product.Farmer == null)
                    {
                        _logger.LogWarning($"Product with ID {product.Id} has null navigation properties.");
                        return NotFound($"Product with ID {product.Id} has null navigation properties.");
                    }
                }

                // Map products to DTOs.
                var productDtos = products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    ProductionDate = p.ProductionDate,
                    FarmerId = p.FarmerId,
                    CategoryId = p.CategoryId,
                    FarmerName = p.Farmer.FullName!,
                    CategoryName = p.Category.Name
                }).ToList();

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Retrieves a single product by its ID.
        /// </summary>
        /// <param name="id">Product ID.</param>
        /// <returns>Product DTO or appropriate error status.</returns>
        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            try
            {
                _logger.LogInformation($"Fetching product with ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Product ID is null or empty.");
                    return BadRequest("Product ID cannot be null or empty.");
                }

                // Retrieve product and related navigation properties.
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Farmer)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    return NotFound($"Product with ID {id} not found.");
                }

                _logger.LogInformation($"Found product: {product.Name}");

                if (product.Category == null || product.Farmer == null)
                {
                    _logger.LogWarning($"Product with ID {id} has null navigation properties.");
                    return NotFound($"Product with ID {id} has null navigation properties.");
                }

                // Map to DTO.
                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    ProductionDate = product.ProductionDate,
                    FarmerId = product.FarmerId,
                    CategoryId = product.CategoryId,
                    FarmerName = product.Farmer.FullName!,
                    CategoryName = product.Category.Name!
                };

                return Ok(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Creates a new product.
        /// Only users in the "Farmer" role are authorized to access this endpoint.
        /// </summary>
        /// <param name="product">Product creation view model.</param>
        /// <returns>Newly created product ID or error status.</returns>
        // POST: api/Product
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateViewModel product)
        {
            try
            {
                _logger.LogInformation($"Creating product: {product.Name}");

                if (product == null)
                {
                    _logger.LogWarning("Product is null.");
                    return BadRequest("Product cannot be null.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for product creation.");
                    return BadRequest(ModelState);
                }

                // Validate Farmer ID.
                var farmer = await _context.Users.FindAsync(product.FarmerId);
                if (farmer == null)
                {
                    _logger.LogWarning($"Farmer with ID {product.FarmerId} not found.");
                    return NotFound($"Farmer with ID {product.FarmerId} not found.");
                }
                _logger.LogInformation($"Farmer found: {farmer.FullName}");

                // Validate Category ID.
                var category = await _context.Categories.FindAsync(product.CategoryId);
                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {product.CategoryId} not found.");
                    return NotFound($"Category with ID {product.CategoryId} not found.");
                }
                _logger.LogInformation($"Category found: {category.Name}");

                // Create and save new product.
                var newProduct = new Product
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    ProductionDate = product.ProductionDate,
                    FarmerId = product.FarmerId,
                    CategoryId = product.CategoryId
                };

                await _context.Products.AddAsync(newProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Product created with ID: {newProduct.Id}");

                return Ok(new { id = newProduct.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Updates an existing product by ID.
        /// Only authorized users may update products.
        /// </summary>
        /// <param name="id">Product ID.</param>
        /// <param name="product">Product update view model.</param>
        /// <returns>Updated product DTO or error status.</returns>
        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] ProductUpdateViewModel product)
        {
            try
            {
                _logger.LogInformation($"Updating product with ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Product ID is null or empty.");
                    return BadRequest("Product ID cannot be null or empty.");
                }

                if (product == null)
                {
                    _logger.LogWarning("Product is null.");
                    return BadRequest("Product cannot be null.");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for product update.");
                    return BadRequest(ModelState);
                }

                // Find existing product.
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    return NotFound($"Product with ID {id} not found.");
                }

                // Update fields.
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Quantity = product.Quantity;
                existingProduct.ProductionDate = product.ProductionDate;
                existingProduct.CategoryId = product.CategoryId;

                await _context.SaveChangesAsync();

                // Reload with navigation properties.
                existingProduct = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Farmer)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (existingProduct == null)
                {
                    _logger.LogWarning($"Updated product with ID {id} not found.");
                    return NotFound($"Updated product with ID {id} not found.");
                }

                if (existingProduct.Category == null || existingProduct.Farmer == null)
                {
                    _logger.LogWarning($"Updated product with ID {id} has null navigation properties.");
                    return NotFound($"Updated product with ID {id} has null navigation properties.");
                }

                var productDto = new ProductDto
                {
                    Id = existingProduct.Id,
                    Name = existingProduct.Name,
                    Description = existingProduct.Description,
                    Price = existingProduct.Price,
                    Quantity = existingProduct.Quantity,
                    ProductionDate = existingProduct.ProductionDate,
                    FarmerId = existingProduct.FarmerId,
                    CategoryId = existingProduct.CategoryId,
                    FarmerName = existingProduct.Farmer.FullName!,
                    CategoryName = existingProduct.Category.Name!
                };

                _logger.LogInformation($"Product updated with ID: {existingProduct.Id}");

                return Ok(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Deletes a product by ID.
        /// Only authorized users can delete products.
        /// </summary>
        /// <param name="id">Product ID.</param>
        /// <returns>Status of the delete operation.</returns>
        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                _logger.LogInformation($"Deleting product with ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Product ID is null or empty.");
                    return BadRequest("Product ID cannot be null or empty.");
                }

                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    return NotFound($"Product with ID {id} not found.");
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Product deleted with ID: {product.Id}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Retrieves products by category ID.
        /// Only authorized users can access this endpoint.
        /// </summary>
        /// <param name="categoryId">Category ID.</param>
        /// <returns>List of products or error status.</returns>
        // GET: api/products/category/{categoryId}
        [HttpGet("category/{categoryId}")]
        [Authorize]
        public async Task<IActionResult> GetProductsByCategory(string categoryId)
        {
            try
            {
                _logger.LogInformation($"Fetching products for category ID: {categoryId}");

                if (string.IsNullOrEmpty(categoryId))
                {
                    _logger.LogWarning("Category ID is null or empty.");
                    return BadRequest("Category ID cannot be null or empty.");
                }

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Farmer)
                    .Where(p => p.CategoryId == categoryId)
                    .ToListAsync();

                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning($"No products found for category ID {categoryId}.");
                    return NotFound($"No products found for category ID {categoryId}.");
                }

                foreach (var product in products)
                {
                    if (product.Category == null || product.Farmer == null)
                    {
                        _logger.LogWarning($"Product with ID {product.Id} has null navigation properties.");
                        return NotFound($"Product with ID {product.Id} has null navigation properties.");
                    }
                }

                var productDtos = products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    ProductionDate = p.ProductionDate,
                    FarmerId = p.FarmerId,
                    CategoryId = p.CategoryId,
                    FarmerName = p.Farmer.FullName!,
                    CategoryName = p.Category.Name!
                }).ToList();

                _logger.LogInformation($"Found {products.Count} products for category ID {categoryId}.");

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by category");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Retrieves products by farmer ID.
        /// Only authorized users can access this endpoint.
        /// </summary>
        /// <param name="farmerId">Farmer ID.</param>
        /// <returns>List of products or error status.</returns>
        // GET: api/products/farmer/{farmerId}
        [HttpGet("farmer/{farmerId}")]
        [Authorize]
        public async Task<IActionResult> GetProductsByFarmer(string farmerId)
        {
            try
            {
                _logger.LogInformation($"Fetching products for farmer ID: {farmerId}");

                if (string.IsNullOrEmpty(farmerId))
                {
                    _logger.LogWarning("Farmer ID is null or empty.");
                    return BadRequest("Farmer ID cannot be null or empty.");
                }

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Farmer)
                    .Where(p => p.FarmerId == farmerId)
                    .ToListAsync();

                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning($"No products found for farmer ID {farmerId}.");
                    return NotFound($"No products found for farmer ID {farmerId}.");
                }

                foreach (var product in products)
                {
                    if (product.Category == null || product.Farmer == null)
                    {
                        _logger.LogWarning($"Product with ID {product.Id} has null navigation properties.");
                        return NotFound($"Product with ID {product.Id} has null navigation properties.");
                    }
                }

                var productDtos = products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    ProductionDate = p.ProductionDate,
                    FarmerId = p.FarmerId,
                    CategoryId = p.CategoryId,
                    FarmerName = p.Farmer.FullName!,
                    CategoryName = p.Category.Name!
                }).ToList();

                _logger.LogInformation($"Found {products.Count} products for farmer ID {farmerId}.");

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by farmer");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }
    }
}