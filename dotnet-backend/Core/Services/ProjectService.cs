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

        public async Task<AssociateAssetsRes> AssociateAssetsWithProject(int projectID, List<int> blobIDs, int submitterID)
        {
            try 
            {
                (List<int> successfulAssociations, List<int> failedAssociations) = await _repository.AssociateAssetsWithProjectinDb(projectID, blobIDs, submitterID);
                AssociateAssetsRes result = new AssociateAssetsRes
                {
                    projectID = projectID,
                    success = successfulAssociations,
                    fail = failedAssociations,
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
                    List<ArchivedProject> projectsNewlyArchived = new List<ArchivedProject>();
                    List<ArchivedProject> projectsAlreadyArchived = new List<ArchivedProject>();

                    (List<int> unfoundProjectIDs, Dictionary<int, DateTime> NewArchivedProjects, Dictionary<int, DateTime> ProjectsArchivedAlready) 
                        = await _repository.ArchiveProjectsInDb(projectIDs);

                    if (NewArchivedProjects != null)
                    {
                        foreach (KeyValuePair<int, DateTime> pair in NewArchivedProjects)
                        {
                            ArchivedProject ap = new ArchivedProject{ projectID = pair.Key, archiveTimestampUTC = pair.Value};
                            projectsNewlyArchived.Add(ap);
                        }
                    }

                    if (ProjectsArchivedAlready != null)
                    {
                        foreach (KeyValuePair<int, DateTime> pair in ProjectsArchivedAlready)
                        {
                            ArchivedProject ap = new ArchivedProject{ projectID = pair.Key, archiveTimestampUTC = pair.Value};
                            projectsAlreadyArchived.Add(ap);
                        }
                    } 

                    ArchiveProjectsRes res = new ArchiveProjectsRes
                    { 
                        projectsNewlyArchived = projectsNewlyArchived, 
                        projectsAlreadyArchived = projectsAlreadyArchived, 
                        unfoundProjectIDs = unfoundProjectIDs 
                    };

                    return res;
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
                List<string> adminList = new List<string>();
                List<string> regularUserList = new List<string>();
                foreach (ProjectMembership pm in project.ProjectMemberships)
                {
                    (pm.UserRole == ProjectMembership.UserRoleType.Admin 
                        ? adminList 
                        : regularUserList).Add(pm.User.Name);
                }
                List<string> tags = project.ProjectTags.Select(pt => pt.Tag.Name).ToList();

                // TODO: Check if the user is admin or regular. If user is regular and if project is archived, throw ArchivedException 

                GetProjectRes result = new GetProjectRes
                {
                    projectID = project.ProjectID,
                    name = project.Name,
                    description = project.Description,
                    location = project.Location,
                    active = project.Active,
                    archivedAt = project.ArchivedAt,
                    adminNames = adminList,
                    regularUserNames = regularUserList,
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

                // foreach (User user in retrievedUsers)
                // {
                //     Console.WriteLine($"User ID: {user.UserID}");
                // }
            
                // foreach (ProjectMembership rpm in retrievedProjectMemberships)
                // {
                //     Console.WriteLine($"retrievedProjectMembership project ID: {rpm.ProjectID}");
                //     Console.WriteLine($"retrievedProjectMembership user ID: {rpm.UserID}");
                // }

                // Make List retrievedUsers into a map
                Dictionary<int, User> retrievedUserDictionary = retrievedUsers.ToDictionary(u => u.UserID);

                GetAllProjectsRes result = new GetAllProjectsRes();

                result.fullProjectInfos = new List<FullProjectInfo>();

                result.projectCount = retrievedProjects.Count;

                // Create a projectMembershipMap for constructig the return result; 
                // The value is a tuple of 2 string lists: first is for admin, and second is for regular users
                Dictionary<int, (HashSet<string>, HashSet<string>)> projectMembershipMap = new Dictionary<int, (HashSet<string>, HashSet<string>)>();
                
                foreach (ProjectMembership pm in retrievedProjectMemberships) 
                {
                    if (!projectMembershipMap.ContainsKey(pm.ProjectID))
                    {
                        projectMembershipMap[pm.ProjectID] = (new HashSet<string>(), new HashSet<string>());
                    }

                    if (retrievedUserDictionary.ContainsKey(pm.UserID)) 
                    {
                        (HashSet<string> adminSet, HashSet<string> regularSet) = projectMembershipMap[pm.ProjectID];
                        if (pm.UserRole == ProjectMembership.UserRoleType.Admin) 
                        {
                            adminSet.Add(retrievedUserDictionary[pm.UserID].Name);
                        }
                        else if (pm.UserRole == ProjectMembership.UserRoleType.Regular) 
                        {
                            regularSet.Add(retrievedUserDictionary[pm.UserID].Name);
                        }
                        projectMembershipMap[pm.ProjectID] = (adminSet, regularSet); // Update the dictionary       
                        // Console.WriteLine($"adminSet: {string.Join(", ", adminSet)}");         
                        // Console.WriteLine($"regularSet: {string.Join(", ", regularSet)}");         
                    }
                }

                // Constructing the result for return by looping through retrievedProjects and using the retrievedUserDictionary 
                // Create a HashSet to prevent duplicated retrievedProjects

                // TODO: Check if the user is admin or regular. If user is regular then only include active projects, 
                HashSet<Project> addedProjects = new HashSet<Project>();
                foreach (var p in retrievedProjects)
                {
                    if (!addedProjects.Contains(p)) 
                    {
                        addedProjects.Add(p);

                        // Populate fullProjectInfo
                        (HashSet<string> adminSet, HashSet<string> regularSet) = projectMembershipMap[p.ProjectID];
                        FullProjectInfo fullProjectInfo = new FullProjectInfo(); 
                        fullProjectInfo.projectID = p.ProjectID;
                        fullProjectInfo.projectName = p.Name;
                        fullProjectInfo.location = p.Location;
                        fullProjectInfo.description = p.Description;
                        fullProjectInfo.creationTime = p.CreationTime;
                        fullProjectInfo.active = p.Active;
                        fullProjectInfo.archivedAt = p.Active ? null : p.ArchivedAt;
                        fullProjectInfo.assetCount = p.Assets != null ? p.Assets.Count : 0; // Get p's associated assets for count
                        fullProjectInfo.adminNames = adminSet;
                        fullProjectInfo.regularUserNames = regularSet;

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
            //TODO: May need to retrive actual assets from Blob to return together.
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
                        paginatedProjectAssets.Add(paginatedProjectAsset);
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
