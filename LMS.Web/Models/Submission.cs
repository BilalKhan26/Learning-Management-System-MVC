using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LMS.Models
{
    public class Submission
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        public string StudentId { get; set; } = null!;
        public ApplicationUser? Student { get; set; }

        public string? Content { get; set; }
        public string? FilePath { get; set; }
        public DateTime? SubmittedAt { get; set; }

        [Range(1, 4, ErrorMessage = "Grade should be between 1-4, 4 being the best and 1 being the worst.")]
        public double? Grade { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
    }
}
