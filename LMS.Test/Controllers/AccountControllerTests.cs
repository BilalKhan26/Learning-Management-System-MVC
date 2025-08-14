using LMS.Controllers;
using LMS.Data;
using LMS.Models;
using LMS.Service;
using LMS.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace LMS.Tests.Controllers
{
    public class AccountControllerTests
    {
        [Fact]
        public async Task Login_Post_ShouldRedirectToDashboard_WhenCredentialsValid()
        {
            // Arrange
            var configData = new Dictionary<string, string>
    {
        { "JwtSettings:Secret", "supersecretkeyforsigningjwtsthatislongenough" },
        { "JwtSettings:Issuer", "TestIssuer" },
        { "JwtSettings:Audience", "TestAudience" },
        { "JwtSettings:EmailTokenExpirationHours", "1" }
    };

            var fakeConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var jwtEmailService = new JwtEmailService(fakeConfig);

            var mockUserManager = MockUserManager();
            var mockSignInManager = MockSignInManager(mockUserManager);

            var mockEmailSender = new Mock<EmailSender>(fakeConfig, Mock.Of<ILogger<EmailSender>>());

            var testUser = new ApplicationUser
            {
                Id = "instr-123",
                Email = "instructor@lms.com",
                UserName = "instructor@lms.com",
                EmailConfirmed = true
            };

            mockUserManager.Setup(x => x.FindByEmailAsync("instructor@lms.com"))
                .ReturnsAsync(testUser);
            mockUserManager.Setup(x => x.GetRolesAsync(testUser))
                .ReturnsAsync(new[] { "Instructor" });

            // ✅ Fix — ensure login succeeds
            mockSignInManager
                .Setup(s => s.PasswordSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var controller = new AccountController(
                mockSignInManager.Object,
                mockUserManager.Object,
                jwtEmailService,
                mockEmailSender.Object,
                fakeConfig
            );

            // Act
            var result = await controller.Login("instructor@lms.com", "Instructor123!", null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("InstructorDashboard", redirectResult.ControllerName);
        }

        // Remove one of the duplicate MockUserManager and MockSignInManager methods
        // Keep only one definition for each helper method

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private static Mock<SignInManager<ApplicationUser>> MockSignInManager(Mock<UserManager<ApplicationUser>> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            return new Mock<SignInManager<ApplicationUser>>(userManager.Object,
                contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }


        //private static Mock<UserManager<ApplicationUser>> MockUserManager()
        //{
        //    var store = new Mock<IUserStore<ApplicationUser>>();
        //    return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        //}

        //private static Mock<SignInManager<ApplicationUser>> MockSignInManager(Mock<UserManager<ApplicationUser>> userManager)
        //{
        //    var contextAccessor = new Mock<IHttpContextAccessor>();
        //    var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        //    return new Mock<SignInManager<ApplicationUser>>(userManager.Object,
        //        contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        //}
    }
}
