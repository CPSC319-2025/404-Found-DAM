using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;
using DataModel;
using ClosedXML.Excel;
using Core.Services.Utils;

namespace Core.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repository;
        public ProjectService(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<AddAssetsToProjectRes> AddAssetsToProject(int projectID, List<int> assetIDs)
        {
            //TODO
            //Assets will inherit project's metadata (at least tags)
            try 
            {
                bool isSuccessul = await _repository.AddAssetsToProjectInDb(projectID, assetIDs);
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

        public async Task<ArchiveProjectsRes> ArchiveProjects(List<int> projectIDs)
         {
            //TODO
            if (projectIDs.Count == 0) {
                throw new Exception("Empty projectIDs.");
            } else {
                try 
                {
                    bool isSuccessul = await _repository.ArchiveProjectsInDb(projectIDs);
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
                    projectID = 123,
                    projectName = "P1",
                    archivedAt = DateTime.UtcNow,
                    admin = "John"
                };
                ArchivedProjectLog log2 = new ArchivedProjectLog
                {
                    projectID = 456,
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

        public async Task<RetrieveProjectRes> RetrieveProject(int projectID) 
        {
            //TODO
            try 
            {
                Project Project = await _repository.RetrieveProjectInDb(projectID);
                RetrieveProjectRes result = new RetrieveProjectRes
                {
                    projectID = projectID,
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


        public async Task<GetProjectAssetsRes> GetProjectAssets(int projectID, string type, int pageNumber, int pageSize)
        {
            //TODO
            int offset = (pageNumber - 1) * pageSize;
            try 
            {
                List<Asset> retrievedAssets = await _repository.GetProjectAssetsInDb(projectID, type, offset, pageSize);
                ProjectAssetsPagination pagination = new ProjectAssetsPagination{page = pageNumber, limit = pageSize, total = 2};
                List<string> tags1 = new List<string>();
                tags1.Add("fieldwork");
                tags1.Add("site");
                ProjectAssetMD metadata1 = new ProjectAssetMD{date = new DateTime(2025, 01, 30, 10, 20, 00, 1), tags = tags1};

                List<string> tags2 = new List<string>();
                tags2.Add("inspection");
                ProjectAssetMD metadata2 = new ProjectAssetMD{date = new DateTime(2025, 01, 30, 10, 25, 00, 1), tags = tags2};

                ProjectAsset asset1 = new ProjectAsset
                {
                    assetID = 12356093, 
                    thumbnailUrl = "https://cdn.example.com/thumbnails/img001.jpg",
                    filename = "image1.jpg",
                    projectAssetMD = metadata1
                };

                ProjectAsset asset2 = new ProjectAsset
                {
                    assetID = 123560623, 
                    thumbnailUrl = "https://cdn.example.com/thumbnails/img002.jpg",
                    filename = "image2.jpg",
                    projectAssetMD = metadata2
                };

                List<ProjectAsset> assets = new List<ProjectAsset>();
                assets.Add(asset1);
                assets.Add(asset2);

                GetProjectAssetsRes result = new GetProjectAssetsRes{projectID = projectID, assets = assets, pagination = pagination};
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<(string, byte[])> ExportProject(int projectID)
        {
            //TODO
            try
            {
                // Fetch project and assets
                Project project = await _repository.RetrieveProjectInDb(projectID);
                List<Asset> assets = await _repository.GetProjectAssetsInDb(projectID);

                // if (project == null) {
                //     return null;
                // }

                (string fileName, byte[] excelByteArray) = ProjectServiceHelpers.GenerateProjectExportExcel(projectID);
                return (fileName, excelByteArray);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}


/*
May Move the following to License

-----------------------------------------------------------------------------
MIT License

Copyright (c) 2016 ClosedXML

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/