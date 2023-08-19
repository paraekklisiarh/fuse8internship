using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class FavoriteCurrency_and_ApiSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currency_api_settings",
                schema: "user",
                columns: table => new
                {
                    id = table.Column<byte>(type: "smallint", nullable: false),
                    default_currency = table.Column<string>(type: "text", nullable: true),
                    currency_round_count = table.Column<int>(type: "integer", maxLength: 27, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currency_api_settings", x => x.id);
                    table.CheckConstraint("CK_currency_api_settings_default_currency_RegularExpression", "default_currency ~ '[A-Z]{3}'");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_favourite_currencies_base_currency_RegularExpression",
                schema: "user",
                table: "favourite_currencies",
                sql: "base_currency ~ '[A-Z]{3}'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_favourite_currencies_currency_RegularExpression",
                schema: "user",
                table: "favourite_currencies",
                sql: "currency ~ '[A-Z]{3}'");

            migrationBuilder.CreateIndex(
                name: "ix_currency_api_settings_default_currency_currency_round_count",
                schema: "user",
                table: "currency_api_settings",
                columns: new[] { "default_currency", "currency_round_count" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "currency_api_settings",
                schema: "user");

            migrationBuilder.DropCheckConstraint(
                name: "CK_favourite_currencies_base_currency_RegularExpression",
                schema: "user",
                table: "favourite_currencies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_favourite_currencies_currency_RegularExpression",
                schema: "user",
                table: "favourite_currencies");
        }
    }
}
