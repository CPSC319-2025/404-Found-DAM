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

            // TODO: Create an OrderedDictionary and make the project's matadatafields as keys with null values

            // Add Columns for asset details
            var assetDataTable = new DataTable();
            assetDataTable.Columns.Add("Blob ID", typeof(int));
            assetDataTable.Columns.Add("File Name", typeof(string));
            assetDataTable.Columns.Add("Mime Type", typeof(string));
            assetDataTable.Columns.Add("File Size (KB)", typeof(double));
            assetDataTable.Columns.Add("Last Updated (UTC)", typeof(string));
            assetDataTable.Columns.Add("Tags", typeof(string));
            // TODO: insert project's matadatafields using that OrderedDictionary

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

                // TODO: retrieve the asset's metadata values, match them with the OrderedDictionary's keys and insert the values

                assetDataTable.Rows.Add
                (
                    a.BlobID,
                    a.FileName,
                    a.MimeType,
                    a.FileSizeInKB,
                    a.LastUpdated.ToString(),
                    assetTagNameStr

                    // TODO: Add all values in the OrderedDictionary
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

        public static (
            List<Project> Projects,
            List<ProjectTag> ProjectTags,
            List<Tag> Tags,
            List<Asset> Assets,
            List<AssetTag> AssetTags,
            List<User> Users,
            List<ProjectMembership> ProjectMemberships
        ) CreateProjectForImport(ZipArchiveEntry entry) 
        {
            Console.WriteLine($"zipArchive entry full name: {entry.FullName}");
            using Stream entryStream = entry.Open();

            // Create project and establish collection relations
            // Create Users and establish ProjectMemberships

            using var workbook = new XLWorkbook(entryStream); // Create an Excel workbook instance

            var wsProject = workbook.Worksheet(1); 
            var wsMembers = workbook.Worksheet(2);
            var nonEmptyProjectSheetRows = wsProject.RowsUsed(); 
            var nonEmptyMemberSheetRows = wsMembers.RowsUsed(); 
            
            List<Project> projectList = new List<Project>();
            List<ProjectTag> projectTagList = new List<ProjectTag>();
            List<Tag> tagList = new List<Tag>();
            List<Asset> assetList = new List<Asset>();
            List<AssetTag> assetTagList = new List<AssetTag>();
            List<User> userList = new List<User>();
            List<ProjectMembership> projectMembershipList = new List<ProjectMembership>();

            // TODO: May add metadatafield and AssetMetadata if needed later. 

            int projectSheetRowCount = 1;

            foreach (var projectSheetRow in nonEmptyProjectSheetRows) 
            {
                if (projectSheetRowCount == 2) 
                {
                    // Create project
                    Project p = new Project 
                    {
                        Name = projectSheetRow.Cell(2).GetValue<string>(),
                        Version = projectSheetRow.Cell(3).GetValue<string>(),
                        Location = projectSheetRow.Cell(4).GetValue<string>(),
                        Description = projectSheetRow.Cell(5).GetValue<string>(),
                        CreationTime =  DateTimeOffset.Parse(projectSheetRow.Cell(6).GetValue<string>()).UtcDateTime,
                        Active = projectSheetRow.Cell(7).GetValue<bool>(),
                        ArchivedAt = projectSheetRow.Cell(7).GetValue<bool>() ? null : DateTimeOffset.Parse(projectSheetRow.Cell(8).GetValue<string>()).UtcDateTime
                    };
                    projectList.Add(p);

                    // Create ProjectTags and Tags
                    string extractedProjectTagString = projectSheetRow.Cell(9).GetValue<string>();
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
                    // Create assets
                    Asset a = new Asset
                    {
                        FileName = projectSheetRow.Cell(2).GetValue<string>(),
                        MimeType = projectSheetRow.Cell(3).GetValue<string>(),
                        FileSizeInKB = projectSheetRow.Cell(4).GetValue<double>(),
                        LastUpdated =  DateTimeOffset.Parse(projectSheetRow.Cell(5).GetValue<string>()).UtcDateTime,
                        assetState = Asset.AssetStateType.SubmittedToProject,
                        Project = projectList[0]
                    };

                    // Create AssetTags and Tags
                    string extractedAssetTagString = projectSheetRow.Cell(6).GetValue<string>();
                    List<string> tagNames = extractedAssetTagString.Split(',').Select(tag => tag.Trim()).ToList();
                    foreach (string tagName in tagNames)
                    {
                        Tag t = new Tag { Name = tagName };
                        AssetTag at = new AssetTag 
                        {
                            Asset = a,
                            Tag = t
                        };
                        assetTagList.Add(at);
                        tagList.Add(t);
                    }

                    // TODO: May create metadatafield and AssetMetadata if needed later.
                }
                projectSheetRowCount++;
            }

            // Create users & relations
            int memberSheetRowCount = 1;
            foreach (var memberSheetRow in nonEmptyMemberSheetRows)
            {
                if (memberSheetRowCount >= 2) {
                    User u = new User 
                    {
                        Name = memberSheetRow.Cell(2).GetValue<string>(),
                        Email = memberSheetRow.Cell(3).GetValue<string>(),
                        IsSuperAdmin = memberSheetRow.Cell(4).GetValue<bool>(),
                        LastUpdated = DateTimeOffset.Parse(memberSheetRow.Cell(5).GetValue<string>()).UtcDateTime,
                    };
                    userList.Add(u);

                    // Create ProjectMembership
                    ProjectMembership pm = new ProjectMembership {
                        Project = projectList[0],
                        User = u,
                        UserRole = memberSheetRow.Cell(6).GetValue<string>() == "admin" ? ProjectMembership.UserRoleType.Admin : ProjectMembership.UserRoleType.Regular
                    };
                    projectMembershipList.Add(pm);
                }
                memberSheetRowCount++;
            } 

            return (projectList, projectTagList, tagList, assetList, assetTagList, userList, projectMembershipList);
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