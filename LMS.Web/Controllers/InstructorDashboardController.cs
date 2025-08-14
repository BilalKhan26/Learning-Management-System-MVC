using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.Web.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InstructorDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ----------------------
        // Courses
        // ----------------------
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            var instructor = await _userManager.GetUserAsync(User);
            int pageSize = 5; // Number of courses per page

            var query = _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                .Where(c => c.InstructorId == instructor.Id);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(c => c.Title.Contains(search) || c.Description.Contains(search));
            }

            var totalCourses = await query.CountAsync();
            var courses = await query
                .OrderBy(c => c.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCourses / (double)pageSize);

            return View(courses);
        }



        // GET: Create Course
        [HttpGet]
        public IActionResult CreateCourse()
        {
            return View();
        }

        // POST: Create Course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course)
        {
            var instructor = await _userManager.GetUserAsync(User);
            if (instructor == null)
            {
                ModelState.AddModelError("", "You must be logged in as an instructor to create a course.");
                return View(course);
            }

            // ✅ Set BEFORE validation
            course.InstructorId = instructor.Id;

            // ✅ Re-run validation after setting
            ModelState.Clear();
            TryValidateModel(course);

            if (ModelState.IsValid)
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Course created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(course);
        }


        // --------------------------------
        // Edit Course
        // --------------------------------
        // GET: Edit Course
        [HttpGet("EditCourse/{id}")]
        public async Task<IActionResult> EditCourse(int id)
        {
            var instructor = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id);

            if (course == null)
                return NotFound();

            return View(course);
        }

        [HttpPost("EditCourse/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, Course course)
        {
            var instructor = await _userManager.GetUserAsync(User);

            // Always set before validation
            course.InstructorId = instructor.Id;

            // Clear and revalidate model after setting InstructorId
            ModelState.Clear();
            TryValidateModel(course);

            if (id != course.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                var existingCourse = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id);

                if (existingCourse == null)
                    return NotFound();

                existingCourse.Title = course.Title;
                existingCourse.Description = course.Description;
                existingCourse.InstructorId = instructor.Id;

                _context.Update(existingCourse);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Course updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(course);
        }




        // --------------------------------
        // Delete Course
        // --------------------------------
        [HttpGet]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            return View(course);
        }

        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ----------------------
        // Lessons
        // ----------------------
        public async Task<IActionResult> Lessons(int courseId)
        {
            var lessons = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .ToListAsync();

            ViewBag.CourseId = courseId;
            return View(lessons);
        }

        [HttpGet]
        public IActionResult CreateLesson(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(Lesson lesson, IFormFile mediaFile)
        {
            if (ModelState.IsValid)
            {
                if (mediaFile != null && mediaFile.Length > 0)
                {
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    var fileName = Guid.NewGuid() + Path.GetExtension(mediaFile.FileName);
                    var filePath = Path.Combine(uploadDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mediaFile.CopyToAsync(stream);
                    }

                    lesson.MediaPath = "/uploads/" + fileName;
                }

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Lessons), new { courseId = lesson.CourseId });
            }

            ViewBag.CourseId = lesson.CourseId;
            return View(lesson);
        }

        
        [HttpPost, ActionName("DeleteLesson")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Lessons), new { courseId = lesson.CourseId });
        }
        // ----------------------
        // Assignments
        // ----------------------
        public async Task<IActionResult> Assignments(int courseId, string search, int page = 1)
        {
            var assignments = await _context.Assignments
                .Where(a => a.CourseId == courseId)
                .ToListAsync();

            ViewBag.CourseId = courseId;
            int pageSize = 5;

            var instructor = await _userManager.GetUserAsync(User);

            var query = _context.Assignments
                .Where(a => a.CourseId == courseId && a.Course.InstructorId == instructor.Id);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(a => a.Title.Contains(search) || a.Description.Contains(search));
            }

            var totalAssignments = await query.CountAsync();
            var assignment = await query
                .OrderBy(a => a.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CourseId = courseId;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalAssignments / (double)pageSize);

            return View(assignments);
        }

        [HttpGet]
        public IActionResult CreateAssignment(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssignment(Assignment assignment, IFormFile? mediaFile)
        {
            if (ModelState.IsValid)
            {
                if (mediaFile != null && mediaFile.Length > 0)
                {
                    // Save file to wwwroot/uploads
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(mediaFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mediaFile.CopyToAsync(stream);
                    }

                    // Store relative path
                    assignment.MediaPath = "/uploads/" + fileName;
                }

                _context.Assignments.Add(assignment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Assignments), new { courseId = assignment.CourseId });
            }

            return View(assignment);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
                return NotFound();
            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Assignments), new { courseId = assignment.CourseId });
        }
        // ----------------------
        // View & Grade Submissions
        // ----------------------
        // View all submissions for a given assignment
        [HttpGet]
        public async Task<IActionResult> ViewSubmissions(int assignmentId, string search, int page = 1)
        {
            int pageSize = 5;

            var instructor = await _userManager.GetUserAsync(User);

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.Course.InstructorId == instructor.Id);

            if (assignment == null)
                return Unauthorized();

            var query = _context.Submissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignmentId);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(s => s.Student.DisplayName.Contains(search) || (s.Content != null && s.Content.Contains(search)));
            }

            var totalSubmissions = await query.CountAsync();
            var submissions = await query
                .OrderByDescending(s => s.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.AssignmentTitle = assignment.Title;
            ViewBag.AssignmentId = assignmentId;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalSubmissions / (double)pageSize);

            return View(submissions);
        }


        // GET: Grade a submission
        [HttpGet]
        public async Task<IActionResult> GradeSubmission(int submissionId)
        {
            var instructor = await _userManager.GetUserAsync(User);

            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.Assignment.Course.InstructorId == instructor.Id);

            if (submission == null)
                return Unauthorized();

            return View(submission);
        }

        // POST: Save grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeSubmission(int submissionId, decimal grade)
        {
            var instructor = await _userManager.GetUserAsync(User);

            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.Assignment.Course.InstructorId == instructor.Id);

            if (submission == null)
                return Unauthorized();

            submission.Grade = (double?)grade;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Grade saved successfully!";
            return RedirectToAction(nameof(ViewSubmissions), new { assignmentId = submission.AssignmentId });
        }

       
    }
}
