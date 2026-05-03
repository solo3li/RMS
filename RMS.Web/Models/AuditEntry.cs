using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace RMS.Web.Models
{
    public enum AuditType
    {
        None = 0,
        Create = 1,
        Update = 2,
        Delete = 3
    }

    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }
        public string? UserId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public Dictionary<string, object?> KeyValues { get; } = new();
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public AuditType AuditType { get; set; }
        public List<string> ChangedColumns { get; } = new();

        public bool HasTemporaryProperties => Entry.Properties.Any(p => p.IsTemporary);

        public IEnumerable<PropertyEntry> TemporaryProperties => Entry.Properties.Where(p => p.IsTemporary);

        public AuditLog ToAudit()
        {
            var audit = new AuditLog();
            audit.UserId = UserId;
            audit.Action = AuditType.ToString();
            audit.EntityType = TableName;
            audit.Timestamp = DateTime.UtcNow;
            audit.EntityId = JsonSerializer.Serialize(KeyValues);
            audit.BeforeValue = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues);
            audit.AfterValue = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues);
            return audit;
        }
    }
}
