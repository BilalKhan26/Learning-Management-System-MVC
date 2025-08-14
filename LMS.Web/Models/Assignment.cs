using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required] public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }
        public string? MediaPath { get; set; } // ✅ Store file path


        public ICollection<Submission>? Submissions { get; set; }
    }
}
