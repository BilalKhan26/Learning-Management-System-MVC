using LMS.Models;

public class StudentIndexViewModel
{
    public List<Course> EnrolledCourses { get; set; } = new();
    public List<Course> AvailableCourses { get; set; } = new();
}
