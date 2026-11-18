namespace StudentManagementApi.Contracts;

/// <summary>Aggregated student and user activity for the selected date range.</summary>
public sealed record AdminAnalyticsResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalUsers,
    int TotalStudents,
    int AdminCount,
    int TrainerCount,
    int StudentCount,
    int ActiveUsers,
    int TotalCourses,
    IReadOnlyList<RoleDistributionResponse> RoleDistribution,
    IReadOnlyList<EnrollmentMonthResponse> MonthlyEnrollments,
    IReadOnlyList<LoginDayResponse> DailyLogins,
    IReadOnlyList<CourseDistributionResponse> CourseDistribution,
    IReadOnlyList<RecentLoginResponse> RecentLogins,
    IReadOnlyList<RecentStudentEnrollmentResponse> RecentEnrollments);

/// <summary>The number of registered users assigned to a role.</summary>
public sealed record RoleDistributionResponse(string Role, int Count);

/// <summary>The number of student records created in a calendar month.</summary>
public sealed record EnrollmentMonthResponse(string Month, int Count);

/// <summary>The number of successful logins recorded on a UTC calendar day.</summary>
public sealed record LoginDayResponse(DateOnly Date, int Count);

/// <summary>The number of student records associated with a course name.</summary>
public sealed record CourseDistributionResponse(string CourseName, int Count);

/// <summary>A recent successful login and the user's current assigned role.</summary>
public sealed record RecentLoginResponse(
    string UserName,
    string Email,
    string Role,
    DateTimeOffset OccurredAtUtc);

/// <summary>A recent student record with a known creation date.</summary>
public sealed record RecentStudentEnrollmentResponse(
    string StudentName,
    string Email,
    string? CourseName,
    DateOnly EnrollmentDate);
