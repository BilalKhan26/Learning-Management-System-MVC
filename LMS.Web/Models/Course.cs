using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course title is required.")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        // Make InstructorId NOT validated by the model binder during form submission
        [Required]
        [ScaffoldColumn(false)] // hides from forms automatically
        public string InstructorId { get; set; } = null!;

        [ForeignKey(nameof(InstructorId))]
        public ApplicationUser? Instructor { get; set; }

        public ICollection<Lesson>? Lessons { get; set; }
        public ICollection<Assignment>? Assignments { get; set; }
        public ICollection<CourseEnrollment>? Enrollments { get; set; }
    }
}
