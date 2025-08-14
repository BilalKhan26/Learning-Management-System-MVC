using LMS.Data;
using LMS.Models;
using LMS.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace LMS.Tests.Controllers
{
    public class StudentDashboardTests
    {
        [Fact]
        public async Task Index_ShouldReturnOnlyEnrolledCourses()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique DB each run
                .Options;

            using var context = new ApplicationDbContext(options);

            var studentId = "student-123";

            // Seed instructors
            var instructor1 = new ApplicationUser
            {
                Id = "instr-123",
                UserName = "instructor1@lms.com",
                Email = "instructor1@lms.com"
            };
            var instructor2 = new ApplicationUser
            {
                Id = "instr-456",
                UserName = "instructor2@lms.com",
                Email = "instructor2@lms.com"
            };
            context.Users.AddRange(instructor1, instructor2);

            // Seed courses with instructors
            var course1 = new Course
            {
                Id = 1,
                Title = "Programming Fundamentals",
                InstructorId = instructor1.Id,
                Instructor = instructor1
            };
            var course2 = new Course
            {
                Id = 2,
                Title = "Object Oriented Programming",
                InstructorId = instructor2.Id,
                Instructor = instructor2
            };
            context.Courses.AddRange(course1, course2);

            // Seed enrollment for student in course1
            context.CourseEnrollments.Add(new CourseEnrollment
            {
                Id = 1,
                CourseId = course1.Id,
                StudentId = studentId,
                Course = course1
            });

            await context.SaveChangesAsync();

            // Mock UserManager to return studentId
            var mockUserManager = MockUserManager();
            mockUserManager
                .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(studentId);

            var controller = new StudentDashboardController(context, mockUserManager.Object);

            // Fake logged-in student
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, studentId),
                new Claim(ClaimTypes.Email, "student@lms.com"),
                new Claim(ClaimTypes.Role, "Student")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<StudentIndexViewModel>(viewResult.Model);

            Assert.Single(model.EnrolledCourses); // should only be enrolled in course1
            Assert.Equal("Programming Fundamentals", model.EnrolledCourses.First().Title);
            Assert.Single(model.AvailableCourses);
            Assert.Equal("Object Oriented Programming", model.AvailableCourses.First().Title);
            //Error when Actual Course1 fails to match Expected Course1
            //Assert.Equal("Data Structure and Algorithm", model.AvailableCourses.First().Title);
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }
    }
}
