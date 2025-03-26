using System;
using System.IO;
using System.IO.Compression;
using ClosedXML.Excel;
using System.Data; // For using DataTable
using Core.Entities;
using Core.Dtos;

namespace Core.Services.Utils
{
    public static class AdminServiceHelpers 
    {  
        private const int _startRow = 1;
        private const int _startColumn = 1;
        private const int _assetStartColumn = 7;


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
            projectDataTable.Columns.Add("Creation Time (UTC)", typeof(string));
            projectDataTable.Columns.Add("Active", typeof(bool));
            projectDataTable.Columns.Add("Archived At (UTC)", typeof(string));
            projectDataTable.Columns.Add("Tags", typeof(string));

            string archivedTime = project.ArchivedAt == null ? "N/A" : project.ArchivedAt.Value.ToString();
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
                project.CreationTime.ToString(),
                project.Active,
                archivedTime,
                projectTagNameStr
            );

            List<int> orderedProjectMetadatafieldList = new List<int>(); // Keep track projectMetadatafield's order for inserting 

            // Create a dictionary for tracking project metadata info and the value in assetmetadata
            // Dictionary<projectMetadatafield, ExportProjectDto>
            Dictionary<int, ExportProjectDto> projectMetadatafiledDict = new Dictionary<int, ExportProjectDto>();
            foreach (ProjectMetadataField pmf in project.ProjectMetadataFields)
            {
                if (!projectMetadatafiledDict.ContainsKey(pmf.FieldID)) 
                {
                    ExportProjectDto val = new ExportProjectDto
                    {
                        fieldName = pmf.FieldName,
                        fieldDataType = pmf.FieldType,
                        assetMetadataVal = null
                    };
                    projectMetadatafiledDict[pmf.FieldID] = val; 
                    orderedProjectMetadatafieldList.Add(pmf.FieldID);
                }
            }


            // Add Columns for asset details
            var assetDataTable = new DataTable();
            assetDataTable.Columns.Add("Blob ID", typeof(string));
            assetDataTable.Columns.Add("File Name", typeof(string));
            assetDataTable.Columns.Add("Mime Type", typeof(string));
            assetDataTable.Columns.Add("File Size (KB)", typeof(double));
            assetDataTable.Columns.Add("Last Updated (UTC)", typeof(string));
            assetDataTable.Columns.Add("Tags", typeof(string));
             
            // Insert project's matadatafields using the dictionary
            if (projectMetadatafiledDict.Count > 0)
            {         
                foreach (int pmf in orderedProjectMetadatafieldList)
                {
                    ExportProjectDto val = projectMetadatafiledDict[pmf];
                    ProjectMetadataField.FieldDataType fieldType = val.fieldDataType;
                    Type type = fieldType == ProjectMetadataField.FieldDataType.Number 
                        ? typeof(double)
                        :  fieldType == ProjectMetadataField.FieldDataType.Boolean 
                            ? typeof(bool)
                            : typeof(string);

                    assetDataTable.Columns.Add(val.fieldName, type);
                }
            }
   

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

                // Fill assetMetadata field value 
                List<AssetMetadata> amList = a.AssetMetadata.ToList();
                foreach (AssetMetadata am in amList)
                {
                    if (projectMetadatafiledDict.ContainsKey(am.FieldID))
                    {
                        ExportProjectDto val = projectMetadatafiledDict[am.FieldID];
                        val.assetMetadataVal = am.FieldValue;
                        projectMetadatafiledDict[am.FieldID] = val;
                    }
                }

                DataRow assetDataRow = assetDataTable.NewRow(); // Create a new row

                assetDataRow["Blob ID"] = a.BlobID;
                assetDataRow["File Name"] = a.FileName;
                assetDataRow["Mime Type"] = a.MimeType;
                assetDataRow["File Size (KB)"] = a.FileSizeInKB;
                assetDataRow["Last Updated (UTC)"] = a.LastUpdated.ToString();
                assetDataRow["Tags"] = assetTagNameStr; 

