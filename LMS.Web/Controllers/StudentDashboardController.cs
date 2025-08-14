using LMS.Data;
using LMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LMS.Web.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var studentId = _userManager.GetUserId(User); // just the ID
            var enrolledCourses = await _context.CourseEnrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Instructor)
                .Where(e => e.StudentId == studentId) // use plain string here
                .Select(e => e.Course)
                .ToListAsync();

            var enrolledIds = enrolledCourses.Select(c => c.Id).ToList();

            var availableCourses = await _context.Courses
                .Include(c => c.Instructor)
                .Where(c => !enrolledIds.Contains(c.Id))
                .ToListAsync();

            var vm = new StudentIndexViewModel
            {
                EnrolledCourses = enrolledCourses,
                AvailableCourses = availableCourses
            };

            return View(vm);
        }


        public async Task<IActionResult> ViewCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            return View(course); // <-- expects a ViewCourse.cshtml
        }


        // List all available courses for enrollment
        public async Task<IActionResult> AvailableCourses()
        {
            var student = await _userManager.GetUserAsync(User);

            var enrolledIds = await _context.CourseEnrollments
                .Where(e => e.StudentId == student.Id)
                .Select(e => e.CourseId)
                .ToListAsync();

            var availableCourses = await _context.Courses
                .Include(c => c.Instructor)
                .Where(c => !enrolledIds.Contains(c.Id))
                .ToListAsync();

            return View(availableCourses);
        }

        // My courses (already enrolled)
        // My courses (already enrolled)
        public async Task<IActionResult> MyCourses(string? search, int page = 1)
        {
            var student = await _userManager.GetUserAsync(User);
            int pageSize = 5;

            var query = _context.CourseEnrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
                .Where(e => e.StudentId == student.Id)
                .Select(e => e.Course);

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


        // Enroll in a course
        [HttpPost]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var student = await _userManager.GetUserAsync(User);

            bool alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == student.Id);

            if (!alreadyEnrolled)
            {
                var enrollment = new CourseEnrollment
                {
                    CourseId = courseId,
                    StudentId = student.Id
                };
                _context.CourseEnrollments.Add(enrollment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyCourses));
        }

        // Unenroll from a course
        [HttpPost]
        public async Task<IActionResult> Unenroll(int courseId)
        {
            var student = await _userManager.GetUserAsync(User);

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == student.Id);

            if (enrollment != null)
            {
                _context.CourseEnrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyCourses));
        }


        // --------------------------------
        // Lessons Management
        // --------------------------------
        public async Task<IActionResult> Lessons(int courseId)
        {
            var lessons = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .ToListAsync();
            ViewBag.CourseId = courseId;
            return View(lessons);
        }



        // --------------------------------
        // Assignments Management
        // --------------------------------
        public async Task<IActionResult> Assignments(int courseId)
        {
            var student = await _userManager.GetUserAsync(User);

            var assignments = await _context.Assignments
                .Where(a => a.CourseId == courseId)
                .Include(a => a.Submissions)
                .ToListAsync();

            // Attach student's submission info to view
            var studentSubmissions = await _context.Submissions
                .Where(s => s.StudentId == student.Id)
                .ToListAsync();

            ViewBag.StudentSubmissions = studentSubmissions;
            ViewBag.CourseId = courseId;

            return View(assignments);
        }

        [HttpGet]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, string search, int page = 1)
        {
            var student = await _userManager.GetUserAsync(User);
            if (student == null) return Unauthorized();

            int pageSize = 5;

            var query = _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.AssignmentId == assignmentId && s.StudentId == student.Id);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(s =>
                    (s.Content != null && s.Content.Contains(search)) ||
                    (s.Feedback != null && s.Feedback.Contains(search)));
            }

            var totalSubmissions = await query.CountAsync();
            var submissions = await query
                .OrderByDescending(s => s.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.AssignmentId = assignmentId;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalSubmissions / (double)pageSize);

            return View(submissions);
        }


        // ============================
        // Submit Assignment (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, IFormFile submissionFile)
        {
            var student = await _userManager.GetUserAsync(User);
            if (student == null) return Unauthorized();

            if (submissionFile != null && submissionFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "submissions");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(submissionFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await submissionFile.CopyToAsync(stream);
                }

                // Save submission
                var submission = new Submission
                {
                    AssignmentId = assignmentId,
                    StudentId = student.Id,
                    FilePath = "/submissions/" + fileName,
                    SubmittedAt = DateTime.UtcNow
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Assignment submitted successfully!";
            }

            var courseId = await _context.Assignments
                .Where(a => a.Id == assignmentId)
                .Select(a => a.CourseId)
                .FirstOrDefaultAsync();

            return RedirectToAction(nameof(Assignments), new { courseId });
        }


        // --------------------------------
        // View & Grade Submissions
        // --------------------------------
        public async Task<IActionResult> Grades(int courseId)
        {
            var student = await _userManager.GetUserAsync(User);

            var submissions = await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.StudentId == student.Id && s.Assignment.CourseId == courseId)
                .ToListAsync();

            ViewBag.CourseId = courseId;
            return View(submissions);
        }
    }
}
