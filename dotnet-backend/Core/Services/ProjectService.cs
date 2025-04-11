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
using Microsoft.AspNetCore.Http;


namespace Core.Services
{
    public class ProjectService : IProjectService
    {
        private const string DateTimeFormatUTC = "yyyy-MM-ddTHH:mm:ss.fffZ";

        private readonly IProjectRepository _repository;
        private readonly IBlobStorageService _blobStorageService;

        public ProjectService(IProjectRepository repository, IBlobStorageService blobStorageService)
        {
            _repository = repository;
            _blobStorageService = blobStorageService;
        }
        
        public async Task<AssociateAssetsWithProjectRes> AssociateAssetsWithProject(AssociateAssetsWithProjectReq request, int submitterID)
        {
            (List<string> successfulAssociations, List<string> failedAssociations) = await _repository.AssociateAssetsWithProjectInDb(request.ProjectID, request.BlobIDs, submitterID);
            foreach (string blobId in successfulAssociations)
            {
                foreach (int tagId in request.TagIDs)
                {
                    await _repository.AddAssetTagAssociationAsync(blobId, tagId);
                }
                foreach (var metadata in request.MetadataEntries)
                {
                    await _repository.UpsertAssetMetadataAsync(blobId, metadata.FieldId, metadata.FieldValue);
                }
            }
            return new AssociateAssetsWithProjectRes
            {
                ProjectID = request.ProjectID,
                UpdatedImages = successfulAssociations,
                FailedAssociations = failedAssociations,
                Message = "Assets associated with project along with tags and metadata successfully."
            };
        }

