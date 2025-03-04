using System;
using System.IO;
using System.IO.Compression;
using ClosedXML.Excel;
using System.Data; // For using DataTable

namespace Core.Services.Utils
{
    public static class ProjectServiceHelpers 
    {  
        public static (string, byte[]) GenerateProjectExportExcel(int projectID)
        {
            //Demo
            var dataTable = new DataTable();
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Sold", typeof(int));
            dataTable.Rows.Add("Cheesecake", 14);
            dataTable.Rows.Add("Medovik", 6);
            dataTable.Rows.Add("Muffin", 10);

            using var workbook = new XLWorkbook(); // Create an Excel workbook instance
            DateTime exportedAt = DateTime.Now;
            // Console.WriteLine(exportedAt.ToString("yyyyMMdd"));
            string exportedYMD = exportedAt.ToString("yyyyMMdd");
            string fileName = $"Project_{projectID}_export_{exportedYMD}.xlsx";

            //Sheet 1: project detail including flattened metadata
            // Add worksheet
            var wsProject = workbook.AddWorksheet("Project");
            // Set headers; .Cell(row, column)
            wsProject.Cell(2, 2).Value = "Dessert Name Proj"; 
            wsProject.Cell(2, 3).Value = "Sales Proj";
            // Insert dataTable
            wsProject.Cell(3, 2).InsertTable(dataTable.AsEnumerable());

            //Sheet 2: each asset detail including flattened metadata
            // Add worksheet
            var wsAssets = workbook.AddWorksheet("Assets");
            // Set headers
            wsAssets.Cell(2, 2).Value = "Dessert Name Assets"; 
            wsAssets.Cell(2, 3).Value = "Sales Assets";
            // Insert dataTable
            wsAssets.Cell(3, 2).InsertTable(dataTable.AsEnumerable());

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