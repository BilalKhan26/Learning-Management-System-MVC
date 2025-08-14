using LMS.Models;
using Microsoft.AspNetCore.Identity;

namespace LMS.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Instructor", "Student" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin
            if (await userManager.FindByEmailAsync("admin@lms.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@lms.com",
                    Email = "admin@lms.com",
                    DisplayName = "System Admin",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed Instructor
            if (await userManager.FindByEmailAsync("instructor@lms.com") == null)
            {
                var instructor = new ApplicationUser
                {
                    UserName = "instructor@lms.com",
                    Email = "instructor@lms.com",
                    DisplayName = "Test Instructor",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(instructor, "Instructor123!");
                await userManager.AddToRoleAsync(instructor, "Instructor");
            }

            // Seed Student
            if (await userManager.FindByEmailAsync("student@lms.com") == null)
            {
                var student = new ApplicationUser
                {
                    UserName = "student@lms.com",
                    Email = "student@lms.com",
                    DisplayName = "Test Student",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(student, "Student123!");
                await userManager.AddToRoleAsync(student, "Student");
            }
        }
    }
}
