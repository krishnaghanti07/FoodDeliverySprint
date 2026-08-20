using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundRequestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "RefundRequests",
                newName: "RefundAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationCharge",
                table: "RefundRequests",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "RefundRequests",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFee",
                table: "RefundRequests",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "RefundRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationCharge",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "PlatformFee",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "RefundRequests");

            migrationBuilder.RenameColumn(
                name: "RefundAmount",
                table: "RefundRequests",
                newName: "Amount");
        }
    }
}
