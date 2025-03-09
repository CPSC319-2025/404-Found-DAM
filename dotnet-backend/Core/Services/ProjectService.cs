﻿/*
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
using Infrastructure.Exceptions;
using DocumentFormat.OpenXml.Spreadsheet;


namespace Core.Services
{
    public class ProjectService : IProjectService
    {
        private const string DateTimeFormatUTC = "yyyy-MM-ddTHH:mm:ss.fffZ";

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
                        return result;
                    }
                    else 
                    {
                        throw new Exception("Failed to archive projects in database.");
                    }
                }
                catch (DataNotFoundException) 
                {
                    throw;
                }
                catch (Exception) 
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
            try 
            {
                (Project project, string projectAdmin, List<string> projectTags) = await _repository.GetProjectInDb(projectID);
                GetProjectRes result = new GetProjectRes
                {
                    projectID = project.ProjectID,
                    name = project.Name,
                    description = project.Description,
                    location = project.Description,
                    archived = project.Active,
                    archivedAt = project.Active ? project.ArchivedAt.ToString(DateTimeFormatUTC) : "n/a",
                    admin = projectAdmin,
                    tags = projectTags
                };
                return result;
            }
            catch (DataNotFoundException)
            {
                throw;
            }
        }

        public async Task<GetAllProjecsRes> GetAllProjects(int userID) 
        {
            try 
            {
                List<Project> retrievedProjects;
                List<User> retrievedUsers;
                List<ProjectMembership> retrievedProjectMemberships;

                (retrievedProjects, retrievedUsers, retrievedProjectMemberships) = 
                    await _repository.GetAllProjectsInDb(userID);

                // Make List retrievedUsers into a map
                Dictionary<int, User> retrievedUserDictionary = retrievedUsers.ToDictionary(u => u.UserID);

                GetAllProjecsRes result = new GetAllProjecsRes();

                result.projectCount = retrievedProjects.Count;

                // Create a projectMembershipMap for constructig the return result
                Dictionary<int, List<string>> projectMembershipMap = new Dictionary<int, List<string>>();
                
                foreach (ProjectMembership pm in retrievedProjectMemberships) 
                {
                    if (!projectMembershipMap.ContainsKey(pm.ProjectID))
                    {
                        projectMembershipMap[pm.ProjectID] = new List<string>();
                    }

                    if (retrievedUserDictionary.ContainsKey(pm.UserID)) 
                    {
                        projectMembershipMap[pm.ProjectID].Add(retrievedUserDictionary[pm.UserID].Name);
                    }
                }

                // Constructing the result for return by looping through retrievedProjects and using the retrievedUserDictionary 
                // Create a HashSet to prevent duplicated retrievedProjects
                HashSet<Project> addedProjects = new HashSet<Project>();
                foreach (var p in retrievedProjects)
                {
                    if (!addedProjects.Contains(p)) 
                    {
                        addedProjects.Add(p);

                        // Populate fullProjectInfo
                        FullProjectInfo fullProjectInfo = new FullProjectInfo(); 
                        fullProjectInfo.projectID = p.ProjectID;
                        fullProjectInfo.projectName = p.Name;
                        fullProjectInfo.location = p.Location;
                        fullProjectInfo.description = p.Description;
                        fullProjectInfo.creationTime = p.CreationTime;
                        fullProjectInfo.active = p.Active;
                        fullProjectInfo.archivedAt = p.Active ? p.ArchivedAt : null;
                        fullProjectInfo.assetCount = p.Assets != null ? p.Assets.Count : 0; // Get p's associated assets for count
                        fullProjectInfo.userNames = projectMembershipMap[p.ProjectID];

                        // Add fullProjectInfo to result
                        result.fullProjectInfos.Add(fullProjectInfo);
                    }
                }
                return result;
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception)
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
                int totalAssetsReturned = retrievedAssets.Count;
                int totalPages = (int)Math.Ceiling((double)totalAssetsReturned / req.assetsPerPage);

                ProjectAssetsPagination pagination = new ProjectAssetsPagination
                {
                    pageNumber = req.pageNumber, 
                    assetsPerPage = req.assetsPerPage, 
                    totalAssetsReturned = totalAssetsReturned, 
                    totalPages = totalPages
                };
                
                // Loop through retrievedAssets to build the return result
                List<PaginatedProjectAsset> paginatedProjectAssets = new List<PaginatedProjectAsset>();
                foreach (Asset a in retrievedAssets)
                {
                    if (a != null) {
                        PaginatedProjectAsset paginatedProjectAsset = new PaginatedProjectAsset();
                        paginatedProjectAsset.blobID = a.BlobID;
                        // paginatedProjectAsset.thumbnailUrl = a.thumbnailUrl;
                        paginatedProjectAsset.filename = a.FileName;
                        if (a.User != null) 
                        {
                            paginatedProjectAsset.uploadedBy = new PaginatedProjectAssetUploadedBy
                            {
                                userID = a.User.UserID, name = a.User.Name
                            };
                        }

                        paginatedProjectAsset.date = a.LastUpdated;
                        paginatedProjectAsset.filesizeInKB = a.FileSizeInKB;

                        if (a.AssetMetadata != null)
                        {
                            foreach (AssetMetadata am in a.AssetMetadata)
                            {
                                if (am.MetadataField != null)
                                {
                                    // for 
                                }
                            }
                        }
                    }
                }
                GetPaginatedProjectAssetsRes result = new GetPaginatedProjectAssetsRes{projectID = req.projectID, assets = paginatedProjectAssets, pagination = pagination};
                return result;
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (Exception)
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
