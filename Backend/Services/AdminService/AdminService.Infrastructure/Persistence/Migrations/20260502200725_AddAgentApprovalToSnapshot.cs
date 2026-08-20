using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentApprovalToSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNotes",
                table: "DeliveryAgentSnapshots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "DeliveryAgentSnapshots",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "DeliveryAgentSnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "DeliveryAgentSnapshots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAgentSnapshots_IsApproved",
                table: "DeliveryAgentSnapshots",
                column: "IsApproved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeliveryAgentSnapshots_IsApproved",
                table: "DeliveryAgentSnapshots");

            migrationBuilder.DropColumn(
                name: "ApprovalNotes",
                table: "DeliveryAgentSnapshots");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "DeliveryAgentSnapshots");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "DeliveryAgentSnapshots");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "DeliveryAgentSnapshots");
        }
    }
}
