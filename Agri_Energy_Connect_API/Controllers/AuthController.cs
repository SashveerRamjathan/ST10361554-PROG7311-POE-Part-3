using Agri_Energy_Connect_API.Services;
using DataContextAndModels.Enums;
using DataContextAndModels.Models;
using DataContextAndModels.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Agri_Energy_Connect_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TokenService _tokenService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("register/farmer")]
        public async Task<IActionResult> RegisterFarmer([FromBody] FarmerRegisterViewModel model)
        {
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
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, RolesEnum.Farmer.ToString());

            return Ok("Farmer registered successfully.");
        }

        [HttpPost("register/employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] EmployeeRegisterViewModel model)
        {
            var user = new ApplicationUser 
            { 
                UserName = model.EmailAddress, 
                Email = model.EmailAddress,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, RolesEnum.Employee.ToString());
            return Ok("Employee registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return Unauthorized("User not found.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
                return Unauthorized("Invalid credentials.");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles == null || roles.Count == 0)
                return Unauthorized("User has no role assigned.");

            if (!Enum.TryParse<RolesEnum>(roles[0], out var userRole))
                return Unauthorized("Invalid role.");

            var token = _tokenService.CreateToken(user, userRole);

            return Ok(new { token });
        }


    }
}
