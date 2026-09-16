using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FnBManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRequestDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestKey",
                table: "Orders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RequestKey",
                table: "Orders",
                column: "RequestKey",
                unique: true,
                filter: "[RequestKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_RequestKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequestKey",
                table: "Orders");
        }
    }
}
