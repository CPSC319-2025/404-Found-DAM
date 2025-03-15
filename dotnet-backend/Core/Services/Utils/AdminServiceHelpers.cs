using System;
using System.IO;
using System.IO.Compression;
using ClosedXML.Excel;
using System.Data; // For using DataTable
using Core.Entities;
using ZstdSharp.Unsafe;

namespace Core.Services.Utils
{
    public static class AdminServiceHelpers 
    {  
        private const int _startRow = 1;
        private const int _startColumn = 1;


        /*
            Each worksheet in a workbook contains details of a given project, inncluding:
                - project info starting from (1, 1)
                - info about each asset starting from (5, 1)
        */
        public static (string, byte[]) GenerateProjectExportExcel(Project project, List<Asset> assets)
        {

            using var workbook = new XLWorkbook(); // Create an Excel workbook instance
            DateTime exportedAt = DateTime.Now;
            string exportedYMD = exportedAt.ToString("yyyyMMdd");
            string fileName = $"Project_{project.ProjectID}_export_{exportedYMD}.xlsx";

            //Sheet 1: project detail including flattened metadata
            // Add Columns for project details
            var projectDataTable = new DataTable();
            projectDataTable.Columns.Add("Project ID", typeof(int));
            projectDataTable.Columns.Add("Name", typeof(string));
            projectDataTable.Columns.Add("Version", typeof(string));
            projectDataTable.Columns.Add("Location", typeof(string));
            projectDataTable.Columns.Add("Description", typeof(string));
            projectDataTable.Columns.Add("Creation Time", typeof(string));
            projectDataTable.Columns.Add("Active", typeof(string));
            projectDataTable.Columns.Add("Archived At", typeof(string));
            projectDataTable.Columns.Add("Tags", typeof(string));
            // Add custom metadata fields if needed

            string active = project.Active ? "Yes" : "No";
            string archivedTime = project.ArchivedAt == null ? "N/A" : project.ArchivedAt.Value.ToString("yyyyMMdd");
            string projectTagNameStr = ""; 

            if (project.ProjectTags != null) {
            // Collect each tag's name
                foreach (var pt in project.ProjectTags)
                {
                    if (pt.Tag != null) {
                        projectTagNameStr = projectTagNameStr == ""
                            ? pt.Tag.Name
                            : projectTagNameStr + ", " + pt.Tag.Name;
                    }
                }
            }

            // Insert project details
            projectDataTable.Rows.Add
            (
                project.ProjectID,
                project.Name,
                project.Version,
                project.Location,
                project.Description,
                project.CreationTime.ToString("yyyyMMdd"),
                active,
                archivedTime,
                projectTagNameStr
            );

            // Add Columns for asset details
            var assetDataTable = new DataTable();
            assetDataTable.Columns.Add("Blob ID", typeof(int));
            assetDataTable.Columns.Add("File Name", typeof(string));
            assetDataTable.Columns.Add("Mime Type", typeof(string));
            assetDataTable.Columns.Add("File Size (KB)", typeof(double));
            assetDataTable.Columns.Add("LastUpdated", typeof(string));
            assetDataTable.Columns.Add("Tags", typeof(string));
            // TODO: Add custom metadata fields if needed

            // Insert asset details
            for (int i = 0; i < assets.Count; i++)
            {
                Asset a = assets[i];
                string assetTagNameStr = ""; 

                if (a.AssetTags != null) {
                // Collect each tag's name
                    foreach (var at in a.AssetTags)
                    {
                        if (at.Tag != null) {
                            assetTagNameStr = assetTagNameStr == ""
                                ? at.Tag.Name
                                : assetTagNameStr + ", " + at.Tag.Name;
                        }
                    }
                }

                assetDataTable.Rows.Add
                (
                    a.BlobID,
                    a.FileName,
                    a.MimeType,
                    a.FileSizeInKB,
                    a.LastUpdated.ToString("yyyyMMdd"),
                    assetTagNameStr
                );
            } 
  
            // Create & add another worksheet/tab for including project's members
            List<User> adminList = new List<User>();
            List<User> regularUserList = new List<User>();
            foreach (ProjectMembership pm in project.ProjectMemberships)
            {
                (pm.UserRole == ProjectMembership.UserRoleType.Admin 
                    ? adminList 
                    : regularUserList).Add(pm.User);
            }


            // Add Columns for user details
            var memberDataTable = new DataTable();
            memberDataTable.Columns.Add("User ID", typeof(int));
            memberDataTable.Columns.Add("Name", typeof(string));
            memberDataTable.Columns.Add("Email", typeof(string));
            memberDataTable.Columns.Add("IsSuperAdmin", typeof(bool));
            memberDataTable.Columns.Add("LastUpdated", typeof(string));
            memberDataTable.Columns.Add("Role", typeof(string));

            // Insert admins
            for (int i = 0; i < adminList.Count; i++)
            {
                User m = adminList[i];
                memberDataTable.Rows.Add
                (
                    m.UserID,
                    m.Name,
                    m.Email,
                    m.IsSuperAdmin,
                    m.LastUpdated.ToString("yyyyMMdd"),
                    "admin"
                );
            } 

           // Insert regular users
            for (int i = 0; i < regularUserList.Count; i++)
            {
                User m = regularUserList[i];
                memberDataTable.Rows.Add
                (
                    m.UserID,
                    m.Name,
                    m.Email,
                    m.IsSuperAdmin,
                    m.LastUpdated.ToString("yyyyMMdd"),
                    "users"
                );
            } 


            // Add worksheets
            var wsProject = workbook.AddWorksheet($"Project {project.ProjectID}");
            // Set headers; .Cell(row, column)
            // wsProject.Cell(2, 2).Value = "Dessert Name Proj"; 
            // wsProject.Cell(2, 3).Value = "Sales Proj";
            var wsMembers = workbook.AddWorksheet($"Project {project.ProjectID} Members");
            wsProject.Cell(_startRow, _startColumn).InsertTable(projectDataTable.AsEnumerable());
            wsProject.Cell(_startRow + 5, _startColumn).InsertTable(assetDataTable.AsEnumerable());
            wsMembers.Cell(_startRow, _startColumn).InsertTable(memberDataTable.AsEnumerable());

            // Adjust spacing
            wsProject.Columns().AdjustToContents();
            wsProject.Rows().AdjustToContents();
            wsMembers.Columns().AdjustToContents();
            wsMembers.Rows().AdjustToContents();

            // Save Excel workbook as xlsx file to the current folder (APIs)
            // workbook.SaveAs(fileName); 
            
            // Save Excel workbook into stream
            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream); 
                byte[] fileContent = stream.ToArray(); // Conver stream content to byte[]
                return (fileName, fileContent); // Return file name and binary data of the Excel file
                // return ProjectServiceHelpers.CompressByteArray(fileContent); // Compress fileContent and return
            }
        }

        // A method to compress byte array
        public static byte[] CompressByteArray(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    compressor.Write(data, 0, data.Length);
                    return compressedStream.ToArray();
                } // compressor.Dispose() is called here
            } // compressedStream.Dispose() is called here
        }
    }
}