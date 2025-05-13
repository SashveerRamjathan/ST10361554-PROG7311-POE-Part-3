using Agri_Energy_Connect_API.Services;
using DataContextAndModels.Enums;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;

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

        [Authorize(Roles = "Employee")]
        [HttpPost("register/farmer")]
        public async Task<IActionResult> RegisterFarmer([FromBody] FarmerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for farmer registration with email: {model.EmailAddress}");
                return BadRequest(ModelState);
            }
            
            if (await _userManager.FindByEmailAsync(model.EmailAddress) != null)
            {
                _logger.LogWarning($"User with email {model.EmailAddress} already exists.");
                return BadRequest("User with this email already exists.");
            }

            _logger.LogInformation($"Farmer registration initiated by employee for email: {model.EmailAddress}");

            var user = new ApplicationUser 
            { 
                UserName = model.EmailAddress, 
                Email = model.EmailAddress,
                Address = model.Address,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };
            
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Failed to register farmer with email: {model.EmailAddress}. Errors: {result.Errors}");
                return BadRequest(result.Errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RolesEnum.Farmer.ToString());

            if (!roleResult.Succeeded)
            {
                _logger.LogError($"Failed to assign 'Farmer' role to user: {model.EmailAddress}. Errors: {roleResult.Errors}");
                return StatusCode(500, "User created but role assignment failed.");
            }

            _logger.LogInformation($"Farmer registered successfully and role assigned for email: {model.EmailAddress}");

            return Ok();
        }

        [Authorize(Roles = "Employee")]
        [HttpPost("register/employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] EmployeeRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for employee registration with email: {model.EmailAddress}");
                return BadRequest(ModelState);
            }

            if (await _userManager.FindByEmailAsync(model.EmailAddress) != null)
            {
                _logger.LogWarning($"User with email {model.EmailAddress} already exists.");
                return BadRequest("User with this email already exists.");
            }

            _logger.LogInformation($"Employee registration initiated by employee for email: {model.EmailAddress}");

            var user = new ApplicationUser 
            { 
                UserName = model.EmailAddress, 
                Email = model.EmailAddress,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                // Log the errors
                _logger.LogWarning($"Failed to register employee with email: {model.EmailAddress}. Errors: {result.Errors}");
                return BadRequest(result.Errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RolesEnum.Employee.ToString());
            
            if (!roleResult.Succeeded)
            {
                // Log the errors
                _logger.LogError($"Failed to assign 'Employee' role to user: {model.EmailAddress}. Errors: {roleResult.Errors}");
                return StatusCode(500, "User created but role assignment failed.");
            }

            _logger.LogInformation($"Employee registered successfully and role assigned for email: {model.EmailAddress}");

            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid model state for login with email: {model.Email}");
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                _logger.LogWarning($"User with email {model.Email} not found.");
                return Unauthorized("User not found."); 
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Invalid password for user with email: {model.Email}");
                return Unauthorized("Invalid credentials.");
            }
            var roles = await _userManager.GetRolesAsync(user);

            if (roles == null || roles.Count == 0)
            {
                _logger.LogWarning($"User with email {model.Email} has no roles assigned.");
                return Unauthorized("User has no role assigned.");
            }

            if (!Enum.TryParse<RolesEnum>(roles[0], out var userRole))
            {
                _logger.LogWarning($"Invalid role for user with email {model.Email}");
                return Unauthorized("Invalid role.");
            }

            var token = _tokenService.CreateToken(user, userRole);

            _logger.LogInformation($"User with email {model.Email} logged in successfully. Token generated.");

            return Ok(new { token, user.Id });
        }


    }
}
