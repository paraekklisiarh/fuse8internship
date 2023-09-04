using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalApi.Infrastructure.Data.ConfigurationContext.Migrations
{
    /// <inheritdoc />
    public partial class Add_Configuration_In_Db : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cur");

            migrationBuilder.CreateTable(
                name: "configuration_entities",
                schema: "cur",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_entities", x => x.key);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuration_entities",
                schema: "cur");
        }
    }
}
