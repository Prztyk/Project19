using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeOcr.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOcrPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DetectedFormat = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StoredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FullText = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessingTimeMs = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OcrLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageOcrRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcrLines_Images_ImageOcrRecordId",
                        column: x => x.ImageOcrRecordId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_StoredFileName",
                table: "Images",
                column: "StoredFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OcrLines_ImageOcrRecordId_SequenceNumber",
                table: "OcrLines",
                columns: new[] { "ImageOcrRecordId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OcrLines");

            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
