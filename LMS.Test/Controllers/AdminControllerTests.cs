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
    public class AdminControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ApplicationDbContext _context;


        public AdminControllerTests()
        {
            _mockUserManager = MockUserManager();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        private AdminDashboardController GetController(ApplicationDbContext context, string role = "Admin")
        {
            var mockUserManager = MockUserManager();

            var controller = new AdminDashboardController(context, mockUserManager.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-123"),
                new Claim(ClaimTypes.Email, "admin@lms.com"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            return controller;
        }

        // ✅ POSITIVE TEST: Get list of instructors
        [Fact]
        public async Task Instructors_ShouldReturnList_WhenInstructorsExist()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            context.Users.Add(new ApplicationUser { Id = "instr-1", UserName = "john", Email = "john@lms.com" });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            var result = await controller.Instructors();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationUser>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("john", model.First().UserName);
        }

        // ❌ NEGATIVE TEST: Instructors returns empty list
        [Fact]
        public async Task Instructors_ShouldReturnEmptyList_WhenNoInstructorsExist()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);

            var result = await controller.Instructors();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationUser>>(viewResult.Model);
            Assert.Empty(model);
        }

        // ✅ POSITIVE TEST: Get courses list
        [Fact]
        public async Task Courses_ShouldReturnList_WhenCoursesExist()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var course = new Course { Id = 1, Title = "C# Basics" };
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            var result = await controller.Courses();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("C# Basics", model.First().Title);
        }

        // ❌ NEGATIVE TEST: Courses returns empty list
        [Fact]
        public async Task Courses_ShouldReturnEmptyList_WhenNoCoursesExist()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);

            var result = await controller.Courses();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.Model);
            Assert.Empty(model);
        }

        // ✅ POSITIVE TEST: CourseDetails shows course
        [Fact]
        public async Task CourseDetails_ShouldReturnCourse_WhenIdExists()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var course = new Course { Id = 2, Title = "Java Basics" };
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            var result = await controller.CourseDetails(2);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Course>(viewResult.Model);
            Assert.Equal("Java Basics", model.Title);
        }

        // ❌ NEGATIVE TEST: CourseDetails returns NotFound
        [Fact]
        public async Task CourseDetails_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);

            var result = await controller.CourseDetails(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Instructors_ShouldReturnList()
        {
            // Arrange
            var instructors = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "instr1", Email = "i1@test.com" }
            };
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Instructor"))
                .ReturnsAsync(instructors);

            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.Instructors();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationUser>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Students_ShouldReturnList()
        {
            // Arrange
            var students = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "2", UserName = "stud1", Email = "s1@test.com" }
            };
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Student"))
                .ReturnsAsync(students);

            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.Students();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationUser>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task AddInstructor_ShouldAdd_WhenValid()
        {
            // Arrange
            var newInstr = new ApplicationUser { UserName = "newinstr", Email = "newinstr@test.com" };

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Instructor"))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.CreateInstructor(newInstr, "Pass123!");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Instructors", redirect.ActionName);
        }

        [Fact]
        public async Task AddInstructor_ShouldFail_WhenInvalid()
        {
            // Arrange
            var newInstr = new ApplicationUser { UserName = "bad", Email = "bad@test.com" };

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Bad password" }));

            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.CreateInstructor(newInstr, "short");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Instructors", viewResult.ViewName);
        }

        [Fact]
        public async Task DeleteUser_ShouldRedirect_WhenFound()
        {
            // Arrange
            var user = new ApplicationUser { Id = "delete1" };
            _mockUserManager.Setup(m => m.FindByIdAsync("delete1")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.DeleteInstructor("delete1");


            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Instructors", redirect.ActionName);
        }

        [Fact]
        public async Task Courses_ShouldReturnList()
        {
            // Arrange
            _context.Courses.Add(new Course { Id = 1, Title = "Programming Fundamentals" });
            await _context.SaveChangesAsync();

            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.Courses();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task CourseDetails_ShouldReturnCourse_WhenFound()
        {
            // Arrange
            var course = new Course { Id = 10, Title = "Java", Lessons = new List<Lesson>(), Assignments = new List<Assignment>() };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.CourseDetails(10);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Course>(viewResult.Model);
            Assert.Equal("Java", model.Title);
        }

        [Fact]
        public async Task CourseDetails_ShouldReturnNotFound_WhenMissing()
        {
            // Arrange
            var controller = new AdminDashboardController(_context, _mockUserManager.Object);

            // Act
            var result = await controller.CourseDetails(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

       
    }
}
