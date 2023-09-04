using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalApi.Infrastructure.Data.CurrencyContext.Migrations
{
    /// <inheritdoc />
    public partial class CurrencyDate_to_DateTimeOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "rate_date",
                schema: "cur",
                table: "currencies",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "rate_date",
                schema: "cur",
                table: "currencies",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
