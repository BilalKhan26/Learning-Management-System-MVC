using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required] public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public string? MediaPath { get; set; } // ✅ Store file path


        public int CourseId { get; set; }
        public Course? Course { get; set; }
    }
}
