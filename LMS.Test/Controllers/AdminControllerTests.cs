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
    public class AdminDashboardControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

        public AdminDashboardControllerTests()
        {
            _mockUserManager = MockUserManager();
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
            var controller = new AdminDashboardController(context, _mockUserManager.Object);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-1"),
                new Claim(ClaimTypes.Email, "admin@test.com"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        [Fact]
        public async Task Index_ShouldSet_DisplayName()
        {
            var context = new ApplicationDbContext(_dbOptions);
            var user = new ApplicationUser { Id = "admin-1", UserName = "AdminUser", DisplayName = "Admin Name" };
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = GetController(context);
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Admin Name", controller.ViewBag.DisplayName);
        }

        [Fact]
        public async Task Instructors_ShouldReturnList_WhenExists()
        {
            var instructors = new List<ApplicationUser> { new ApplicationUser { Id = "i1", UserName = "john" } };
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Instructor")).ReturnsAsync(instructors);

            var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);

            var result = await controller.Instructors();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationUser>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Instructors_ShouldReturnEmpty_WhenNone()
        {
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Instructor")).ReturnsAsync(new List<ApplicationUser>());

            var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);

            var result = await controller.Instructors();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationUser>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task CreateInstructor_ShouldRedirect_WhenValid()
        {
            var context = new ApplicationDbContext(_dbOptions);
            var newInstr = new ApplicationUser { Email = "i@test.com" };

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Instructor")).ReturnsAsync(IdentityResult.Success);

            var controller = GetController(context);
            var result = await controller.CreateInstructor(newInstr, "Pass123!");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Instructors", redirect.ActionName);
        }

        [Fact]
        public async Task CreateInstructor_ShouldReturnView_WhenInvalid()
        {
            var context = new ApplicationDbContext(_dbOptions);
            var newInstr = new ApplicationUser { Email = "bad@test.com" };

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            var controller = GetController(context);
            var result = await controller.CreateInstructor(newInstr, "bad");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(newInstr, viewResult.Model);
        }

        [Fact]
        public async Task EditInstructor_ShouldUpdate_WhenValid()
        {
            var instr = new ApplicationUser { Id = "i1", Email = "old@test.com" };
            _mockUserManager.Setup(m => m.FindByIdAsync("i1")).ReturnsAsync(instr);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);
            var result = await controller.EditInstructor(new ApplicationUser { Id = "i1", Email = "new@test.com" });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Instructors", redirect.ActionName);
        }

        [Fact]
        public async Task EditInstructor_ShouldReturnNotFound_WhenMissing()
        {
            _mockUserManager.Setup(m => m.FindByIdAsync("x")).ReturnsAsync((ApplicationUser)null);

            var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);
            var result = await controller.EditInstructor(new ApplicationUser { Id = "x" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteInstructor_ShouldRedirect_WhenExists()
        {
            var instr = new ApplicationUser { Id = "i1" };
            _mockUserManager.Setup(m => m.FindByIdAsync("i1")).ReturnsAsync(instr);
            _mockUserManager.Setup(m => m.DeleteAsync(instr)).ReturnsAsync(IdentityResult.Success);

            var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);
            var result = await controller.DeleteInstructor("i1");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Instructors", redirect.ActionName);
        }

        [Fact]
        public async Task Students_ShouldReturnList()
        {
            var students = new List<ApplicationUser> { new ApplicationUser { Id = "s1" } };
            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Student")).ReturnsAsync(students);

            var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);
            var result = await controller.Students();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationUser>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Courses_ShouldReturnList()
        {
            var context = new ApplicationDbContext(_dbOptions);

            // Create instructor so FK InstructorId is valid
            var instructor = new ApplicationUser
            {
                Id = "inst-1",
                UserName = "instructor1",
                Email = "instructor1@test.com"
            };
            context.Users.Add(instructor);

            // Add course with valid InstructorId
            context.Courses.Add(new Course
            {
                Id = 1,
                Title = "C# Basics",
                InstructorId = instructor.Id,
                Instructor = instructor
            });

            await context.SaveChangesAsync();

            var controller = GetController(context);
            var result = await controller.Courses();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.Model);
            Assert.Single(model);
        }


        [Fact]
        public async Task CourseDetails_ShouldReturnView_WhenFound()
        {
            var context = new ApplicationDbContext(_dbOptions);

            var instructor = new ApplicationUser
            {
                Id = "i100",
                UserName = "instructor1",
                Email = "instructor@lms.com"
            };
            context.Users.Add(instructor);

            context.Courses.Add(new Course
            {
                Id = 5,
                Title = "Java Basics",
                InstructorId = instructor.Id,
                Instructor = instructor
            });

            await context.SaveChangesAsync();

            var controller = GetController(context);
            var result = await controller.CourseDetails(5);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Course>(viewResult.Model);
            Assert.Equal("Java Basics", model.Title);
        }


        [Fact]
        public async Task CourseDetails_ShouldReturnNotFound_WhenMissing()
        {
            var context = new ApplicationDbContext(_dbOptions);
            var controller = GetController(context);
            var result = await controller.CourseDetails(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
