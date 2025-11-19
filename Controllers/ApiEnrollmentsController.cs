using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Data;
using StudentManagement.Models;

namespace StudentManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // TEMPORARILY DISABLED FOR TESTING
    public class ApiEnrollmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiEnrollmentsController> _logger;

        public ApiEnrollmentsController(ApplicationDbContext context, ILogger<ApiEnrollmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all enrollments
        /// </summary>
        /// <returns>List of all student-course enrollments</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEnrollments()
        {
            _logger.LogInformation("Getting all enrollments");
            var enrollments = await _context.StudentCourses
                .Include(sc => sc.Student)
                .Include(sc => sc.Course)
                .Select(sc => new
                {
                    sc.StudentId,
                    Student = new { sc.Student.Id, sc.Student.FullName, sc.Student.StudentNumber, sc.Student.Email },
                    sc.CourseId,
                    Course = new { sc.Course.Id, sc.Course.Code, sc.Course.Title, sc.Course.Credits },
                    sc.EnrolledOn
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        /// <summary>
        /// Get enrollments for a specific student
        /// </summary>
        /// <param name="studentId">Student ID</param>
        /// <returns>List of courses the student is enrolled in</returns>
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetStudentEnrollments(int studentId)
        {
            _logger.LogInformation($"Getting enrollments for student with ID: {studentId}");
            
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return NotFound(new { message = $"Student with ID {studentId} not found" });
            }

            var enrollments = await _context.StudentCourses
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course)
                .Select(sc => new
                {
                    sc.StudentId,
                    sc.CourseId,
                    Course = new { sc.Course.Id, sc.Course.Code, sc.Course.Title, sc.Course.Credits },
                    sc.EnrolledOn
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        /// <summary>
        /// Get enrollments for a specific course
        /// </summary>
        /// <param name="courseId">Course ID</param>
        /// <returns>List of students enrolled in the course</returns>
        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetCourseEnrollments(int courseId)
        {
            _logger.LogInformation($"Getting enrollments for course with ID: {courseId}");
            
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound(new { message = $"Course with ID {courseId} not found" });
            }

            var enrollments = await _context.StudentCourses
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student)
                .Select(sc => new
                {
                    sc.StudentId,
                    Student = new { sc.Student.Id, sc.Student.FullName, sc.Student.StudentNumber, sc.Student.Email },
                    sc.CourseId,
                    sc.EnrolledOn
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        /// <summary>
        /// Create a new enrollment (Admin only)
        /// </summary>
        /// <param name="enrollment">Enrollment data</param>
        /// <returns>Created enrollment</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEnrollment([FromBody] StudentCourse enrollment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if student exists
            var studentExists = await _context.Students.AnyAsync(s => s.Id == enrollment.StudentId);
            if (!studentExists)
            {
                return BadRequest(new { message = $"Student with ID {enrollment.StudentId} not found" });
            }

            // Check if course exists
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == enrollment.CourseId);
            if (!courseExists)
            {
                return BadRequest(new { message = $"Course with ID {enrollment.CourseId} not found" });
            }

            // Check if already enrolled
            var alreadyEnrolled = await _context.StudentCourses
                .AnyAsync(sc => sc.StudentId == enrollment.StudentId && sc.CourseId == enrollment.CourseId);
            if (alreadyEnrolled)
            {
                return BadRequest(new { message = "Student is already enrolled in this course" });
            }

            _logger.LogInformation($"Creating enrollment for student {enrollment.StudentId} in course {enrollment.CourseId}");
            enrollment.EnrolledOn = DateTime.UtcNow;
            _context.StudentCourses.Add(enrollment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudentEnrollments), new { studentId = enrollment.StudentId }, enrollment);
        }

        /// <summary>
        /// Delete an enrollment (Admin only)
        /// </summary>
        /// <param name="studentId">Student ID</param>
        /// <param name="courseId">Course ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{studentId}/{courseId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEnrollment(int studentId, int courseId)
        {
            _logger.LogInformation($"Deleting enrollment for student {studentId} in course {courseId}");
            
            var enrollment = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (enrollment == null)
            {
                return NotFound(new { message = $"Enrollment not found for student {studentId} in course {courseId}" });
            }

            _context.StudentCourses.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrollment deleted successfully" });
        }

        /// <summary>
        /// Get enrollment statistics
        /// </summary>
        /// <returns>Statistics about enrollments</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            _logger.LogInformation("Getting enrollment statistics");
            
            var totalEnrollments = await _context.StudentCourses.CountAsync();
            var totalStudents = await _context.Students.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var uniqueEnrolledStudents = await _context.StudentCourses.Select(sc => sc.StudentId).Distinct().CountAsync();
            var uniqueEnrolledCourses = await _context.StudentCourses.Select(sc => sc.CourseId).Distinct().CountAsync();

            var statistics = new
            {
                TotalEnrollments = totalEnrollments,
                TotalStudents = totalStudents,
                TotalCourses = totalCourses,
                UniqueEnrolledStudents = uniqueEnrolledStudents,
                UniqueEnrolledCourses = uniqueEnrolledCourses,
                AverageEnrollmentsPerStudent = totalStudents > 0 ? Math.Round((double)totalEnrollments / uniqueEnrolledStudents, 2) : 0,
                AverageEnrollmentsPerCourse = totalCourses > 0 ? Math.Round((double)totalEnrollments / uniqueEnrolledCourses, 2) : 0
            };

            return Ok(statistics);
        }
    }
}
