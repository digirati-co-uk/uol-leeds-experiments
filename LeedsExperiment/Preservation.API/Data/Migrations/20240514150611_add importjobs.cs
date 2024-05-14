using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Preservation.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class addimportjobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_jobs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    original_import_job_id = table.Column<string>(type: "text", nullable: false),
                    deposit = table.Column<string>(type: "text", nullable: false),
                    digital_object = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    date_submitted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    date_begun = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_finished = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    new_version = table.Column<string>(type: "text", nullable: true),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    containers_added = table.Column<string>(type: "jsonb", nullable: true),
                    binaries_added = table.Column<string>(type: "jsonb", nullable: true),
                    containers_deleted = table.Column<string>(type: "jsonb", nullable: true),
                    binaries_deleted = table.Column<string>(type: "jsonb", nullable: true),
                    binaries_patched = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_jobs", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_jobs");
        }
    }
}
