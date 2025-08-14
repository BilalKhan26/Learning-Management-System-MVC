using Xunit;
using LMS.Models;
using LMS.Data;
using LMS.Tests.Helpers;
using System.Threading.Tasks;
using System.Linq;

namespace LMS.Tests.Features
{
    public class SubmissionTests
    {
        private readonly ApplicationDbContext _context;

        public SubmissionTests()
        {
            _context = TestHelper.GetInMemoryDbContext("SubmissionTestsDB");
        }

        [Fact]
        public async Task SubmitAssignment_ShouldBeStoredInDatabase()
        {
            // Arrange
            var submission = new Submission
            {
                Id = 1,
                AssignmentId = 1,
                StudentId = "student1",
                FilePath = "/submissions/test.pdf"
            };
            _context.Submissions.Add(submission);

            // Act
            await _context.SaveChangesAsync();

            // Assert
            Assert.Single(_context.Submissions.ToList());
            Assert.Equal("student1", _context.Submissions.First().StudentId);
        }
    }
}
