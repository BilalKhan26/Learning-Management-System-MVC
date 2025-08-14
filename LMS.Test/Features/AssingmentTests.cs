using Xunit;
using LMS.Models;
using LMS.Data;
using LMS.Tests.Helpers;
using System.Threading.Tasks;
using System.Linq;

namespace LMS.Tests.Features
{
    public class AssignmentTests
    {
        private readonly ApplicationDbContext _context;

        public AssignmentTests()
        {
            _context = TestHelper.GetInMemoryDbContext("AssignmentTestsDB");
        }

        [Fact]
        public async Task AddAssignment_ShouldIncreaseAssignmentCount()
        {
            // Arrange
            var assignment = new Assignment { Id = 1, Title = "HW 1", CourseId = 1 };
            _context.Assignments.Add(assignment);

            // Act
            await _context.SaveChangesAsync();

            // Assert
            Assert.Single(_context.Assignments.ToList());
            Assert.Equal("HW 1", _context.Assignments.First().Title);
        }
    }
}
