using Agri_Energy_Connect_API.Services;
using DataContextAndModels.Enums;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

/*
    * Code Attribution
    * Purpose: User registration, role assignment, and authentication using ASP.NET Core Identity and JWT
    * Author: Microsoft Docs & Community Samples (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn - Secure an ASP.NET Core Web API with Identity and JWT
    * URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
 */

/*
    * Controller: AuthController
    * Description: This controller provides endpoints for registration and authentication of Farmers and Employees.
    * Utilizes ASP.NET Core Identity for user and role management and issues JWT tokens for authenticated users.
    * Logging is included for traceability and error diagnostics.
    * No business logic is changed, only documentation and attribution are added.
 */

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Constructor for AuthController.
        /// </summary>
        /// <param name="userManager">UserManager for managing users.</param>
        /// <param name="signInManager">SignInManager for sign-in operations.</param>
        /// <param name="tokenService">TokenService for generating JWT tokens.</param>
        /// <param name="logger">Logger for diagnostics and traceability.</param>
        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new Farmer account. Only Employees can perform this action.
        /// </summary>
        /// <param name="model">The registration details for the Farmer.</param>
        /// <returns>Success or error status.</returns>
        [Authorize(Roles = "Employee")]
        [HttpPost("register/farmer")]
        public async Task<IActionResult> RegisterFarmer([FromBody] FarmerRegisterViewModel model)
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for farmer registration with email: {model.EmailAddress}");
                return BadRequest(ModelState);
            }

            // Check if user already exists
            if (await _userManager.FindByEmailAsync(model.EmailAddress) != null)
            {
                _logger.LogWarning($"User with email {model.EmailAddress} already exists.");
                return BadRequest("User with this email already exists.");
            }

            _logger.LogInformation($"Farmer registration initiated by employee for email: {model.EmailAddress}");

            // Create new user entity
            var user = new ApplicationUser
            {
                UserName = model.EmailAddress,
                Email = model.EmailAddress,
                Address = model.Address,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            // Persist user
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Failed to register farmer with email: {model.EmailAddress}. Errors: {result.Errors}");
                return BadRequest(result.Errors);
            }

            // Assign Farmer role
            var roleResult = await _userManager.AddToRoleAsync(user, RolesEnum.Farmer.ToString());

            if (!roleResult.Succeeded)
            {
                _logger.LogError($"Failed to assign 'Farmer' role to user: {model.EmailAddress}. Errors: {roleResult.Errors}");
                return StatusCode(500, "User created but role assignment failed.");
            }

            _logger.LogInformation($"Farmer registered successfully and role assigned for email: {model.EmailAddress}");

            return Ok();
        }

        /// <summary>
        /// Registers a new Employee account. Only Employees can perform this action.
        /// </summary>
        /// <param name="model">The registration details for the Employee.</param>
        /// <returns>Success or error status.</returns>
        [Authorize(Roles = "Employee")]
        [HttpPost("register/employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] EmployeeRegisterViewModel model)
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for employee registration with email: {model.EmailAddress}");
                return BadRequest(ModelState);
            }

            // Check if user already exists
            if (await _userManager.FindByEmailAsync(model.EmailAddress) != null)
            {
                _logger.LogWarning($"User with email {model.EmailAddress} already exists.");
                return BadRequest("User with this email already exists.");
            }

            _logger.LogInformation($"Employee registration initiated by employee for email: {model.EmailAddress}");

            // Create new user entity
            var user = new ApplicationUser
            {
                UserName = model.EmailAddress,
                Email = model.EmailAddress,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            // Persist user
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Failed to register employee with email: {model.EmailAddress}. Errors: {result.Errors}");
                return BadRequest(result.Errors);
            }

            // Assign Employee role
            var roleResult = await _userManager.AddToRoleAsync(user, RolesEnum.Employee.ToString());

            if (!roleResult.Succeeded)
            {
                _logger.LogError($"Failed to assign 'Employee' role to user: {model.EmailAddress}. Errors: {roleResult.Errors}");
                return StatusCode(500, "User created but role assignment failed.");
            }

            _logger.LogInformation($"Employee registered successfully and role assigned for email: {model.EmailAddress}");

            return Ok();
        }

        /// <summary>
        /// Authenticates a user and issues a JWT token if credentials are valid.
        /// </summary>
        /// <param name="model">Login credentials.</param>
        /// <returns>JWT token and user ID on success, or error status.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for login with email: {model.Email}");
                return BadRequest(ModelState);
            }

            // Retrieve user by email
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                _logger.LogWarning($"User with email {model.Email} not found.");
                return Unauthorized("User not found.");
            }

            // Check password
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Invalid password for user with email: {model.Email}");
                return Unauthorized("Invalid credentials.");
            }

            // Retrieve user roles
            var roles = await _userManager.GetRolesAsync(user);

            if (roles == null || roles.Count == 0)
            {
                _logger.LogWarning($"User with email {model.Email} has no roles assigned.");
                return Unauthorized("User has no role assigned.");
            }

            // Validate role enum
            if (!Enum.TryParse<RolesEnum>(roles[0], out var userRole))
            {
                _logger.LogWarning($"Invalid role for user with email {model.Email}");
                return Unauthorized("Invalid role.");
            }

            // Generate JWT token
            var token = _tokenService.CreateToken(user, userRole);

            _logger.LogInformation($"User with email {model.Email} logged in successfully. Token generated.");

            return Ok(new { token, user.Id });
        }
    }
}