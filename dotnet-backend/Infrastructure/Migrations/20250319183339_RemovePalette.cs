using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePalette : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetadataFields_Palettes_PaletteID",
                table: "MetadataFields");

            migrationBuilder.DropTable(
                name: "Palettes");

            migrationBuilder.DropIndex(
                name: "IX_MetadataFields_PaletteID",
                table: "MetadataFields");

            migrationBuilder.DropColumn(
                name: "PaletteID",
                table: "MetadataFields");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaletteID",
                table: "MetadataFields",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Palettes",
                columns: table => new
                {
                    PaletteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Palettes", x => x.PaletteID);
                    table.ForeignKey(
                        name: "FK_Palettes_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataFields_PaletteID",
                table: "MetadataFields",
                column: "PaletteID");

            migrationBuilder.CreateIndex(
                name: "IX_Palettes_ProjectID",
                table: "Palettes",
                column: "ProjectID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataFields_Palettes_PaletteID",
                table: "MetadataFields",
                column: "PaletteID",
                principalTable: "Palettes",
                principalColumn: "PaletteID");
        }
    }
}
