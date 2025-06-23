using DataContextAndModels.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

/*
    * Code Attribution
    * Purpose: ASP.NET Core MVC controller logic for view rendering, role-based access control, and error diagnostics
    * Author: Microsoft Docs (adapted for Agri-Energy Connect)
    * Date Accessed: 23 June 2025
    * Source: Microsoft Learn - Introduction to ASP.NET Core MVC
    * URL: https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
 */

/*
    * Controller: HomeController
    * Description: Handles the main navigation views for the Agri-Energy Connect web application,
    * including home, employee and farmer dashboards, privacy, about, and error pages.
    * Uses role-based authorization for protected endpoints, and includes logging for diagnostics.
 */

namespace Agri_Energy_Connect.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Constructor for HomeController, injects logger dependency.
        /// </summary>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns the main landing page.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Returns the dashboard view for employees. Only accessible to users in the "Employee" role.
        /// </summary>
        [Authorize(Roles = "Employee")]
        public IActionResult EmployeeIndex()
        {
            return View();
        }

        /// <summary>
        /// Returns the dashboard view for farmers. Only accessible to users in the "Farmer" role.
        /// </summary>
        [Authorize(Roles = "Farmer")]
        public IActionResult FarmerIndex()
        {
            return View();
        }

        /// <summary>
        /// Returns the Privacy page.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Returns the About page.
        /// </summary>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Returns the Error page with diagnostic information.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}