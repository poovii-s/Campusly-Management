using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentOwnershipAndTrainerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentUserId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainerUserId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentUserId",
                table: "Students",
                column: "StudentUserId",
                unique: true,
                filter: "[StudentUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Students_TrainerUserId",
                table: "Students",
                column: "TrainerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_StudentUserId",
                table: "Students",
                column: "StudentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_TrainerUserId",
                table: "Students",
                column: "TrainerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_StudentUserId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_TrainerUserId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_StudentUserId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_TrainerUserId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentUserId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TrainerUserId",
                table: "Students");
        }
    }
}
