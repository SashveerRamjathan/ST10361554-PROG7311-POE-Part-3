using DataContextAndModels.Data;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;

        private readonly ApplicationDbContext _context;
        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/products/all
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                // log the request
                _logger.LogInformation("Fetching all products.");

                // get all products from the database
                var products = await _context.Products
                    .Include(p => p.Category) // Include the Category navigation property
                    .Include(p => p.Farmer) // Include the Farmer navigation property
                    .ToListAsync();

                // Check if the list is empty or null
                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning("No products found.");
                    return NotFound("No products found.");
                }

                // log the number of products found
                _logger.LogInformation($"Found {products.Count} products.");

                // Return the list of products
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetProductById(string id)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Fetching product with ID: {id}");

                // Check if the ID is null or empty
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Product ID is null or empty.");

                    return BadRequest("Product ID cannot be null or empty.");
                }

                // get the product from the database
                var product = await _context.Products
                    .Include(p => p.Category) // Include the Category navigation property
                    .Include(p => p.Farmer) // Include the Farmer navigation property
                    .FirstOrDefaultAsync(p => p.Id == id);

                // Check if the product is null
                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    return NotFound($"Product with ID {id} not found.");
                }
                // log the product details
                _logger.LogInformation($"Found product: {product.Name}");

                // Return the product
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // POST: api/products
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateViewModel product)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Creating product: {product.Name}");

                // Check if the product is null
                if (product == null)
                {
                    _logger.LogWarning("Product is null.");
                    return BadRequest("Product cannot be null.");
                }

                // Validate the model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for product creation.");
                    return BadRequest(ModelState);
                }

                // Check if the Farmer ID is valid
                var farmer = await _context.Users.FindAsync(product.FarmerId);

                if (farmer == null)
                {
                    _logger.LogWarning($"Farmer with ID {product.FarmerId} not found.");
                    return NotFound($"Farmer with ID {product.FarmerId} not found.");
                }

                // log the farmer details
                _logger.LogInformation($"Farmer found: {farmer.FullName}");

                // Check if the Category ID is valid
                var category = await _context.Categories.FindAsync(product.CategoryId);

                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {product.CategoryId} not found.");
                    return NotFound($"Category with ID {product.CategoryId} not found.");
                }

                // log the category details
                _logger.LogInformation($"Category found: {category.Name}");

                // Create a new product object
                var newProduct = new Product
                {
                    Id = Guid.NewGuid().ToString(), // Generate a new ID
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    ProductionDate = product.ProductionDate,
                    FarmerId = product.FarmerId,
                    CategoryId = product.CategoryId
                };

                // Add the product to the database
                await _context.Products.AddAsync(newProduct);

                // Save changes to the database
                await _context.SaveChangesAsync();
                // log the product creation

                _logger.LogInformation($"Product created with ID: {newProduct.Id}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                // Return a 500 Internal Server Error response

                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }


        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] ProductUpdateViewModel product)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Updating product with ID: {id}");

                // Check if the ID is null or empty
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Product ID is null or empty.");
                    return BadRequest("Product ID cannot be null or empty.");
                }

                // Check if the product is null
                if (product == null)
                {
                    _logger.LogWarning("Product is null.");
                    return BadRequest("Product cannot be null.");
                }

                // Validate the model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for product update.");
                    return BadRequest(ModelState);
                }

                // get the product from the database
                var existingProduct = await _context.Products.FindAsync(id);

                // Check if the product exists
                if (existingProduct == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    return NotFound($"Product with ID {id} not found.");
                }

                // Update the product properties
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Quantity = product.Quantity;
                existingProduct.ProductionDate = product.ProductionDate;

                // Save changes to the database
                await _context.SaveChangesAsync();

                // log the product update
                _logger.LogInformation($"Product updated with ID: {existingProduct.Id}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Deleting product with ID: {id}");

                // Check if the ID is null or empty
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Product ID is null or empty.");
                    return BadRequest("Product ID cannot be null or empty.");
                }

                // get the product from the database
                var product = await _context.Products.FindAsync(id);

                // Check if the product exists
                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found.");
                    return NotFound($"Product with ID {id} not found.");
                }

                // Remove the product from the database
                _context.Products.Remove(product);

                // Save changes to the database
                await _context.SaveChangesAsync();

                // log the product deletion
                _logger.LogInformation($"Product deleted with ID: {product.Id}");

                // Return a success response
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // GET: api/products/category/{categoryId}
        [HttpGet("category/{categoryId}")]
        [Authorize]
        public async Task<IActionResult> GetProductsByCategory(string categoryId)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Fetching products for category ID: {categoryId}");

                // Check if the category ID is null or empty
                if (string.IsNullOrEmpty(categoryId))
                {
                    _logger.LogWarning("Category ID is null or empty.");
                    return BadRequest("Category ID cannot be null or empty.");
                }

                // get the products from the database
                var products = await _context.Products
                    .Include(p => p.Category) // Include the Category navigation property
                    .Include(p => p.Farmer) // Include the Farmer navigation property
                    .Where(p => p.CategoryId == categoryId)
                    .ToListAsync();

                // Check if the list is empty or null
                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning($"No products found for category ID {categoryId}.");
                    return NotFound($"No products found for category ID {categoryId}.");
                }

                // log the number of products found
                _logger.LogInformation($"Found {products.Count} products for category ID {categoryId}.");

                // Return the list of products
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by category");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // GET: api/products/farmer/{farmerId}
        [HttpGet("farmer/{farmerId}")]
        [Authorize]
        public async Task<IActionResult> GetProductsByFarmer(string farmerId)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Fetching products for farmer ID: {farmerId}");

                // Check if the farmer ID is null or empty
                if (string.IsNullOrEmpty(farmerId))
                {
                    _logger.LogWarning("Farmer ID is null or empty.");
                    return BadRequest("Farmer ID cannot be null or empty.");
                }

                // get the products from the database
                var products = await _context.Products
                    .Include(p => p.Category) // Include the Category navigation property
                    .Include(p => p.Farmer) // Include the Farmer navigation property
                    .Where(p => p.FarmerId == farmerId)
                    .ToListAsync();

                // Check if the list is empty or null
                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning($"No products found for farmer ID {farmerId}.");
                    return NotFound($"No products found for farmer ID {farmerId}.");
                }

                // log the number of products found
                _logger.LogInformation($"Found {products.Count} products for farmer ID {farmerId}.");

                // Return the list of products
                return Ok(products);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by farmer");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }
    }
}
