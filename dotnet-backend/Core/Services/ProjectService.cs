using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;
using DataModel;
using ClosedXML.Excel;
using System.Data; // For using DataTable

namespace Core.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repository;
        public ProjectService(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<AddAssetsToProjectRes> AddAssetsToProject(string projectId, List<string> imageIds)
        {
            //TODO
            if (projectId == "") {
                throw new Exception("Empty project Id.");
            } else {
                try 
                {
                    bool isSuccessul = await _repository.AddAssetsToProjectInDb(projectId, imageIds);
                    if (isSuccessul)
                    {
                        AddAssetsToProjectRes result = new AddAssetsToProjectRes
                        {
                            UploadedAt = DateTime.UtcNow
                        };
                        return result;
                    }
                    else 
                    {
                        throw new Exception("Failed to add assets to project in database.");
                    }
                }
                catch (Exception ex) 
                {
                    throw;
                }
            }
        }

        public async Task<ArchiveProjectsRes> ArchiveProjects(List<string> projectIds)
         {
            //TODO
            if (projectIds.Count == 0) {
                throw new Exception("Empty projectIds.");
            } else {
                try 
                {
                    bool isSuccessul = await _repository.ArchiveProjectsInDb(projectIds);
                    if (isSuccessul)
                    {
                        ArchiveProjectsRes result = new ArchiveProjectsRes{archiveTimestamp = DateTime.UtcNow};
                        return result;
                    }
                    else 
                    {
                        throw new Exception("Failed to archive projects in database.");
                    }
                }
                catch (Exception ex) 
                {
                    throw;
                }
            }
        }

        public async Task<GetArchivedProjectLogsRes> GetArchivedProjectLogs()
        {
            try 
            { 
                //TODO
                List<Log> retrievedLogs = await _repository.GetArchivedProjectLogsInDb();
            
                ArchivedProjectLog log1 = new ArchivedProjectLog
                {
                    projectId = "123",
                    projectName = "P1",
                    archivedAt = DateTime.UtcNow,
                    admin = "John"
                };
                ArchivedProjectLog log2 = new ArchivedProjectLog
                {
                    projectId = "456",
                    projectName = "P2",
                    archivedAt = DateTime.UtcNow,
                    admin = "Betty"
                };
                List<ArchivedProjectLog> logs = new List<ArchivedProjectLog>();
                logs.Add(log1);
                logs.Add(log2);
                GetArchivedProjectLogsRes result = new GetArchivedProjectLogsRes{logs = logs};
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<RetrieveProjectRes> RetrieveProject(string projectId) 
        {
            //TODO
            try 
            {
                Project Project = await _repository.RetrieveProjectInDb(projectId);
                RetrieveProjectRes result = new RetrieveProjectRes
                {
                    projectId = projectId,
                    projectName = "Project Name",
                    archived = true,
                    archivedAt = "2025-02-02T12:00:00Z",
                    admin = "John Doe"
                };
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<GetProjectAssetsRes> GetProjectAssets(string projectId, string type, int pageNumber, int pageSize)
        {
            //TODO
            int offset = (pageNumber - 1) * pageSize;
            try 
            {
                List<Asset> retrievedAssets = await _repository.GetProjectAssetsInDb(projectId, type, offset, pageSize);
                ProjectAssetsPagination pagination = new ProjectAssetsPagination{page = pageNumber, limit = pageSize, total = 2};
                List<string> tags1 = new List<string>();
                tags1.Add("fieldwork");
                tags1.Add("site");
                ProjectAssetMd metadata1 = new ProjectAssetMd{date = "2025-01-30T10:20:00Z", tags = tags1};

                List<string> tags2 = new List<string>();
                tags2.Add("inspection");
                ProjectAssetMd metadata2 = new ProjectAssetMd{date =  "2025-01-30T10:25:00Z", tags = tags2};

                ProjectAsset asset1 = new ProjectAsset
                {
                    id = "img001", 
                    thumbnailUrl = "https://cdn.example.com/thumbnails/img001.jpg",
                    filename = "image1.jpg",
                    projectAssetMd = metadata1
                };

                ProjectAsset asset2 = new ProjectAsset
                {
                    id = "img002", 
                    thumbnailUrl = "https://cdn.example.com/thumbnails/img002.jpg",
                    filename = "image2.jpg",
                    projectAssetMd = metadata2
                };

                List<ProjectAsset> assets = new List<ProjectAsset>();
                assets.Add(asset1);
                assets.Add(asset2);

                GetProjectAssetsRes result = new GetProjectAssetsRes{projectId = projectId, assets = assets, pagination = pagination};
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<byte[]> ExportProject(string projectId)
        {
            //TODO
            try
            {
                //Demo
                var dataTable = new DataTable();
                dataTable.Columns.Add("Name", typeof(string));
                dataTable.Columns.Add("Sold", typeof(int));
                dataTable.Rows.Add("Cheesecake", 14);
                dataTable.Rows.Add("Medovik", 6);
                dataTable.Rows.Add("Muffin", 10);

                // Fetch project and assets
                Project project = await _repository.RetrieveProjectInDb(projectId);
                List<Asset> assets = await _repository.GetProjectAssetsInDb(projectId);

                // if (project == null) {
                //     return null;
                // }

                using var workbook = new XLWorkbook();
                
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

                // Save the xlsx file to the current folder (APIs)
                string fileName = projectId + "_export.xlsx";
                workbook.SaveAs(fileName); 
                
                // Save the xlsx file as stream
                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    byte[] fileContent = stream.ToArray();
                    return fileContent;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
