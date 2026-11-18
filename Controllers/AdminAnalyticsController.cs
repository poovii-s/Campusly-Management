using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Contracts;
using StudentManagementApi.Data;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public sealed class AdminAnalyticsController(ApplicationDbContext context) : ControllerBase
{
    private static readonly string[] SupportedRoles = ["Admin", "Trainer", "Student"];

    [HttpGet]
    [ProducesResponseType(typeof(AdminAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminAnalyticsResponse>> Get(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        if (startDate.HasValue != endDate.HasValue)
        {
            return InvalidDateRange("Provide both startDate and endDate, or omit both.");
        }

        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeStart = startDate ?? utcToday.AddDays(-29);
        var rangeEnd = endDate ?? utcToday;
        if (rangeStart > rangeEnd)
        {
            return InvalidDateRange("startDate must be on or before endDate.");
        }

        var totalUsers = await context.Users.CountAsync(cancellationToken);
        var totalStudents = await context.Students.CountAsync(cancellationToken);
        var roleCounts = await (
            from userRole in context.UserRoles
            join role in context.Roles on userRole.RoleId equals role.Id
            where role.Name != null && SupportedRoles.Contains(role.Name)
            group userRole by role.Name into roleUsers
            select new { Role = roleUsers.Key, Count = roleUsers.Count() }
        ).ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);

        var courseNames = await context.Students
            .AsNoTracking()
            .Where(student => student.CourseName != null && student.CourseName.Trim() != "")
            .Select(student => student.CourseName!.Trim())
            .ToListAsync(cancellationToken);
        var courseDistribution = courseNames
            .GroupBy(courseName => courseName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CourseDistributionResponse(group.Key, group.Count()))
            .OrderByDescending(course => course.Count)
            .ThenBy(course => course.CourseName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var enrollmentRecords = await context.Students
            .AsNoTracking()
            .Where(student =>
                student.EnrollmentDate.HasValue &&
                student.EnrollmentDate.Value >= rangeStart &&
                student.EnrollmentDate.Value <= rangeEnd)
            .OrderByDescending(student => student.EnrollmentDate)
            .Select(student => new
            {
                student.FirstName,
                student.LastName,
                student.Email,
                student.CourseName,
                EnrollmentDate = student.EnrollmentDate!.Value
            })
            .ToListAsync(cancellationToken);
        var monthlyEnrollments = enrollmentRecords
            .GroupBy(record => new
            {
                record.EnrollmentDate.Year,
                record.EnrollmentDate.Month
            })
            .Select(group => new EnrollmentMonthResponse(
                group.Key.Year.ToString("D4", CultureInfo.InvariantCulture) + "-" +
                group.Key.Month.ToString("D2", CultureInfo.InvariantCulture),
                group.Count()))
            .OrderBy(month => month.Month, StringComparer.Ordinal)
            .ToArray();

        var loginActivities = await context.UserLoginActivities
            .AsNoTracking()
            .Where(activity =>
                activity.ActivityDateUtc >= rangeStart &&
                activity.ActivityDateUtc <= rangeEnd)
            .Select(activity => new
            {
                activity.UserId,
                activity.ActivityDateUtc,
                activity.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);
        var dailyLogins = loginActivities
            .GroupBy(activity => activity.ActivityDateUtc)
            .Select(group => new LoginDayResponse(group.Key, group.Count()))
            .OrderBy(day => day.Date)
            .ToArray();
        var activeUsers = loginActivities
            .Select(activity => activity.UserId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        var recentLoginRows = loginActivities
            .GroupBy(activity => activity.UserId, StringComparer.Ordinal)
            .Select(group => group.MaxBy(activity => activity.OccurredAtUtc)!)
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .Take(10)
            .ToArray();
        var recentUserIds = recentLoginRows
            .Select(activity => activity.UserId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var recentUsers = await context.Users
            .AsNoTracking()
            .Where(user => recentUserIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? ""
            })
            .ToDictionaryAsync(user => user.Id, cancellationToken);
        var recentRoles = new Dictionary<string, string>(StringComparer.Ordinal);
        if (recentUserIds.Length > 0)
        {
            var recentRoleRows = await (
                from userRole in context.UserRoles
                join role in context.Roles on userRole.RoleId equals role.Id
                where recentUserIds.Contains(userRole.UserId) &&
                    role.Name != null &&
                    SupportedRoles.Contains(role.Name)
                select new { userRole.UserId, Role = role.Name }
            ).ToListAsync(cancellationToken);
            recentRoles = recentRoleRows
                .GroupBy(item => item.UserId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(
                        ", ",
                        group.Select(item => item.Role)
                            .OrderBy(role => Array.IndexOf(SupportedRoles, role))),
                    StringComparer.Ordinal);
        }

        var recentLogins = recentLoginRows
            .Where(activity => recentUsers.ContainsKey(activity.UserId))
            .Select(activity =>
            {
                var user = recentUsers[activity.UserId];
                var role = recentRoles.TryGetValue(activity.UserId, out var assignedRole)
                    ? assignedRole
                    : "Unassigned";
                return new RecentLoginResponse(
                    user.UserName,
                    user.Email,
                    role,
                    activity.OccurredAtUtc);
            })
            .ToArray();
        var recentEnrollments = enrollmentRecords
            .Take(10)
            .Select(record => new RecentStudentEnrollmentResponse(
                $"{record.FirstName} {record.LastName}".Trim(),
                record.Email,
                record.CourseName,
                record.EnrollmentDate))
            .ToArray();

        return Ok(new AdminAnalyticsResponse(
            rangeStart,
            rangeEnd,
            totalUsers,
            totalStudents,
            roleCounts.GetValueOrDefault("Admin"),
            roleCounts.GetValueOrDefault("Trainer"),
            roleCounts.GetValueOrDefault("Student"),
            activeUsers,
            courseDistribution.Length,
            SupportedRoles
                .Select(role => new RoleDistributionResponse(role, roleCounts.GetValueOrDefault(role)))
                .ToArray(),
            monthlyEnrollments,
            dailyLogins,
            courseDistribution,
            recentLogins,
            recentEnrollments));
    }

    private BadRequestObjectResult InvalidDateRange(string detail) =>
        BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid analytics date range",
            Detail = detail,
            Instance = Request.Path
        });
}
