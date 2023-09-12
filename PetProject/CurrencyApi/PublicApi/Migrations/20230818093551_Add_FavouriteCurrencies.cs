using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class Add_FavouriteCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user");

            migrationBuilder.CreateTable(
                name: "favourite_currencies",
                schema: "user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    base_currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favourite_currencies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_favourite_currencies_base_currency_currency",
                schema: "user",
                table: "favourite_currencies",
                columns: new[] { "base_currency", "currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_favourite_currencies_name",
                schema: "user",
                table: "favourite_currencies",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favourite_currencies",
                schema: "user");
        }
    }
}
