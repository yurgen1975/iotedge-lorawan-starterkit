using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace restAPI.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceMapPoints",
                columns: table => new
                {
                    eui = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id = table.Column<uint>(nullable: false),
                    Longitude = table.Column<double>(nullable: false),
                    Latitude = table.Column<double>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceMapPoints", x => x.eui);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceMapPoints");
        }
    }
}
