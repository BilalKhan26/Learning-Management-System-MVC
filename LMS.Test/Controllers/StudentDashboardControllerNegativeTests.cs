using LMS.Data;
using LMS.Models;
using LMS.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace LMS.Tests.Controllers
{
    public class StudentDashboardControllerNegativeTests
    {
        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        private static ControllerContext FakeStudentContext(string studentId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, studentId),
                new Claim(ClaimTypes.Role, "Student")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            return new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        }

        [Fact]
        public async Task Index_ShouldReturnEmptyCourses_WhenNoEnrollments()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var studentId = "student-1";

            var mockUserManager = MockUserManager();
            mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(studentId);

            var controller = new StudentDashboardController(context, mockUserManager.Object)
            {
                ControllerContext = FakeStudentContext(studentId)
            };

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<StudentIndexViewModel>(viewResult.Model);
            Assert.Empty(model.EnrolledCourses);
        }

        [Fact]
        public async Task ViewCourse_ShouldReturnNotFound_WhenCourseDoesNotExist()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var mockUserManager = MockUserManager();
            var controller = new StudentDashboardController(context, mockUserManager.Object);

            var result = await controller.ViewCourse(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Enroll_ShouldReturnNotFound_WhenCourseDoesNotExist()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var studentId = "student-1";

            var mockUserManager = MockUserManager();
            mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = studentId });

            var controller = new StudentDashboardController(context, mockUserManager.Object)
            {
                ControllerContext = FakeStudentContext(studentId)
            };

            var result = await controller.Enroll(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Enroll_ShouldNotDuplicateEnrollment_WhenAlreadyEnrolled()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var studentId = "student-1";

            var course = new Course { Id = 1, Title = "Test Course", InstructorId = "instr-1" };
            context.Courses.Add(course);
            context.CourseEnrollments.Add(new CourseEnrollment { CourseId = 1, StudentId = studentId });
            await context.SaveChangesAsync();

            var mockUserManager = MockUserManager();
            mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = studentId });

            var controller = new StudentDashboardController(context, mockUserManager.Object)
            {
                ControllerContext = FakeStudentContext(studentId)
            };

            await controller.Enroll(1);

            var count = await context.CourseEnrollments.CountAsync(e => e.CourseId == 1 && e.StudentId == studentId);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Unenroll_ShouldNotFail_WhenNotEnrolled()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var studentId = "student-1";

            var mockUserManager = MockUserManager();
            mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = studentId });

            var controller = new StudentDashboardController(context, mockUserManager.Object)
            {
                ControllerContext = FakeStudentContext(studentId)
            };

            var result = await controller.Unenroll(1);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Assignments_ShouldReturnEmptyList_WhenCourseHasNoAssignments()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var studentId = "student-1";

            var mockUserManager = MockUserManager();
            mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = studentId });

            var controller = new StudentDashboardController(context, mockUserManager.Object)
            {
                ControllerContext = FakeStudentContext(studentId)
            };

            var result = await controller.Assignments(1);
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Assignment>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Lessons_ShouldReturnEmptyList_WhenCourseHasNoLessons()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var mockUserManager = MockUserManager();
            var controller = new StudentDashboardController(context, mockUserManager.Object);

            var result = await controller.Lessons(1);
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Lesson>>(viewResult.Model);
            Assert.Empty(model);
        }
    }
}