        public async Task<ArchiveProjectRes> ArchiveProject(int projectID)
         {
            try 
            {
                await _repository.ArchiveProjectInDb(projectID);

                return new ArchiveProjectRes {projectID = projectID, archiveTimestampUTC = DateTime.UtcNow};
            }
            catch (InvalidOperationException) 
            {
                throw;
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

                GetProjectRes result = new GetProjectRes
                {
                    projectID = project.ProjectID,
                    name = project.Name,
                    description = project.Description,
                    location = project.Location,
                    active = project.Active,
                    archivedAt = project.ArchivedAt,
                    admins = adminList,
                    regularUsers = regularUserList,
                    tags = tags,
                    metadataFields = project.ProjectMetadataFields.Select(field => new ProjectMetadataCustomInfo
                    {
                        FieldName = field.FieldName,
                        FieldID = field.FieldID,
                        IsEnabled = field.IsEnabled,
                        FieldType = Enum.GetName(typeof(ProjectMetadataField.FieldDataType), field.FieldType)
                    }).ToList()
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

        public async Task<GetAllProjectsRes> GetAllProjects() 
        {
            try 
            {
                List<Project> retrievedProjects;
                List<User> retrievedUsers;
                List<ProjectMembership> retrievedProjectMemberships;

                try
                {
                    (retrievedProjects, retrievedUsers, retrievedProjectMemberships) = 
                        await _repository.GetAllProjectsInDb();
                }
                catch (Exception e)
                {
                    throw;
                }
                
                Dictionary<int, User> retrievedUserDictionary = retrievedUsers.ToDictionary(u => u.UserID);

                GetAllProjectsRes result = new GetAllProjectsRes();
                result.fullProjectInfos = new List<FullProjectInfo>();
                result.projectCount = retrievedProjects.Count;

                Dictionary<int, (HashSet<UserCustomInfo>, HashSet<UserCustomInfo>)> projectMembershipMap = new Dictionary<int, (HashSet<UserCustomInfo>, HashSet<UserCustomInfo>)>();

                foreach (ProjectMembership pm in retrievedProjectMemberships) 
                {
                    try
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
                            projectMembershipMap[pm.ProjectID] = (adminSet, regularSet);
                        }
                        else
                        {
                            Console.WriteLine($"UserID {pm.UserID} not found in dictionary.");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing ProjectMembership with UserID {pm.UserID}: {e.Message}");
                    }
                }

                HashSet<Project> addedProjects = new HashSet<Project>();
                foreach (var p in retrievedProjects)
                {
                    try
                    {
                        if (!addedProjects.Contains(p)) 
                        {
                            addedProjects.Add(p);

                            var (adminSet, regularSet) = projectMembershipMap.ContainsKey(p.ProjectID) ? projectMembershipMap[p.ProjectID] : (new HashSet<UserCustomInfo>(), new HashSet<UserCustomInfo>());

                            FullProjectInfo fullProjectInfo = new FullProjectInfo(); 
                            fullProjectInfo.projectID = p.ProjectID;
                            fullProjectInfo.projectName = p.Name;
                            fullProjectInfo.location = p.Location;
                            fullProjectInfo.description = p.Description;
                            fullProjectInfo.creationTime = p.CreationTime;
                            fullProjectInfo.active = p.Active;
                            fullProjectInfo.archivedAt = p.Active ? null : p.ArchivedAt;
                            fullProjectInfo.assetCount = p.Assets != null ? p.Assets.Count : 0; // Get p's associated assets for count
                            fullProjectInfo.admins = adminSet;
                            fullProjectInfo.regularUsers = regularSet;

                            result.fullProjectInfos.Add(fullProjectInfo);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing project {p.ProjectID}: {e.Message}");
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred in GetAllProjects: " + e.Message);
                throw;
            }
        }

        public async Task<GetPaginatedProjectAssetsRes> GetPaginatedProjectAssets(GetPaginatedProjectAssetsReq req, int requesterID)
        {
            int offset = (req.pageNumber - 1) * req.assetsPerPage;
            try 
            {
                (
                    List<Asset> retrievedAssets, 
                    int totalFilteredAssetCount,
                    List<string> assetBlobSASUrlList
                ) = await _repository.GetPaginatedProjectAssetsInDb(req, offset, requesterID);
                
                int totalPages = (int)Math.Ceiling((double)totalFilteredAssetCount / req.assetsPerPage);

                ProjectAssetsPagination pagination = new ProjectAssetsPagination
                {
                    pageNumber = req.pageNumber, 
                    assetsPerPage = req.assetsPerPage, 
                    totalAssetsReturned = retrievedAssets.Count, 
                    totalPages = totalPages
                };
                
                List<PaginatedProjectAsset> paginatedProjectAssets = retrievedAssets.Select(a => new PaginatedProjectAsset
                {
                    blobID = a.BlobID,
                    filename = a.FileName,
                    uploadedBy = new PaginatedProjectAssetUploadedBy
                    {
                        userID = a.User?.UserID ?? -1,
                        name = a.User?.Name ?? "Unknown",
                        email = a.User?.Email ?? "Unknown",
                    },
                    date = a.LastUpdated,
                    filesizeInKB = a.FileSizeInKB,
                    mimetype = a.MimeType,
                    tags = a.AssetTags.Select(t => t.Tag.Name).ToList()
                }).ToList();


                List<GetAssetFileFromStorageReq> assetIdNameList = retrievedAssets
                    .Select(a => new GetAssetFileFromStorageReq { blobID = a.BlobID, filename = a.FileName })
                    .ToList();

                GetPaginatedProjectAssetsRes result = new GetPaginatedProjectAssetsRes
                {  
                    projectID = req.projectID, 
                    assets = paginatedProjectAssets, 
                    assetBlobSASUrlList = assetBlobSASUrlList,
                    pagination = pagination,
                    assetIdNameList = assetIdNameList
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


        public async Task<UpdateProjectRes> UpdateProject(int projectID, UpdateProjectReq req)
        {
            return await _repository.UpdateProjectInDb(projectID, req);
        }
        

        public async Task<List<GetProjectRes>> GetMyProjects(int userId)
        {
            List<Project> projects = await _repository.GetProjectsForUserInDb(userId);

            List<GetProjectRes> result = projects.Select(p => new GetProjectRes
            {
                projectID = p.ProjectID,
                name = p.Name,
                description = p.Description,
                location = p.Location,
                active = p.Active,
                archivedAt = p.ArchivedAt,
                admins = p.ProjectMemberships?
                    .Where(pm => pm.User != null && pm.UserRole == ProjectMembership.UserRoleType.Admin)
                    .Select(pm => new UserCustomInfo
                    {
                        userID = pm.User.UserID,
                        name = pm.User.Name,
                        email = pm.User.Email
                    }).ToList() ?? new List<UserCustomInfo>(),
                regularUsers = p.ProjectMemberships?
                    .Where(pm => pm.User != null && pm.UserRole == ProjectMembership.UserRoleType.Regular)
                    .Select(pm => new UserCustomInfo
                    {
                        userID = pm.User.UserID,
                        name = pm.User.Name,
                        email = pm.User.Email
                    }).ToList() ?? new List<UserCustomInfo>(),
                tags = p.ProjectTags?
                    .Select(pt => new TagCustomInfo
                    {
                        tagID = pt.Tag.TagID,
                        name = pt.Tag.Name
                    }).ToList() ?? new List<TagCustomInfo>(),
                metadataFields = p.ProjectMetadataFields?
                    .Select(pmf => new ProjectMetadataCustomInfo
                    {
                        FieldID = pmf.FieldID,
                        FieldName = pmf.FieldName,
                        FieldType = pmf.FieldType.ToString(),
                        IsEnabled = pmf.IsEnabled
                    }).ToList() ?? new List<ProjectMetadataCustomInfo>()
            }).ToList();

            return result;
        }

        public async Task<string?> GetAssetNameByBlobIdAsync(string blobID)
        {
            return await _repository.GetAssetNameByBlobIdAsync(blobID);
        }

        public async Task<string?> GetProjectNameByIdAsync(int projectID) {
            return await _repository.GetProjectNameByIdAsync(projectID);
        }

        public async Task<string?> GetTagNameByIdAsync(int tagID) {
            return await _repository.GetTagNameByIdAsync(tagID);
        }
        
        public async Task DeleteAssetFromProject(int projectId, string blobId)
        {
            await _repository.DeleteAssetFromProjectInDb(projectId, blobId);
        }

        public async Task<string?> GetCustomMetadataNameByIdAsync(int fieldID) 
        {
            return await _repository.GetCustomMetadataNameByIdAsync(fieldID);
        }
    }
}