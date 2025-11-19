namespace StudentManagement.Models;

public class StudentEnrollmentViewModel
{
    public Student? Student { get; set; }
    public List<Course> Courses { get; set; } = new();
}
