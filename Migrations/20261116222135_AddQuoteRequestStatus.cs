
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HunterGrassServices.Api.Migrations
{
    /// <summary>
    /// Adds persistent status tracking to customer
    /// quote requests.
    ///
    /// Existing quote requests receive the
    /// default status "New".
    /// </summary>
    public partial class AddQuoteRequestStatus : Migration
    {
        // =========================================
        // APPLY DATABASE CHANGES
        // =========================================

        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            // Add the Status column to the
            // existing QuoteRequests table.
            //
            // Existing customer records are
            // preserved and receive "New".

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "QuoteRequests",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "New");
        }

        // =========================================
        // REVERSE DATABASE CHANGES
        // =========================================

        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            // Remove only the Status column
            // if this migration is rolled back.

            migrationBuilder.DropColumn(
                name: "Status",
                table: "QuoteRequests");
        }
    }
}
