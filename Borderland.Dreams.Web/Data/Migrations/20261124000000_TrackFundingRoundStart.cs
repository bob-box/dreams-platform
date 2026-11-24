using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Web.Data.Migrations
{
    public partial class TrackFundingRoundStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "FundingRound",
                type: "TEXT",
                nullable: true);

            // Previous versions only recorded a successful start when money was allocated.
            migrationBuilder.Sql("""
                UPDATE FundingRound
                SET StartedAtUtc = StartDateUtc
                WHERE CAST(TotalFundsAllocated AS NUMERIC) > 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "StartedAtUtc", table: "FundingRound");
        }
    }
}
