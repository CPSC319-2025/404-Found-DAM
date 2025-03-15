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
        private const int _projectStartRow = 1;
        private const int _assetStartRow = 1;
        private const int _startColumn = 1;


        /*
            Each worksheet in a workbook contains details of a given project, inncluding:
                - project info starting from (1, 1)
                - info about each asset starting from (5, 1)
        */
        public static (string, byte[]) GenerateProjectExportExcel(Project project, List<Asset> assets)
        {
            //Sheet 1: project detail including flattened metadata

            //TODO: add admins and regular users if time permits
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

            using var workbook = new XLWorkbook(); // Create an Excel workbook instance
            DateTime exportedAt = DateTime.Now;
            string exportedYMD = exportedAt.ToString("yyyyMMdd");
            string fileName = $"Project_{project.ProjectID}_export_{exportedYMD}.xlsx";

            // Add worksheet
            var wsProject = workbook.AddWorksheet($"Project {project.ProjectID}");
            // Set headers; .Cell(row, column)
            // wsProject.Cell(2, 2).Value = "Dessert Name Proj"; 
            // wsProject.Cell(2, 3).Value = "Sales Proj";

            // Insert projectdDataTable
            wsProject.Cell(_projectStartRow, _startColumn).InsertTable(projectDataTable.AsEnumerable());


            // Insert asset data
            var assetDataTable = new DataTable();
            assetDataTable.Columns.Add("File Name", typeof(string));
            assetDataTable.Columns.Add("MimeType", typeof(string));
            assetDataTable.Columns.Add("File Size (KB)", typeof(double));
            assetDataTable.Columns.Add("LastUpdated", typeof(string));
            assetDataTable.Columns.Add("Tags", typeof(string));
            // TODO: Add custom metadata fields if needed

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
                    a.FileName,
                    a.MimeType,
                    a.FileSizeInKB,
                    a.LastUpdated.ToString("yyyyMMdd"),
                    assetTagNameStr
                );
                // Insert assetDataTable
                wsProject.Cell(_assetStartRow + i, _startColumn).InsertTable(assetDataTable.AsEnumerable());
            } 
  

            // Adjust spacing
            wsProject.Columns().AdjustToContents();
            wsProject.Rows().AdjustToContents();

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