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
    public class LessonNegativeTests
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
        public async Task Lessons_ShouldReturnEmpty_WhenCourseHasNoLessons()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("LessonNegativeTest1")
                .Options;
            using var context = new ApplicationDbContext(options);

            context.Courses.Add(new Course { Id = 1, Title = "Empty Lessons", InstructorId = "instr-1" });
            await context.SaveChangesAsync();

            var controller = GetController(context, "student-123");

            var result = await controller.Lessons(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var lessons = Assert.IsAssignableFrom<IEnumerable<Lesson>>(viewResult.Model);
            Assert.Empty(lessons);
        }

        [Fact]
        public async Task Lessons_ShouldReturnEmpty_WhenCourseDoesNotExist()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("LessonNegativeTest2")
                .Options;
            using var context = new ApplicationDbContext(options);

            var controller = GetController(context, "student-123");

            var result = await controller.Lessons(99);

            var viewResult = Assert.IsType<ViewResult>(result);
            var lessons = Assert.IsAssignableFrom<IEnumerable<Lesson>>(viewResult.Model);
            Assert.Empty(lessons);
        }
    }
 }
