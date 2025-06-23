using DataContextAndModels.Data;
using DataContextAndModels.DataTransferObjects;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/*
    * Code Attribution
    * Purpose: Implementing category CRUD operations in an ASP.NET Core Web API using Entity Framework Core and role-based authorization
    * Author: Microsoft Documentation & Community Samples (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn - ASP.NET Core Web API with EF Core
    * URL: https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/intro
 */

/*
    * Controller: CategoriesController
    * Description: Provides CRUD operations for product categories. Utilizes Entity Framework Core for data access.
    * Includes endpoints for retrieving, creating, and deleting categories with proper logging and error handling.
    * Authorization is enforced for sensitive operations.
 */

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        /// <summary>
        /// Constructor for CategoriesController.
        /// </summary>
        /// <param name="context">Database context for category access.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all categories, including the count of products in each.
        /// </summary>
        /// <returns>List of CategoryDto objects with product counts, or 404 if none found.</returns>
        // GET: api/categories/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                // Log the request
                _logger.LogInformation("Fetching all categories.");

                // Retrieve all categories and include their products
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .ToListAsync();

                // Check if categories exist
                if (categories == null || categories.Count == 0)
                {
                    _logger.LogWarning("No categories found.");
                    return NotFound("No categories found.");
                }

                _logger.LogInformation($"Found {categories.Count} categories.");

                // Map to DTOs and count products for each category
                var categoryDtos = categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();

                foreach (var categoryDto in categoryDtos)
                {
                    var category = categories.FirstOrDefault(c => c.Id == categoryDto.Id);
                    if (category != null)
                    {
                        categoryDto.NumberOfProducts = category.Products.Count;
                    }
                }

                // Return the categories
                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Retrieves a category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category.</param>
        /// <returns>The CategoryDto or error status.</returns>
        // GET: api/categories/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetCategoryById(string id)
        {
            try
            {
                // Log the request
                _logger.LogInformation($"Fetching category with ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Category ID is null or empty.");
                    return BadRequest("Category ID cannot be null or empty.");
                }

                // Retrieve the category with its products
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {id} not found.");
                    return NotFound($"Category with ID {id} not found.");
                }

                _logger.LogInformation($"Found category: {category.Name}");

                // Map to DTO
                var categoryDto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name
                };

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Creates a new category if it does not already exist.
        /// </summary>
        /// <param name="category">Category creation view model.</param>
        /// <returns>The created CategoryDto or error status.</returns>
        // POST: api/categories
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateViewModel category)
        {
            try
            {
                // Log the request
                _logger.LogInformation($"Creating new category: {category.Name}");

                // Validate model
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for category creation.");
                    return BadRequest(ModelState);
                }

                // Check for duplicate by name (case-insensitive)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());

                if (existingCategory != null)
                {
                    _logger.LogWarning($"Category with name {category.Name} already exists.");
                    return BadRequest($"Category with name {category.Name} already exists.");
                }

                // Create new category entity
                var newCategory = new Category
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = category.Name
                };

                await _context.Categories.AddAsync(newCategory);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category created with ID: {newCategory.Id}");

                var categoryDto = new CategoryDto
                {
                    Id = newCategory.Id,
                    Name = newCategory.Name
                };

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Deletes a category by its ID. Only employees can perform this action.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>Status of the delete operation.</returns>
        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            try
            {
                // Log the request
                _logger.LogInformation($"Deleting category with ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Category ID is null or empty.");
                    return BadRequest("Category ID cannot be null or empty.");
                }

                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {id} not found.");
                    return NotFound($"Category with ID {id} not found.");
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category with ID {id} deleted.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }
    }
}