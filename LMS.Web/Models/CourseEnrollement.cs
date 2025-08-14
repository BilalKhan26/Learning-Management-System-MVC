namespace LMS.Models
{
    public class CourseEnrollment
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public string StudentId { get; set; } = null!;
        public ApplicationUser? Student { get; set; }
    }
}
