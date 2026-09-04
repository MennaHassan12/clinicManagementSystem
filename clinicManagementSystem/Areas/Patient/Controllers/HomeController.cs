using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using clinicManagementSystem.Data;
using clinicManagementSystem.Models;
using clinicManagementSystem.ViewModels;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity.UI.Services;
using DoctorModel = clinicManagementSystem.Models.Doctor;
using Microsoft.AspNetCore.Authorization;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();

            var featuredDoctors = await _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Department)
                .AsNoTracking()
                .Take(4)
                .ToListAsync();

            return View(featuredDoctors);
        }

        
    }
}