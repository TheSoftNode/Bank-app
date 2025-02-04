using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data_service.Migrations
{
    /// <inheritdoc />
    public partial class addSingleProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerAccountId",
                table: "BatchChargeArchives");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerAccountId",
                table: "BatchChargeArchives",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
