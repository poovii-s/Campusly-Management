using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Models;

namespace StudentManagementApi.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Student> Students => Set<Student>();

    public DbSet<UserLoginActivity> UserLoginActivities => Set<UserLoginActivity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL")
            .HasDatabaseName("EmailIndex");

        builder.Entity<Student>()
            .HasOne(student => student.StudentUser)
            .WithOne()
            .HasForeignKey<Student>(student => student.StudentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Student>()
            .HasIndex(student => student.StudentUserId)
            .IsUnique()
            .HasFilter("[StudentUserId] IS NOT NULL");

        builder.Entity<Student>()
            .HasOne(student => student.TrainerUser)
            .WithMany()
            .HasForeignKey(student => student.TrainerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Student>()
            .HasIndex(student => student.TrainerUserId);

        builder.Entity<UserLoginActivity>()
            .HasOne(activity => activity.User)
            .WithMany()
            .HasForeignKey(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserLoginActivity>()
            .HasIndex(activity => new { activity.ActivityDateUtc, activity.UserId });
    }
}
