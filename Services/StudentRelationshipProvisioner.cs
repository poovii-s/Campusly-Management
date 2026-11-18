using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;

namespace StudentManagementApi.Services;

public sealed class StudentRelationshipProvisioner(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager)
{
    public async Task AssignAsync(
        int studentId,
        string studentUserName,
        string trainerUserName)
    {
        if (studentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(studentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(studentUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(trainerUserName);

        var studentUser = await userManager.FindByNameAsync(studentUserName)
            ?? throw new InvalidOperationException("The specified Student account was not found.");
        var trainerUser = await userManager.FindByNameAsync(trainerUserName)
            ?? throw new InvalidOperationException("The specified Trainer account was not found.");

        if (!await userManager.IsInRoleAsync(studentUser, "Student"))
        {
            throw new InvalidOperationException("The specified account does not have the Student role.");
        }

        if (!await userManager.IsInRoleAsync(trainerUser, "Trainer"))
        {
            throw new InvalidOperationException("The specified account does not have the Trainer role.");
        }

        var student = await context.Students.SingleOrDefaultAsync(record => record.Id == studentId)
            ?? throw new InvalidOperationException("The specified student record was not found.");

        if (student.StudentUserId is not null && student.StudentUserId != studentUser.Id)
        {
            throw new InvalidOperationException(
                "The student record is already linked to a different Student account.");
        }

        var otherRecordIsLinked = await context.Students.AnyAsync(record =>
            record.Id != studentId && record.StudentUserId == studentUser.Id);
        if (otherRecordIsLinked)
        {
            throw new InvalidOperationException(
                "The Student account is already linked to another student record.");
        }

        student.StudentUserId = studentUser.Id;
        student.TrainerUserId = trainerUser.Id;
        await context.SaveChangesAsync();
    }
}
