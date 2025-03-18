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

        public async Task<SubmitAssetsRes> SubmitAssets(int projectID, List<int> blobIDs, int submitterID)
        {
            try 
            {
                (List<int> successfulSubmissions, List<int> failedSubmissions) = await _repository.SubmitAssetstoDb(projectID, blobIDs, submitterID);
                SubmitAssetsRes result = new SubmitAssetsRes
                {
                    projectID = projectID,
                    successfulSubmissions = successfulSubmissions,
                    failedSubmissions = failedSubmissions,
                    submittedAt = DateTime.UtcNow
                };
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
                catch (PartialSuccessException) 
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

        /*
            GetProject method allows all valid users to retrieve any project in the DB, 
            regardless of whether they are part of it or not.
        */
        public async Task<GetProjectRes> GetProject(int projectID) 
        {
            try 
            {
                Project project = await _repository.GetProjectInDb(projectID);
                List<UserCustomInfo> adminList = new List<UserCustomInfo>();
                List<UserCustomInfo> regularUserList = new List<UserCustomInfo>();

                foreach (ProjectMembership pm in project.ProjectMemberships)
                {
                    var userInfo = new UserCustomInfo
                    {
                        name = pm.User.Name,
                        email = pm.User.Email,
                        userID = pm.User.UserID
                    };

                    if (pm.UserRole == ProjectMembership.UserRoleType.Admin)
                    {
                        adminList.Add(userInfo);
                    }
                    else
                    {
                        regularUserList.Add(userInfo);
                    }
                }

                List<TagCustomInfo> tags = project.ProjectTags
                    .Select(pt => new TagCustomInfo
                    {
                        tagID = pt.Tag.TagID,
                        name = pt.Tag.Name
                    })
                    .ToList();

                // TODO: Check if the user is admin or regular. If user is regular and if project is archived, throw ArchivedException 

                GetProjectRes result = new GetProjectRes
                {
                    projectID = project.ProjectID,
                    name = project.Name,
                    description = project.Description,
                    location = project.Location,
                    archived = project.Active,
                    archivedAt = project.ArchivedAt,
                    admins = adminList,
                    regularUsers = regularUserList,
                    tags = tags
                };

                return result;
            }
            catch (DataNotFoundException)
            {
                throw;
            }
            catch (ArchivedException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<GetAllProjectsRes> GetAllProjects(int requesterID) 
        {
            try 
            {
                List<Project> retrievedProjects;
                List<User> retrievedUsers;
                List<ProjectMembership> retrievedProjectMemberships;

                (retrievedProjects, retrievedUsers, retrievedProjectMemberships) = 
                    await _repository.GetAllProjectsInDb(requesterID);

                Dictionary<int, User> retrievedUserDictionary = retrievedUsers.ToDictionary(u => u.UserID);

                GetAllProjectsRes result = new GetAllProjectsRes();

                result.fullProjectInfos = new List<FullProjectInfo>();

                result.projectCount = retrievedProjects.Count;

                Dictionary<int, (HashSet<UserCustomInfo>, HashSet<UserCustomInfo>)> projectMembershipMap = new Dictionary<int, (HashSet<UserCustomInfo>, HashSet<UserCustomInfo>)>();
                
                foreach (ProjectMembership pm in retrievedProjectMemberships) 
                {
                    if (!projectMembershipMap.ContainsKey(pm.ProjectID))
                    {
                        projectMembershipMap[pm.ProjectID] = (new HashSet<UserCustomInfo>(), new HashSet<UserCustomInfo>());
                    }

                    if (retrievedUserDictionary.ContainsKey(pm.UserID)) 
                    {
                        var user = retrievedUserDictionary[pm.UserID];
                        var userInfo = new UserCustomInfo
                        {
                            name = user.Name,
                            email = user.Email,
                            userID = user.UserID
                        };

                        (HashSet<UserCustomInfo> adminSet, HashSet<UserCustomInfo> regularSet) = projectMembershipMap[pm.ProjectID];
                        
                        if (pm.UserRole == ProjectMembership.UserRoleType.Admin) 
                        {
                            adminSet.Add(userInfo);
                        }
                        else if (pm.UserRole == ProjectMembership.UserRoleType.Regular) 
                        {
                            regularSet.Add(userInfo);
                        }
                        projectMembershipMap[pm.ProjectID] = (adminSet, regularSet); // Update the dictionary       
                    }
                }

                // Constructing the result for return by looping through retrievedProjects
                HashSet<Project> addedProjects = new HashSet<Project>();
                foreach (var p in retrievedProjects)
                {
                    if (!addedProjects.Contains(p)) 
                    {
                        addedProjects.Add(p);

                        // Populate fullProjectInfo
                        (HashSet<UserCustomInfo> adminSet, HashSet<UserCustomInfo> regularSet) = projectMembershipMap[p.ProjectID];
                        FullProjectInfo fullProjectInfo = new FullProjectInfo(); 
                        fullProjectInfo.projectID = p.ProjectID;
                        fullProjectInfo.projectName = p.Name;
                        fullProjectInfo.location = p.Location;
                        fullProjectInfo.description = p.Description;
                        fullProjectInfo.creationTime = p.CreationTime;
                        fullProjectInfo.active = p.Active;
                        fullProjectInfo.archivedAt = p.Active ? p.ArchivedAt : null;
                        fullProjectInfo.assetCount = p.Assets != null ? p.Assets.Count : 0; // Get p's associated assets for count
                        fullProjectInfo.admins = adminSet;
                        fullProjectInfo.regularUsers = regularSet;

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


        public async Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req, int requesterID)
        {
            //TODO
            int offset = (req.pageNumber - 1) * req.assetsPerPage;
            try 
            {
                List<Asset> retrievedAssets = await _repository.GetPaginatedProjectAssetsInDb(req, offset, requesterID);
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
                        paginatedProjectAsset.tags = a.AssetTags.Select(t => t.Tag.Name).ToList();

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
    }
}