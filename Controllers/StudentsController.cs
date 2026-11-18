using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.Models;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(ApplicationDbContext context) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetAll(
        [FromQuery] string? courseName,
        CancellationToken cancellationToken)
    {
        IQueryable<Student> query = context.Students.AsNoTracking();

        if (!User.IsInRole("Admin"))
        {
            var trainerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (trainerUserId is null)
            {
                return Forbid();
            }

            query = query.Where(student => student.TrainerUserId == trainerUserId);
        }

        if (!string.IsNullOrWhiteSpace(courseName))
        {
            query = query.Where(student => student.CourseName == courseName);
        }

        var students = await query
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    [Authorize(Roles = "Admin,Trainer,Student")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Student>> GetById(int id, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(student => student.Id == id, cancellationToken);

        if (student is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAssignedTrainer = User.IsInRole("Trainer") &&
                userId is not null &&
                student.TrainerUserId == userId;
            var isOwningStudent = User.IsInRole("Student") &&
                userId is not null &&
                student.StudentUserId == userId;

            if (!isAssignedTrainer && !isOwningStudent)
            {
                return Forbid();
            }
        }

        return Ok(student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Student>> Create(Student student, CancellationToken cancellationToken)
    {
        student.Id = 0;
        student.EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        context.Students.Add(student);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [Authorize(Roles = "Admin,Trainer,Student")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Student student, CancellationToken cancellationToken)
    {
        if (id != student.Id)
        {
            return BadRequest();
        }

        var existingStudent = await context.Students.FindAsync([id], cancellationToken);
        if (existingStudent is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Admin"))
        {
            existingStudent.FirstName = student.FirstName;
            existingStudent.LastName = student.LastName;
            existingStudent.Email = student.Email;
            existingStudent.CourseName = student.CourseName;
            existingStudent.DateOfBirth = student.DateOfBirth;
        }
        else
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Trainer") && userId is not null &&
                existingStudent.TrainerUserId == userId)
            {
                existingStudent.CourseName = student.CourseName;
            }
            else if (User.IsInRole("Student") && userId is not null &&
                existingStudent.StudentUserId == userId)
            {
                existingStudent.FirstName = student.FirstName;
                existingStudent.LastName = student.LastName;
                existingStudent.Email = student.Email;
                existingStudent.DateOfBirth = student.DateOfBirth;
            }
            else
            {
                return Forbid();
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var student = await context.Students.FindAsync([id], cancellationToken);
        if (student is null)
        {
            return NotFound();
        }

        context.Students.Remove(student);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
