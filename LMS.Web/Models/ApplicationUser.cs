using Microsoft.AspNetCore.Identity;

namespace LMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }

        public ICollection<Course>? CoursesTaught { get; set; }
        public ICollection<CourseEnrollment>? Enrollments { get; set; }
        public ICollection<Submission>? Submissions { get; set; }
    }
}
