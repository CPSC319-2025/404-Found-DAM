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

        public async Task<List<Log>> GetLogsAsync(int? userID, string? action, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Logs.AsQueryable();

            if (userID.HasValue)
                query = query.Where(log => log.UserID == userID.Value);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(log => log.Action.Contains(action));

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            return await query.OrderByDescending(log => log.Timestamp).ToListAsync();
        }
    }
}
