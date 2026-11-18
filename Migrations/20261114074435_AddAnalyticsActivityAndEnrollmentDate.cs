using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsActivityAndEnrollmentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EnrollmentDate",
                table: "Students",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserLoginActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActivityDateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginActivities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginActivities_ActivityDateUtc_UserId",
                table: "UserLoginActivities",
                columns: new[] { "ActivityDateUtc", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginActivities_UserId",
                table: "UserLoginActivities",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLoginActivities");

            migrationBuilder.DropColumn(
                name: "EnrollmentDate",
                table: "Students");
        }
    }
}
