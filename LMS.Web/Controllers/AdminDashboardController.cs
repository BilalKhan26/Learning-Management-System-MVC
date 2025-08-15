using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.DisplayName = user?.DisplayName ?? user?.UserName;
            return View();
        }

        // ==========================
        // INSTRUCTORS CRUD
        // ==========================
        public async Task<IActionResult> Instructors()
        {
            var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
            return View(instructors);
        }

        [HttpGet]
        public IActionResult CreateInstructor() => View();

        [HttpPost]
        public async Task<IActionResult> CreateInstructor(ApplicationUser model, string password)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.Email;
                var result = await _userManager.CreateAsync(model, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(model, "Instructor");
                    return RedirectToAction(nameof(Instructors));
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditInstructor(string id)
        {
            var instructor = await _userManager.FindByIdAsync(id);
            if (instructor == null) return NotFound();
            return View(instructor);
        }

        [HttpPost]
        public async Task<IActionResult> EditInstructor(ApplicationUser model)
        {
            var instructor = await _userManager.FindByIdAsync(model.Id);
            if (instructor == null) return NotFound();

            instructor.Email = model.Email;
            instructor.DisplayName = model.DisplayName;
            var result = await _userManager.UpdateAsync(instructor);

            if (result.Succeeded)
                return RedirectToAction(nameof(Instructors));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteInstructor(string id)
        {
            var instructor = await _userManager.FindByIdAsync(id);
            if (instructor != null)
                await _userManager.DeleteAsync(instructor);

            return RedirectToAction(nameof(Instructors));
        }

        // ==========================
        // STUDENTS CRUD
        // ==========================
        public async Task<IActionResult> Students()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            return View(students);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStudent(ApplicationUser model, string password)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.Email;
                var result = await _userManager.CreateAsync(model, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(model, "Student");
                    return RedirectToAction(nameof(Students));
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            return RedirectToAction(nameof(Students));
        }

        [HttpPost]
        public async Task<IActionResult> EditStudent(ApplicationUser model)
        {
            var student = await _userManager.FindByIdAsync(model.Id);
            if (student == null) return NotFound();

            student.Email = model.Email;
            student.DisplayName = model.DisplayName;
            await _userManager.UpdateAsync(student);

            return RedirectToAction(nameof(Students));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student != null)
                await _userManager.DeleteAsync(student);

            return RedirectToAction(nameof(Students));
        }

        // ==========================
        // COURSES OVERVIEW
        // ==========================
        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .ToListAsync();

            return View(courses);
        }

        // ==========================
        // COURSE DETAILS (Lessons + Assignments)
        // ==========================
        public async Task<IActionResult> CourseDetails(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Assignments)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();
            return View(course);
        }
    }
}
