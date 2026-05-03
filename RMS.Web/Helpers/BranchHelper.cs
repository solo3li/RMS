using System;

namespace RMS.Web.Helpers
{
    public static class BranchHelper
    {
        public static bool IsWithinWorkingHours(string workingHours)
        {
            if (string.IsNullOrEmpty(workingHours)) return true; // Assume always open if not set

            try
            {
                var parts = workingHours.Split('-');
                if (parts.Length != 2) return true;

                var now = DateTime.Now.TimeOfDay;
                var start = TimeSpan.Parse(parts[0]);
                var end = TimeSpan.Parse(parts[1]);

                if (start <= end)
                {
                    return now >= start && now <= end;
                }
                else
                {
                    // Handle overnight hours (e.g., 22:00-04:00)
                    return now >= start || now <= end;
                }
            }
            catch
            {
                return true; // Fallback to open on parse error
            }
        }
    }
}
