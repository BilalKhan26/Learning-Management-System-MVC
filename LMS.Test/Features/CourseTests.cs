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
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LMS.Test.Features
{
    public class CourseNegativeTests
    {
        private StudentDashboardController GetController(ApplicationDbContext context, string studentId)
        {
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null
            );

            mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(studentId);
            mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new ApplicationUser { Id = studentId });

            var controller = new StudentDashboardController(context, mockUserManager.Object);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, studentId),
                new Claim(ClaimTypes.Email, "student@lms.com"),
                new Claim(ClaimTypes.Role, "Student")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            };

            return controller;
        }

        [Fact]
        public async Task ViewCourse_ShouldReturnNotFound_WhenCourseDoesNotExist()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("CourseNegativeTest1")
                .Options;
            using var context = new ApplicationDbContext(options);

            var controller = GetController(context, "student-123");

            var result = await controller.ViewCourse(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Enroll_ShouldReturnNotFound_WhenCourseDoesNotExist()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("CourseNegativeTest2")
                .Options;
            using var context = new ApplicationDbContext(options);

            var controller = GetController(context, "student-123");

            var result = await controller.Enroll(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Enroll_ShouldNotDuplicate_WhenAlreadyEnrolled()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("CourseNegativeTest3")
                .Options;
            using var context = new ApplicationDbContext(options);

            var course = new Course { Id = 1, Title = "C# Basics", InstructorId = "instr-1" };
            context.Courses.Add(course);
            context.CourseEnrollments.Add(new CourseEnrollment { CourseId = 1, StudentId = "student-123" });
            await context.SaveChangesAsync();

            var controller = GetController(context, "student-123");

            var result = await controller.Enroll(1);

            // Should redirect without creating duplicate enrollment
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyCourses", redirect.ActionName);

            var enrollmentCount = await context.CourseEnrollments.CountAsync();
            Assert.Equal(1, enrollmentCount);
        }
    }
}
