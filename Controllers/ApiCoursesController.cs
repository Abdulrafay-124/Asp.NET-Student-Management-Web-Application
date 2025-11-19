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
    public class ApiCoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiCoursesController> _logger;

        public ApiCoursesController(ApplicationDbContext context, ILogger<ApiCoursesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all courses
        /// </summary>
        /// <returns>List of all courses</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            _logger.LogInformation("Getting all courses");
            return await _context.Courses.ToListAsync();
        }

        /// <summary>
        /// Get a specific course by ID
        /// </summary>
        /// <param name="id">Course ID</param>
        /// <returns>Course details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            _logger.LogInformation($"Getting course with ID: {id}");
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                _logger.LogWarning($"Course with ID {id} not found");
                return NotFound(new { message = $"Course with ID {id} not found" });
            }

            return course;
        }

        /// <summary>
        /// Create a new course (Admin only)
        /// </summary>
        /// <param name="course">Course data</param>
        /// <returns>Created course</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Course>> CreateCourse([FromBody] Course course)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Creating new course: {course.Title}");
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }

        /// <summary>
        /// Update an existing course (Admin only)
        /// </summary>
        /// <param name="id">Course ID</param>
        /// <param name="course">Updated course data</param>
        /// <returns>Updated course</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] Course course)
        {
            if (id != course.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Updating course with ID: {id}");
            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound(new { message = $"Course with ID {id} not found" });
                }
                throw;
            }

            return Ok(new { message = "Course updated successfully", course });
        }

        /// <summary>
        /// Delete a course (Admin only)
        /// </summary>
        /// <param name="id">Course ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            _logger.LogInformation($"Deleting course with ID: {id}");
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound(new { message = $"Course with ID {id} not found" });
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course deleted successfully" });
        }

        /// <summary>
        /// Get all students enrolled in a course
        /// </summary>
        /// <param name="id">Course ID</param>
        /// <returns>List of students enrolled in the course</returns>
        [HttpGet("{id}/students")]
        public async Task<ActionResult<IEnumerable<Student>>> GetCourseStudents(int id)
        {
            _logger.LogInformation($"Getting students enrolled in course with ID: {id}");
            
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound(new { message = $"Course with ID {id} not found" });
            }

            var students = await _context.StudentCourses
                .Where(sc => sc.CourseId == id)
                .Include(sc => sc.Student)
                .Select(sc => sc.Student)
                .ToListAsync();

            return Ok(students);
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
