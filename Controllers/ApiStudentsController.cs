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
    public class ApiStudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiStudentsController> _logger;

        public ApiStudentsController(ApplicationDbContext context, ILogger<ApiStudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all students
        /// </summary>
        /// <returns>List of all students</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
        {
            _logger.LogInformation("Getting all students");
            return await _context.Students.ToListAsync();
        }

        /// <summary>
        /// Get a specific student by ID
        /// </summary>
        /// <param name="id">Student ID</param>
        /// <returns>Student details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
            _logger.LogInformation($"Getting student with ID: {id}");
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                _logger.LogWarning($"Student with ID {id} not found");
                return NotFound(new { message = $"Student with ID {id} not found" });
            }

            return student;
        }

        /// <summary>
        /// Create a new student (Admin only)
        /// </summary>
        /// <param name="student">Student data</param>
        /// <returns>Created student</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Student>> CreateStudent([FromBody] Student student)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Creating new student: {student.FullName}");
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
        }

        /// <summary>
        /// Update an existing student (Admin only)
        /// </summary>
        /// <param name="id">Student ID</param>
        /// <param name="student">Updated student data</param>
        /// <returns>Updated student</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] Student student)
        {
            if (id != student.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Updating student with ID: {id}");
            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                {
                    return NotFound(new { message = $"Student with ID {id} not found" });
                }
                throw;
            }

            return Ok(new { message = "Student updated successfully", student });
        }

        /// <summary>
        /// Delete a student (Admin only)
        /// </summary>
        /// <param name="id">Student ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            _logger.LogInformation($"Deleting student with ID: {id}");
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound(new { message = $"Student with ID {id} not found" });
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student deleted successfully" });
        }

        /// <summary>
        /// Get a student's enrolled courses
        /// </summary>
        /// <param name="id">Student ID</param>
        /// <returns>List of courses the student is enrolled in</returns>
        [HttpGet("{id}/courses")]
        public async Task<ActionResult<IEnumerable<Course>>> GetStudentCourses(int id)
        {
            _logger.LogInformation($"Getting courses for student with ID: {id}");
            
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound(new { message = $"Student with ID {id} not found" });
            }

            var courses = await _context.StudentCourses
                .Where(sc => sc.StudentId == id)
                .Include(sc => sc.Course)
                .Select(sc => sc.Course)
                .ToListAsync();

            return Ok(courses);
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
