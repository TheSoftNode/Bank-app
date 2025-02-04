using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data_service.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessDateToDEbitQue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedDate",
                table: "DirectDebitQueues",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedDate",
                table: "DirectDebitQueues");
        }
    }
}
