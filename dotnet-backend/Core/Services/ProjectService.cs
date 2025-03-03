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

        public async Task<AddAssetsToProjectRes> AddAssetsToProject(string projectId, List<string> imageIds)
        {
            //TODO
            //Assets will inherit project's metadata (at least tags)
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

        public async Task<(string, byte[])> ExportProject(string projectId)
        {
            //TODO
            try
            {
                // Fetch project and assets
                Project project = await _repository.RetrieveProjectInDb(projectId);
                List<Asset> assets = await _repository.GetProjectAssetsInDb(projectId);

                // if (project == null) {
                //     return null;
                // }

                (string fileName, byte[] excelByteArray) = ProjectServiceHelpers.GenerateProjectExportExcel(projectId);
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