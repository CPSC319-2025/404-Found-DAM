using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.DataAccess
{
    public class EFCoreAdminRepository : IAdminRepository
    {
        private IDbContextFactory<DAMDbContext> _contextFactory;
        public EFCoreAdminRepository(IDbContextFactory<DAMDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<(bool, string)> ToggleMetadataCategoryActivationInDb(int projectID, int metadataFieldID, bool setEnabled)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();
           
            // Check if project exists
            var project = await _context.Projects.FindAsync(projectID);

            if (project != null) 
            {
                // Access metadata and update the specific field
                var projectMetadataField = await _context.ProjectMetadataFields
                    .Include(pmf => pmf.MetadataField) // Eager loading
                    .FirstOrDefaultAsync(pmf => pmf.FieldID == metadataFieldID && pmf.FieldID == metadataFieldID);                
                    
                if (projectMetadataField != null) 
                {
                    projectMetadataField.IsEnabled = setEnabled;
                    await _context.SaveChangesAsync();
                    return (true, projectMetadataField.MetadataField.FieldName);
                } 
                else 
                {
                    throw new DataNotFoundException($"No such metadata field found for Project {projectID}.");
                }
            } 
            else 
            {
                throw new DataNotFoundException($"Project {projectID} not found.");
            }
        }

        public async Task<(User, List<ProjectMembership>)> GetRoleDetailsInDb(int userID)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // get User and associated ProjectMemberships
            var user = await _context.Users
                .Include(u => u.ProjectMemberships)
                .FirstOrDefaultAsync(u => u.UserID == userID);

            return user != null 
                ? (user, user.ProjectMemberships.ToList()) 
                : throw new DataNotFoundException("No user found.");
        }

        public async Task<DateTime> ModifyRoleInDb(int projectID, int userID, bool userToAdmin)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();
            
            // Get the ProjectMembership and update the user role if found
            var projectMembership = await _context.ProjectMemberships
                .FirstOrDefaultAsync(pm => pm.ProjectID == projectID && pm.UserID == userID);

            if (projectMembership != null)
            {
                projectMembership.UserRole = userToAdmin 
                    ? ProjectMembership.UserRoleType.Admin 
                    : ProjectMembership.UserRoleType.Regular;
                return DateTime.UtcNow;
            }
            else 
            {
                throw new DataNotFoundException("No record found");
            }
        }


        public async Task<int> AddMetaDataToProjectInDb(int projectID, string fieldName, string fieldType)
        {
            using DAMDbContext _context = _contextFactory.CreateDbContext();

            // Get the project; 
            var project = await _context.Projects.FindAsync(projectID);

            if (project != null) 
            {
                // If project found, insert this new metadata field to the metadatafield table and get the ID
                MetadataField.FieldDataType dataType;
                if (Enum.TryParse(fieldType, true, out dataType))
                {
                    MetadataField mf = new MetadataField { FieldName = fieldName, FieldType = dataType };
                    await _context.MetadataFields.AddAsync(mf);
                    await _context.SaveChangesAsync();
                    
                    // Insert a new entry using the projectID and the fieldID to the ProjectMetadataField table, and return the fieldID
                    int newFieldID = mf.FieldID;
                    ProjectMetadataField pmf = new ProjectMetadataField
                    {
                        IsEnabled = false, // Explicitly set IsEnabled (default)
                        FieldValue = "", // Empty string 
                        
                        // Do NOT set projectID and fieldID. Below will let EF Core takes care of everything.
                        Project = project,
                        MetadataField = mf
                    };
                    await _context.ProjectMetadataFields.AddAsync(pmf);
                    await _context.SaveChangesAsync();
                    return newFieldID;
                }
                else
                {
                throw new ArgumentException($"Invalid field type: {fieldType}");
                }
            }
            else 
            {
                throw new DataNotFoundException("Project not found");
            }
        }
    }
}
