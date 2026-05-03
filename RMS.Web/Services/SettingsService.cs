using Microsoft.EntityFrameworkCore;
using RMS.Web.Data;

namespace RMS.Web.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;

        public SettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            return await _context.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
        }
    }
}
