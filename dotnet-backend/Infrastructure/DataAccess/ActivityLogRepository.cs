using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Dtos;
using Core.Entities;

namespace Infrastructure.DataAccess;
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly IDbContextFactory<DAMDbContext> _contextFactory;

        public ActivityLogRepository(IDbContextFactory<DAMDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<bool> AddLogAsync(Log log)
        {

            using var _context = _contextFactory.CreateDbContext();
            try
            {
                _context.Logs.Add(log);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while saving log: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Log>> GetLogsAsync(int? userID, string? changeType, int? projectID, string? assetID, DateTime? fromDate, DateTime? toDate, bool isAdminAction)
        {
            using var _context = _contextFactory.CreateDbContext();
            var query = _context.Logs.AsQueryable();

            if (userID.HasValue)
                query = query.Where(log => log.UserID == userID.Value);

            if (!string.IsNullOrEmpty(changeType))
                query = query.Where(log => log.ChangeType.Equals(changeType));

            if (projectID.HasValue)
                query = query.Where(log => log.ProjectID == projectID.Value);

            if (!string.IsNullOrEmpty(assetID))
                query = query.Where(log => log.AssetID.Equals(assetID));

            if (fromDate.HasValue)
            {
                DateTime utcFromDate = fromDate.Value.ToUniversalTime();
                query = query.Where(log => log.Timestamp >= utcFromDate);
            }

            if (toDate.HasValue)
            {
                DateTime utcToDate = toDate.Value.ToUniversalTime();
                query = query.Where(log => log.Timestamp <= utcToDate);
            }

            return await query.AsNoTracking().OrderByDescending(log => log.Timestamp).ToListAsync();
        }

    }