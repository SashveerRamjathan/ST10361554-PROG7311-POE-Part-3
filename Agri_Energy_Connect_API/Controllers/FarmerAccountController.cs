using Agri_Energy_Connect_API.Services;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmerAccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FarmerAccountController> _logger;
        public FarmerAccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            ILogger<FarmerAccountController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // Add methods for managing farmer accounts here

        // Get all Farmer accounts
        [Authorize(Roles = "Employee")]
        [HttpGet("farmer/all")]
        public async Task<IActionResult> GetAllFarmers()
        {
            // log the request
            _logger.LogInformation("Fetching all farmers.");

            var farmers = await _userManager.GetUsersInRoleAsync("Farmer");

            // Check if the list is empty or null
            if (farmers == null || !farmers.Any())
            {
                _logger.LogWarning("No farmers found.");
                return NotFound("No farmers found.");
            }

            // log the number of farmers found
            _logger.LogInformation($"Found {farmers.Count} farmers.");

            // Return the list of farmers
            return Ok(farmers);
        }

        // Get Farmer account by ID
        [Authorize(Roles = "Employee")]
        [HttpGet("farmer/{id}")]
        public async Task<IActionResult> GetFarmerById(string id)
        {
            // log the request
            _logger.LogInformation($"Fetching farmer with ID: {id}");

            var farmer = await _userManager.FindByIdAsync(id);

            // Check if the farmer exists
            if (farmer == null)
            {
                _logger.LogWarning($"Farmer with ID: {id} not found.");
                return NotFound($"Farmer with ID: {id} not found.");
            }

            // log the farmer details
            _logger.LogInformation($"Found farmer: {farmer.FullName}");

            // Return the farmer details
            return Ok(farmer);
        }

        // Update Farmer account
        [Authorize(Roles = "Employee")]
        [HttpPut("farmer/{id}")]
        public async Task<IActionResult> UpdateFarmer(string id, [FromBody] FarmerUpdateViewModel model)
        {
            // log the request
            _logger.LogInformation($"Updating farmer with ID: {id}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for updating farmer with ID: {id}");
                return BadRequest(ModelState);
            }

            var farmer = await _userManager.FindByIdAsync(id);

            // Check if the farmer exists
            if (farmer == null)
            {
                _logger.LogWarning($"Farmer with ID: {id} not found.");
                return NotFound($"Farmer with ID: {id} not found.");
            }

            // Update the farmer details
            farmer.FullName = model.FullName;
            farmer.Address = model.Address;
            farmer.PhoneNumber = model.PhoneNumber;
            farmer.Email = model.EmailAddress;
            farmer.UserName = model.EmailAddress;

            var result = await _userManager.UpdateAsync(farmer);

            if (!result.Succeeded)
            {
                _logger.LogError($"Error updating farmer with ID: {id}");
                return BadRequest(result.Errors);
            }

            // log the successful update
            _logger.LogInformation($"Successfully updated farmer with ID: {id}");

            return Ok(farmer);
        }

        // Delete Farmer account
        [Authorize(Roles = "Employee")]
        [HttpDelete("farmer/{id}")]
        public async Task<IActionResult> DeleteFarmer(string id)
        {
            // log the request
            _logger.LogInformation($"Deleting farmer with ID: {id}");

            var farmer = await _userManager.FindByIdAsync(id);

            // Check if the farmer exists
            if (farmer == null)
            {
                _logger.LogWarning($"Farmer with ID: {id} not found.");
                return NotFound($"Farmer with ID: {id} not found.");
            }

            var result = await _userManager.DeleteAsync(farmer);

            if (!result.Succeeded)
            {
                _logger.LogError($"Error deleting farmer with ID: {id}");
                return BadRequest(result.Errors);
            }

            // log the successful deletion
            _logger.LogInformation($"Successfully deleted farmer with ID: {id}");
            return Ok();
        }

    }
}
