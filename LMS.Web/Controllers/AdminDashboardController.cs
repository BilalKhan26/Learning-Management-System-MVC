using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMS.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace LMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public AdminDashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.DisplayName = user?.DisplayName ?? user?.UserName;
            return View();
        }
    }
}
