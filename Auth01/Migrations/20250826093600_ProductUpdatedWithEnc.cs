using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth01.Migrations
{
    /// <inheritdoc />
    public partial class ProductUpdatedWithEnc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedDescription",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedDescription",
                table: "Products");
        }
    }
}
