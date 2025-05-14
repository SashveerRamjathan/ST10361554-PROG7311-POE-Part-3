using DataContextAndModels.Data;
using DataContextAndModels.DataTransferObjects;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/categories/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                // log the request
                _logger.LogInformation("Fetching all categories.");

                // get all categories from the database
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .ToListAsync();

                // Check if the list is empty or null
                if (categories == null || categories.Count == 0)
                {
                    _logger.LogWarning("No categories found.");
                    return NotFound("No categories found.");
                }

                // log the number of categories found
                _logger.LogInformation($"Found {categories.Count} categories.");

                // convert the categories to a list of CategoryDtos
                var categoryDtos = categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();

                // Return the list of categories
                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetCategoryById(string id)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Fetching category with ID: {id}");

                // Check if the ID is null or empty
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Category ID is null or empty.");

                    return BadRequest("Category ID cannot be null or empty.");
                }

                // get the category from the database
                var category = await _context.Categories
                        .Include(c => c.Products)
                        .FirstOrDefaultAsync(c => c.Id == id);


                // Check if the category is null
                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {id} not found.");

                    return NotFound($"Category with ID {id} not found.");
                }

                // log the category details
                _logger.LogInformation($"Found category: {category.Name}");

                // convert the category to a CategoryDto
                var categoryDto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name
                };

                // Return the category
                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // POST: api/categories
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateViewModel category)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Creating new category: {category.Name}");

                // Check if the model is valid
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for category creation.");

                    return BadRequest(ModelState);
                }

                // Check if the category already exists
                var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());


                if (existingCategory != null)
                {
                    _logger.LogWarning($"Category with name {category.Name} already exists.");
                    return BadRequest($"Category with name {category.Name} already exists.");
                }

                // Create a new category object
                var newCategory = new Category
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = category.Name
                };

                // add the category to the database
                await _context.Categories.AddAsync(newCategory);

                // save changes to the database
                await _context.SaveChangesAsync();

                // log the category creation
                _logger.LogInformation($"Category created with ID: {newCategory.Id}");

                // convert the new category to a CategoryDto
                var categoryDto = new CategoryDto
                {
                    Id = newCategory.Id,
                    Name = newCategory.Name
                };

                // return ok response
                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            try
            {
                // log the request
                _logger.LogInformation($"Deleting category with ID: {id}");

                // Check if the ID is null or empty
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Category ID is null or empty.");
                    return BadRequest("Category ID cannot be null or empty.");
                }

                // get the category from the database
                var category = await _context.Categories.FindAsync(id);

                // Check if the category is null
                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {id} not found.");
                    return NotFound($"Category with ID {id} not found.");
                }

                // remove the category from the database
                _context.Categories.Remove(category);

                // save changes to the database
                await _context.SaveChangesAsync();

                // log the category deletion
                _logger.LogInformation($"Category with ID {id} deleted.");

                // return ok response
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");

                // Return a 500 Internal Server Error response
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

    }
}
