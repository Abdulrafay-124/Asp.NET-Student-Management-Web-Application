using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Data;
using StudentManagement.Models;

namespace StudentManagement.Controllers
{
    //[Authorize] // TEMPORARILY DISABLED FOR TESTING
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Enrollments - Show all enrollments
        public async Task<IActionResult> Index()
        {
            var enrollments = await _context.StudentCourses
                .Include(sc => sc.Student)
                .Include(sc => sc.Course)
                .ToListAsync();
            
            return View(enrollments);
        }

        // GET: Enrollments/EnrollStudent - Form to enroll a student
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EnrollStudent()
        {
            ViewBag.StudentId = new SelectList(
                await _context.Students.ToListAsync(), 
                "Id", 
                "FullName"
            );
            ViewBag.CourseId = new SelectList(
                await _context.Courses.ToListAsync(), 
                "Id", 
                "Title"
            );
            return View();
        }

        // POST: Enrollments/EnrollStudent
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent([Bind("StudentId,CourseId")] StudentCourse studentCourse)
        {
            // Check if student exists
            var studentExists = await _context.Students.AnyAsync(s => s.Id == studentCourse.StudentId);
            if (!studentExists)
            {
                ModelState.AddModelError("StudentId", "Selected student does not exist.");
            }

            // Check if course exists
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == studentCourse.CourseId);
            if (!courseExists)
            {
                ModelState.AddModelError("CourseId", "Selected course does not exist.");
            }

            // Check if already enrolled
            var alreadyEnrolled = await _context.StudentCourses
                .AnyAsync(sc => sc.StudentId == studentCourse.StudentId && sc.CourseId == studentCourse.CourseId);
            if (alreadyEnrolled)
            {
                ModelState.AddModelError("", "This student is already enrolled in this course.");
            }

            if (ModelState.IsValid)
            {
                studentCourse.EnrolledOn = DateTime.UtcNow;
                _context.Add(studentCourse);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.StudentId = new SelectList(
                await _context.Students.ToListAsync(),
                "Id",
                "FullName",
                studentCourse.StudentId
            );
            ViewBag.CourseId = new SelectList(
                await _context.Courses.ToListAsync(),
                "Id",
                "Title",
                studentCourse.CourseId
            );
            return View(studentCourse);
        }

        // GET: Enrollments/StudentCourses/5 - Show all courses for a student
        public async Task<IActionResult> StudentCourses(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var enrollments = await _context.StudentCourses
                .Where(sc => sc.StudentId == id)
                .Include(sc => sc.Course)
                .ToListAsync();

            ViewBag.Student = student;
            return View(enrollments);
        }

        // GET: Enrollments/CoursesForStudent - Quick view of enrollments
        public async Task<IActionResult> CoursesForStudent()
        {
            var enrollments = await _context.StudentCourses
                .Include(sc => sc.Student)
                .Include(sc => sc.Course)
                .ToListAsync();

            var groupedEnrollments = enrollments
                .Where(sc => sc.Student != null)
                .GroupBy(sc => sc.Student!)
                .Select(g => new StudentEnrollmentViewModel
                {
                    Student = g.Key,
                    Courses = g.Select(sc => sc.Course).Where(c => c != null).Cast<Course>().ToList()
                })
                .ToList();

            return View(groupedEnrollments);
        }

        // GET: Enrollments/Delete/5/5 - Remove enrollment
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? studentId, int? courseId)
        {
            if (studentId == null || courseId == null)
            {
                return NotFound();
            }

            var studentCourse = await _context.StudentCourses
                .Include(sc => sc.Student)
                .Include(sc => sc.Course)
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (studentCourse == null)
            {
                return NotFound();
            }

            return View(studentCourse);
        }

        // POST: Enrollments/Delete/5/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int studentId, int courseId)
        {
            var studentCourse = await _context.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

            if (studentCourse != null)
            {
                _context.StudentCourses.Remove(studentCourse);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
