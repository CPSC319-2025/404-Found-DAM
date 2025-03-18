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
        private DAMDbContext _context;

        public ActivityLogRepository(DAMDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddLogAsync(Log log)
        {
            try
            {
                _context.Logs.Add(log);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Log>> GetLogsAsync(int? userID, string? changeType, int? projectID, int? assetID, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Logs.AsQueryable();

            if (userID.HasValue)
                query = query.Where(log => log.UserID == userID.Value);

            if (!string.IsNullOrEmpty(changeType))
                query = query.Where(log => log.ChangeType.Equals(changeType));

            if (projectID.HasValue)
                query = query.Where(log => log.ProjectID == projectID.Value);

            if (assetID.HasValue)
                query = query.Where(log => log.AssetID == assetID.Value);

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            return await query.AsNoTracking().OrderByDescending(log => log.Timestamp).ToListAsync();
        }

    }
