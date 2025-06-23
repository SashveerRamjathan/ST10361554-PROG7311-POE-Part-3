using Agri_Energy_Connect_API.Services;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

/*
    * Code Attribution
    * Purpose: Managing user accounts using ASP.NET Core Identity with role-based authorization in a RESTful API
    * Author: Microsoft Documentation & Community Samples (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn - Identity in ASP.NET Core
    * URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
 */

/*
    * Controller: FarmerAccountController
    * Description: This controller manages CRUD operations for Farmer accounts using ASP.NET Core Identity.
    * Only users with the "Employee" role are authorized to access these endpoints.
    * Logging is included for traceability of actions and errors.
    * No business logic is changed, only documentation and attribution are added.
 */

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmerAccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FarmerAccountController> _logger;

        /// <summary>
        /// Constructor for FarmerAccountController.
        /// </summary>
        /// <param name="userManager">UserManager for ApplicationUser.</param>
        /// <param name="signInManager">SignInManager for ApplicationUser (not used, but injected).</param>
        /// <param name="tokenService">TokenService dependency (not used, but injected).</param>
        /// <param name="logger">Logger for FarmerAccountController.</param>
        public FarmerAccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            ILogger<FarmerAccountController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all Farmer user accounts.
        /// Only users with the "Employee" role can access this endpoint.
        /// </summary>
        /// <returns>A list of users in the Farmer role or 404 if none found.</returns>
        [Authorize(Roles = "Employee")]
        [HttpGet("farmer/all")]
        public async Task<IActionResult> GetAllFarmers()
        {
            // Log the request
            _logger.LogInformation("Fetching all farmers.");

            var farmers = await _userManager.GetUsersInRoleAsync("Farmer");

            // Check if the list is empty or null
            if (farmers == null || !farmers.Any())
            {
                _logger.LogWarning("No farmers found.");
                return NotFound("No farmers found.");
            }

            // Log the number of farmers found
            _logger.LogInformation($"Found {farmers.Count} farmers.");

            // Return the list of farmers
            return Ok(farmers);
        }

        /// <summary>
        /// Retrieves a Farmer user account by ID.
        /// Only users with the "Employee" role can access this endpoint.
        /// </summary>
        /// <param name="id">The ID of the farmer to retrieve.</param>
        /// <returns>The farmer details or 404 if not found.</returns>
        [Authorize(Roles = "Employee")]
        [HttpGet("farmer/{id}")]
        public async Task<IActionResult> GetFarmerById(string id)
        {
            // Log the request
            _logger.LogInformation($"Fetching farmer with ID: {id}");

            var farmer = await _userManager.FindByIdAsync(id);

            // Check if the farmer exists
            if (farmer == null)
            {
                _logger.LogWarning($"Farmer with ID: {id} not found.");
                return NotFound($"Farmer with ID: {id} not found.");
            }

            // Log the farmer details
            _logger.LogInformation($"Found farmer: {farmer.FullName}");

            // Return the farmer details
            return Ok(farmer);
        }

        /// <summary>
        /// Updates a Farmer user account by ID.
        /// Only users with the "Employee" role can access this endpoint.
        /// </summary>
        /// <param name="id">The ID of the farmer to update.</param>
        /// <param name="model">The view model containing updated farmer properties.</param>
        /// <returns>The updated farmer or error information.</returns>
        [Authorize(Roles = "Employee")]
        [HttpPut("farmer/{id}")]
        public async Task<IActionResult> UpdateFarmer(string id, [FromBody] FarmerUpdateViewModel model)
        {
            // Log the request
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

            // Log the successful update
            _logger.LogInformation($"Successfully updated farmer with ID: {id}");

            return Ok(farmer);
        }

        /// <summary>
        /// Deletes a Farmer user account by ID.
        /// Only users with the "Employee" role can access this endpoint.
        /// </summary>
        /// <param name="id">The ID of the farmer to delete.</param>
        /// <returns>Status of the delete operation.</returns>
        [Authorize(Roles = "Employee")]
        [HttpDelete("farmer/{id}")]
        public async Task<IActionResult> DeleteFarmer(string id)
        {
            // Log the request
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

            // Log the successful deletion
            _logger.LogInformation($"Successfully deleted farmer with ID: {id}");
            return Ok();
        }
    }
}