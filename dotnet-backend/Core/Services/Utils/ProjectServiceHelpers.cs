using System;
using System.IO;
using System.IO.Compression;
using ClosedXML.Excel;
using System.Data; // For using DataTable
using Core.Entities;
using ZstdSharp.Unsafe;

namespace Core.Services.Utils
{
    public static class ProjectServiceHelpers 
    {  
        /*
            Each worksheet in a workbook contains details of a given project, inncluding:
                - project info starting from (1, 1)
                - info about each asset starting from (5, 1)
        */
        public static (string, byte[]) GenerateProjectExportExcel(Project project, List<Asset> assets)
        {
            //Sheet 1: project detail including flattened metadata
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
            // Console.WriteLine(exportedAt.ToString("yyyyMMdd"));
            string exportedYMD = exportedAt.ToString("yyyyMMdd");
            string fileName = $"Project_{project.ProjectID}_export_{exportedYMD}.xlsx";

            // Add worksheet
            var wsProject = workbook.AddWorksheet($"Project {project.ProjectID}");
            // Set headers; .Cell(row, column)
            // wsProject.Cell(2, 2).Value = "Dessert Name Proj"; 
            // wsProject.Cell(2, 3).Value = "Sales Proj";

            // Insert projectdDataTable
            wsProject.Cell(1, 1).InsertTable(projectDataTable.AsEnumerable());


            var assetDataTable = new DataTable();
            assetDataTable.Columns.Add("Project ID", typeof(int));
            assetDataTable.Columns.Add("Name", typeof(string));
            assetDataTable.Columns.Add("Version", typeof(string));
            assetDataTable.Columns.Add("Location", typeof(string));
            assetDataTable.Columns.Add("Description", typeof(string));
            assetDataTable.Columns.Add("Creation Time", typeof(string));
            assetDataTable.Columns.Add("Active", typeof(string));
            assetDataTable.Columns.Add("Archived At", typeof(string));
            assetDataTable.Columns.Add("Tags", typeof(string));
            // Add custom metadata fields if needed

            assetDataTable.Rows.Add
            (
                project.ProjectID,
                project.Name,
                project.Version,
                project.Location,
                project.Description,
                project.CreationTime.ToString("yyyyMMdd"),
                active,
                archivedTime
            );


            // Insert assetDataTable
            wsProject.Cell(5, 1).InsertTable(assetDataTable.AsEnumerable());
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