                foreach (int pmf in orderedProjectMetadatafieldList) // Continue to add to existing row
                {
                    ExportProjectDto val = projectMetadatafiledDict[pmf];
                    if (val.assetMetadataVal != null)
                    {
                        assetDataRow[val.fieldName] = val.assetMetadataVal; 
                    }
                }

                assetDataTable.Rows.Add(assetDataRow); // Add the finalized assetDataRow to assetDataTable

                // Clear the dictionary values
                foreach (var key in projectMetadatafiledDict.Keys)
                {
                    ExportProjectDto val = projectMetadatafiledDict[key];
                    val.assetMetadataVal = null;
                    projectMetadatafiledDict[key] = val;
                }
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
            memberDataTable.Columns.Add("Is SuperAdmin", typeof(bool));
            memberDataTable.Columns.Add("Last Updated (UTC)", typeof(string));
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
                    m.LastUpdated.ToString(),
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
                    m.LastUpdated.ToString(),
                    "user"
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

        public static (List<Project>, List<ProjectTag>, List<Tag>, List<ImportUserProfile>) CreateProjectForImport(ZipArchiveEntry entry) 
        {
            // Console.WriteLine($"zipArchive entry full name: {entry.FullName}");
            using Stream entryStream = entry.Open();

            // Create project and establish collection relations
            // Create Users and establish ProjectMemberships

            using var workbook = new XLWorkbook(entryStream); // Create an Excel workbook instance

            var wsProject = workbook.Worksheet(1); 
            var nonEmptyProjectSheetRows = wsProject.RowsUsed(); 
            
            List<Project> projectList = new List<Project>();
            List<ProjectTag> projectTagList = new List<ProjectTag>();
            List<Tag> tagList = new List<Tag>();
            List<ImportUserProfile> importUserProfileList = new List<ImportUserProfile>();


            int projectSheetRowCount = 1;

            foreach (var projectSheetRow in nonEmptyProjectSheetRows) 
            {
                if (projectSheetRowCount == 2) 
                {
                    // Create project
                    Project p = new Project 
                    {
                        Name = projectSheetRow.Cell(1).GetValue<string>(),
                        Version = "0.0",
                        Location = projectSheetRow.Cell(2).GetValue<string>(),
                        Description = projectSheetRow.Cell(3).GetValue<string>(),
                        // CreationTime =  DateTimeOffset.Parse(projectSheetRow.Cell(6).GetValue<string>()).UtcDateTime,
                        CreationTime = DateTime.UtcNow,
                        Active = true
                        // ArchivedAt = projectSheetRow.Cell(7).GetValue<bool>() ? null : DateTimeOffset.Parse(projectSheetRow.Cell(8).GetValue<string>()).UtcDateTime
                    };
                    projectList.Add(p);

                    // Create ProjectTags and Tags
                    string extractedProjectTagString = projectSheetRow.Cell(4).GetValue<string>();
                    List<string> tagNames = extractedProjectTagString.Split(',').Select(tag => tag.Trim()).ToList();
                    foreach (string tagName in tagNames)
                    {
                        Tag t = new Tag { Name = tagName };
                        ProjectTag pt = new ProjectTag 
                        {
                            Project = p,
                            Tag = t
                        };
                        projectTagList.Add(pt);
                        tagList.Add(t);
                    }
                }
                else if (projectSheetRowCount >= 4) 
                {
                    // Collect user profiles
                    ImportUserProfile importUserProfile = new ImportUserProfile
                    {
                        userID = projectSheetRow.Cell(1).GetValue<int>(),
                        userName = projectSheetRow.Cell(2).GetValue<string>(),
                        userEmail = projectSheetRow.Cell(3).GetValue<string>(),
                        userIsSuperAdmin = projectSheetRow.Cell(4).GetValue<bool>(),
                        userRole = projectSheetRow.Cell(5).GetValue<string>().ToLower() == "admin" ? ProjectMembership.UserRoleType.Admin : ProjectMembership.UserRoleType.Regular
                    };

                    importUserProfileList.Add(importUserProfile);
                }
                projectSheetRowCount++;
            }

            return (projectList, projectTagList, tagList, importUserProfileList);
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