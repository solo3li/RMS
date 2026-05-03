namespace RMS.Web.Services
{
    public interface ISettingsService
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task<Dictionary<string, string>> GetAllSettingsAsync();
    }
}
