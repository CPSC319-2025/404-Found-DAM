/*
This file uses the ClosedXML library, which is licensed under the MIT License.
------------------------------------------------------------------------------

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

-----------------------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using ClosedXML.Excel;
using Core.Services.Utils;

namespace Core.Services
{
    public class ProjectService : IProjectService
    {
        public const double MockedTotalAssetsReturned = 2;
        public const double MockedTotalPages = 10;
        private readonly IProjectRepository _repository;
        public ProjectService(IProjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<SubmitAssetsRes> SubmitAssets(int projectID, List<int> blobIDs)
        {
            //TODO
            //Assets will inherit project's metadata (at least tags)
            try 
            {
                bool isSuccessul = await _repository.SubmitAssetstoDb(projectID, blobIDs);
                if (isSuccessul)
                {
                    SubmitAssetsRes result = new SubmitAssetsRes
                    {
                        submittedAt = DateTime.UtcNow
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

                        // TODO: Remove all regular users from this archived project.

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

        public async Task<GetProjectRes> GetProject(int projectID) 
        {
            //TODO
            try 
            {
                (Project project, string projectAdmin, List<string> projectTags) = await _repository.GetProjectInDb(projectID);
                GetProjectRes result = new GetProjectRes
                {
                    projectID = project.ProjectID,
                    name = project.Name,
                    description = project.Description,
                    location = project.Description,
                    archived = true,
                    archivedAt = "2025-02-02T12:00:00Z",
                    admin = projectAdmin,
                    tags = projectTags
                };
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req)
        {
            //TODO
            int offset = (req.pageNumber - 1) * req.assetsPerPage;
            try 
            {
                List<Asset> retrievedAssets = await _repository.GetPaginatedProjectAssetsInDb(req, offset);

                int totalPages = (int)Math.Ceiling( MockedTotalAssetsReturned / MockedTotalPages);

                ProjectAssetsPagination pagination = new ProjectAssetsPagination{pageNumber = req.pageNumber, assetsPerPage = req.assetsPerPage, totalAssetsReturned = (int)MockedTotalAssetsReturned, totalPages  = (int)MockedTotalPages};
                List<string> tags1 = new List<string>();
                tags1.Add("fieldwork");
                tags1.Add("site");
                ProjectAssetMD metadata1 = new ProjectAssetMD{date = new DateTime(2025, 01, 30, 10, 20, 00, 1), tags = tags1};

                List<string> tags2 = new List<string>();
                tags2.Add("inspection");
                ProjectAssetMD metadata2 = new ProjectAssetMD{date = new DateTime(2025, 01, 30, 10, 25, 00, 1), tags = tags2};

                ProjectAsset asset1 = new ProjectAsset
                {
                    blobID = 12356093, 
                    thumbnailUrl = "https://cdn.example.com/thumbnails/img001.jpg",
                    filename = "image1.jpg",
                    metadata = metadata1
                };

                ProjectAsset asset2 = new ProjectAsset
                {
                    blobID = 123560623, 
                    thumbnailUrl = "https://cdn.example.com/thumbnails/img002.jpg",
                    filename = "image2.jpg",
                    metadata = metadata2
                };

                List<ProjectAsset> assets = new List<ProjectAsset>();
                assets.Add(asset1);
                assets.Add(asset2);

                GetPaginatedProjectAssetsRes result = new GetPaginatedProjectAssetsRes{projectID = req.projectID, assets = assets, pagination = pagination};
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
                (Project project, string admin, List<string> tags) = await _repository.GetProjectInDb(projectID);
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
