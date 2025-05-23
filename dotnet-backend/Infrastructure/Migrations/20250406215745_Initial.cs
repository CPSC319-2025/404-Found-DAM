﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectID);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMetadataFields",
                columns: table => new
                {
                    FieldID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMetadataFields", x => x.FieldID);
                    table.ForeignKey(
                        name: "FK_ProjectMetadataFields_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTags",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    TagID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTags", x => new { x.ProjectID, x.TagID });
                    table.ForeignKey(
                        name: "FK_ProjectTags_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTags_Tags_TagID",
                        column: x => x.TagID,
                        principalTable: "Tags",
                        principalColumn: "TagID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    BlobID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeInKB = table.Column<double>(type: "float", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    assetState = table.Column<int>(type: "int", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.BlobID);
                    table.ForeignKey(
                        name: "FK_Assets_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID");
                    table.ForeignKey(
                        name: "FK_Assets_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    ChangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    AssetID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    IsAdminAction = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.ChangeID);
                    table.ForeignKey(
                        name: "FK_Logs_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMemberships",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    UserRole = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberships", x => new { x.ProjectID, x.UserID });
                    table.ForeignKey(
                        name: "FK_ProjectMemberships_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMemberships_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetMetadata",
                columns: table => new
                {
                    BlobID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FieldID = table.Column<int>(type: "int", nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMetadata", x => new { x.BlobID, x.FieldID });
                    table.ForeignKey(
                        name: "FK_AssetMetadata_Assets_BlobID",
                        column: x => x.BlobID,
                        principalTable: "Assets",
                        principalColumn: "BlobID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetMetadata_ProjectMetadataFields_FieldID",
                        column: x => x.FieldID,
                        principalTable: "ProjectMetadataFields",
                        principalColumn: "FieldID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetTags",
                columns: table => new
                {
                    BlobID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TagID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTags", x => new { x.BlobID, x.TagID });
                    table.ForeignKey(
                        name: "FK_AssetTags_Assets_BlobID",
                        column: x => x.BlobID,
                        principalTable: "Assets",
                        principalColumn: "BlobID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetTags_Tags_TagID",
                        column: x => x.TagID,
                        principalTable: "Tags",
                        principalColumn: "TagID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "Email", "IsSuperAdmin", "LastUpdated", "Name", "PasswordHash" },
                values: new object[] { 1, "userTest@example.com", true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "userTest", "AQAAAAIAAYagAAAAEJUsEQ+L7D9znNxLO6g9+Hr7QMereAnXK/SHFLUTqkpH67y8L7R4Uj78pVmY3/hyjg==" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMetadata_FieldID",
                table: "AssetMetadata",
                column: "FieldID");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ProjectID",
                table: "Assets",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UserID",
                table: "Assets",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTags_BlobID_TagID",
                table: "AssetTags",
                columns: new[] { "BlobID", "TagID" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetTags_TagID",
                table: "AssetTags",
                column: "TagID");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_UserID",
                table: "Logs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberships_UserID",
                table: "ProjectMemberships",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMetadataFields_ProjectID",
                table: "ProjectMetadataFields",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTags_ProjectID_TagID",
                table: "ProjectTags",
                columns: new[] { "ProjectID", "TagID" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTags_TagID",
                table: "ProjectTags",
                column: "TagID");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMetadata");

            migrationBuilder.DropTable(
                name: "AssetTags");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "ProjectMemberships");

            migrationBuilder.DropTable(
                name: "ProjectTags");

            migrationBuilder.DropTable(
                name: "ProjectMetadataFields");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
