
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using StudentManagementApi.Data;
using StudentManagementApi.Options;
using StudentManagementApi.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

const string provisionIdentityRoleCommand = "--provision-identity-role";
const string assignStudentRelationshipsCommand = "--assign-student-relationships";
var provisionIdentityRole = args.Contains(provisionIdentityRoleCommand, StringComparer.Ordinal);
var assignStudentRelationships = args.Contains(assignStudentRelationshipsCommand, StringComparer.Ordinal);
var builderArguments = args
    .Where(argument =>
        !string.Equals(argument, provisionIdentityRoleCommand, StringComparison.Ordinal) &&
        !string.Equals(argument, assignStudentRelationshipsCommand, StringComparison.Ordinal))
    .ToArray();

var builder = WebApplication.CreateBuilder(builderArguments);

// Add services to the container
builder.Services.AddControllers();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter the JWT access token. Swagger UI sends it as an Authorization: Bearer header.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.OperationFilter<AuthorizeOperationFilter>();
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("https://studentmanagementfrontend-liart.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Configure SQL Server Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.HasValidSigningKey,
        "Jwt:SigningKey must be a Base64-encoded key containing at least 32 bytes.")
    .ValidateOnStart();

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IdentityRoleProvisioner>();
builder.Services.AddScoped<StudentRelationshipProvisioner>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
    {
        var optionsValue = jwtOptionsAccessor.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = optionsValue.Issuer,
            ValidateAudience = true,
            ValidAudience = optionsValue.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(optionsValue.SigningKey)),
            ValidAlgorithms = [JwtOptions.SigningAlgorithm],
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddProblemDetails();

var app = builder.Build();

if (provisionIdentityRole && assignStudentRelationships)
{
    throw new InvalidOperationException("Run only one identity provisioning command at a time.");
}

if (provisionIdentityRole)
{
    var userName = app.Configuration["IdentityProvisioning:UserName"];
    var role = app.Configuration["IdentityProvisioning:Role"];
    if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(role))
    {
        throw new InvalidOperationException(
            "Provisioning requires IdentityProvisioning:UserName and IdentityProvisioning:Role.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var provisioner = scope.ServiceProvider.GetRequiredService<IdentityRoleProvisioner>();
    await provisioner.ProvisionAsync(userName, role);
    Console.WriteLine("Identity roles provisioned and the selected role assigned.");
    await app.DisposeAsync();
    return;
}

if (assignStudentRelationships)
{
    var studentIdValue = app.Configuration["StudentAssignment:StudentId"];
    var studentUserName = app.Configuration["StudentAssignment:StudentUserName"];
    var trainerUserName = app.Configuration["StudentAssignment:TrainerUserName"];
    if (!int.TryParse(studentIdValue, out var studentId) ||
        string.IsNullOrWhiteSpace(studentUserName) ||
        string.IsNullOrWhiteSpace(trainerUserName))
    {
        throw new InvalidOperationException(
            "Assignment requires StudentAssignment:StudentId, StudentAssignment:StudentUserName, and StudentAssignment:TrainerUserName.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var provisioner = scope.ServiceProvider.GetRequiredService<StudentRelationshipProvisioner>();
    await provisioner.AssignAsync(studentId, studentUserName, trainerUserName);
    Console.WriteLine("Student account and Trainer assignment completed.");
    await app.DisposeAsync();
    return;
}

app.UseExceptionHandler();

// Configure Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresAuthorization =
            context.MethodInfo.IsDefined(typeof(AuthorizeAttribute), inherit: true) ||
            context.MethodInfo.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), inherit: true) == true;

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
    }
}
