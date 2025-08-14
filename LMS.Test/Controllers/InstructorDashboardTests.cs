using LMS.Data;
using LMS.Models;
using LMS.Tests.Helpers;
using LMS.Web.Controllers;
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
    public class InstructorDashboardTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public InstructorDashboardTests()
        {
            _context = TestHelper.GetInMemoryDbContext("InstructorDashboardTestsDB");

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Index_ShouldReturnOnlyInstructorsCourses()
        {
            // Arrange
            var instructor = new ApplicationUser { Id = "instr-123", Email = "instructor@lms.com" };
            _context.Users.Add(instructor);


            var course1 = new Course { Id = 1, Title = "Instructor's Course", InstructorId = instructor.Id };
            var course2 = new Course { Id = 2, Title = "Other Course", InstructorId = "inst2" };
            _context.Courses.AddRange(course1, course2);

            await _context.SaveChangesAsync();

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                            .ReturnsAsync(instructor);

            var controller = new InstructorDashboardController(_context, _userManagerMock.Object);

            // Act
var result = await controller.Index(search: null, page: 1) as ViewResult;
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(result.Model);

            // Assert
            Assert.Single(model);
            Assert.Equal("Instructor's Course", model.First().Title);
        }

    }
}


//public async Task Index_ShouldReturnOnlyInstructorsCourses()
//{
//    // Arrange
//    var instructor = new ApplicationUser { Id = "inst1", UserName = "inst@test.com" };
//    _context.Users.Add(instructor);

//    var course1 = new Course { Id = 1, Title = "Instructor's Course", InstructorId = instructor.Id };
//    var course2 = new Course { Id = 2, Title = "Other Course", InstructorId = "inst2" };
//    _context.Courses.AddRange(course1, course2);

//    await _context.SaveChangesAsync();

//    _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//                    .ReturnsAsync(instructor);

//    var controller = new InstructorDashboardController(_context, _userManagerMock.Object);

//    // Act
//    var result = await controller.Index(search: null, page: 1) as ViewResult;
//    var model = Assert.IsAssignableFrom<IEnumerable<Course>>(result.Model);

//    // Assert
//    Assert.Single(model);
//    Assert.Equal("Instructor's Course", model.First().Title);
//}
