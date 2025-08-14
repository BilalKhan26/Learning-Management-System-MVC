using LMS.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Tests.Helpers
{
    public static class TestHelper
    {
        public static ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }
    }
}
