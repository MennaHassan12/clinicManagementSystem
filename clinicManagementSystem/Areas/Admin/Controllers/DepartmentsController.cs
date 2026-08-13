using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DepartmentsController : Controller
    {
        private readonly IRepository<Department> _departmentRepository;

        public DepartmentsController(IRepository<Department> departmentRepository)
        {
            _departmentRepository = departmentRepository;
        }

        // GET: Admin/Departments
        public async Task<IActionResult> Index()
        {
            var departments = await _departmentRepository.GetAsync(
                orderBy: q => q.OrderBy(d => d.Name),
                tracked: false
            );

            return View(departments);
        }

        // GET: Admin/Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (!ModelState.IsValid)
            {
                return View(department);
            }

            await _departmentRepository.CreateAsync(department);
            await _departmentRepository.CommitAsync();

            TempData["Success"] = "Department created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Departments/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _departmentRepository.GetOneAsync(
                d => d.DepartmentId == id
            );

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Admin/Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.DepartmentId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(department);
            }

            _departmentRepository.Update(department);
            await _departmentRepository.CommitAsync();

            TempData["Success"] = "Department updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Departments/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _departmentRepository.GetOneAsync(
                d => d.DepartmentId == id
            );

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Admin/Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _departmentRepository.GetOneAsync(
                d => d.DepartmentId == id,
                includes: new System.Linq.Expressions.Expression<Func<Department, object>>[]
                {
                    d => d.Doctors
                }
            );

            if (department == null)
            {
                return NotFound();
            }

            // Department cannot be deleted if it has doctors
            if (department.Doctors.Any())
            {
                TempData["Error"] = "This department cannot be deleted because it has doctors assigned to it.";
                return RedirectToAction(nameof(Index));
            }

            _departmentRepository.Delete(department);
            await _departmentRepository.CommitAsync();

            TempData["Success"] = "Department deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}